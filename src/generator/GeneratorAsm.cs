using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;

public class GeneratorAsm : GeneratorBase
{

    public TFAsmGen TFAsmGen;
    ICallingConvention callingConvention;
    RegisterProfile registerProfile;
    Dictionary<IrFunction, FunctionContext> contexts = new Dictionary<IrFunction, FunctionContext>();
    Dictionary<IrFunction, FunctionFrame> frames = new Dictionary<IrFunction, FunctionFrame>();
    Dictionary<IrBlock, BlockLiveness> liveness = new Dictionary<IrBlock, BlockLiveness>();

    FunctionContext currentContext;
    FunctionFrame currentFrame;
    public GeneratorAsm(IrModule module, Arguments arguments) : base(module, arguments)
    {
        if (arguments.CallingConventions == CallingConventions.SysV)
        {
            if (arguments.Bits == 64)
            {
                callingConvention = BackendSelector.Create(RegisterProfile.X86_64_SysV);
                registerProfile = RegisterProfile.X86_64_SysV;
            }
            else if (arguments.Bits == 32)
            {
                callingConvention = BackendSelector.Create(RegisterProfile.X86_32_SysV);
                registerProfile = RegisterProfile.X86_32_SysV;
            }
        }

        TFAsmGen = new TFAsmGen(callingConvention);
        ScratchAllocator = new ScratchRegisterAllocator(callingConvention, registerProfile);
    }

    public override void GeneratePass()
    {
        foreach (IrFunction f in Module.Functions)
        {
            int offset = 0;
            currentContext = new FunctionContext();

            foreach (IrLocal l in f.Locals)
            {
                int size = align(Math.Max(1, l.Type.SizeInBits / 8), 8);
                offset += size;

                var v = new Variable
                {
                    Local = l,
                    StackOffset = -offset,
                    AlignedSize = size,
#if DEBUG
                    Name = l.Name,
#endif
                };


                currentContext.AddLocal(l, v);
            }
            foreach (IrTemp t in f.Temps)
            {
                int size = align(Math.Max(1, t.Type.SizeInBits / 8), 8);
                offset += size;

                var v = new Variable
                {
                    Temp = t,
                    StackOffset = -offset,
                    AlignedSize = size,
#if DEBUG
                    Name = t.Dump(),
#endif
                };

                currentContext.AddTemp(t, v);
            }

            int frameSize = align(offset, callingConvention.StackAlignment);

            currentFrame = TFAsmGen.EnterFunction(f.Name, frameSize, callingConvention, registerProfile);

            foreach (IrBlock b in f.Blocks)
            {
                liveness[b] = new BlockLiveness();
                ComputeUseDef(b, liveness[b]);
            }

            ComputeLiveness(f);


            contexts[f] = currentContext;
            frames[f] = currentFrame;
        }
    }

    void ComputeUseDef(IrBlock block, BlockLiveness lv)
    {
        foreach (IrInstr instr in block.Instrs)
        {
            // Uses
            foreach (IrOperand op in instr.Operands)
            {
                if (op.GetOperand(out IrTemp t) && !lv.Def.Contains(t))
                    lv.Use.Add(t);
            }

            // Def
            if (instr.Result != null &&
                instr.Result.GetOperand(out IrTemp defTemp))
            {
                lv.Def.Add(defTemp);
            }
        }
    }

    int align(int size, int alignment)
    {
        int padding = (alignment - (size % alignment)) % alignment;
        int aligned = size + padding;
        return aligned;
    }

    public override void Generate()
    {
        if (Module.Strings.Count > 0)
        {
            Output.Add(TFAsmGen.Data());
            foreach (IrConstStr s in Module.Strings)
            {
                Output.Add($"const_str {s.Label} = \"{s.Value}\"");
            }
        }

        Output.Add(TFAsmGen.Text());
        foreach (IrFunction f in Module.Functions)
        {
            currentContext = contexts[f];
            currentFrame = frames[f];

            string paramList = "";
            foreach (IrLocal param in f.Parameters)
            {
                paramList += $"{param.Name} : {param.Type.Dump()}, ";
            }
            paramList = paramList.TrimEnd(' ', ',');
            Output.Add($"; func @{f.Name}({paramList}) : {f.ReturnType.Dump()}");
            int startFunctionIndex = Output.Count;

            foreach (IrBlock b in f.Blocks)
            {
                Output.Add($"; {b.Label}:");
                Output.Add($"; new scope");
                BlockLiveness lv = liveness[b];
                foreach (IrInstr i in b.Instrs)
                {
                    bool isLastInstrInFunction = f.Blocks.Last().Instrs.Last() == i;
                    GenerateIrInstr(i, isLastInstrInFunction);

                    // 🔴 FREE DEAD TEMPS AFTER EACH INSTR (simple version)
                    FreeDeadTemps(lv);
                }

                // 🔴 ENSURE BLOCK-EXIT CLEANUP
                FreeDeadTemps(lv);
                Output.Add($"; end of scope");
            }

            Output.Insert(startFunctionIndex, PrologueEpilogueGenerator.GeneratePrologue(currentFrame, callingConvention, registerProfile));
            Output.Add($"{f.Name}_end:");
            Output.Add(PrologueEpilogueGenerator.GenerateEpilogue(currentFrame, callingConvention, registerProfile));
        }
    }
    void ComputeLiveness(IrFunction f)
    {
        bool changed;
        do
        {
            changed = false;

            for (int i = f.Blocks.Count - 1; i >= 0; i--)
            {
                IrBlock b = f.Blocks[i];
                BlockLiveness lv = liveness[b];

                var oldIn = new HashSet<IrTemp>(lv.LiveIn);
                var oldOut = new HashSet<IrTemp>(lv.LiveOut);

                // LiveOut = union of successors' LiveIn
                lv.LiveOut.Clear();
                foreach (IrBlock succ in GetSuccessors(f, b))
                    lv.LiveOut.UnionWith(liveness[succ].LiveIn);

                // LiveIn = Use ∪ (LiveOut - Def)
                lv.LiveIn.Clear();
                lv.LiveIn.UnionWith(lv.Use);
                lv.LiveIn.UnionWith(lv.LiveOut.Except(lv.Def));

                if (!oldIn.SetEquals(lv.LiveIn) ||
                    !oldOut.SetEquals(lv.LiveOut))
                    changed = true;
            }
        }
        while (changed);
    }
    IEnumerable<IrBlock> GetSuccessors(IrFunction f, IrBlock b)
    {
        int index = f.Blocks.IndexOf(b);

        // TEMP: linear fallthrough only
        if (index + 1 < f.Blocks.Count)
            yield return f.Blocks[index + 1];
    }
    void FreeDeadTemps(BlockLiveness lv)
    {
        var deadTemps = currentContext.tempLocations
            .Keys
            .Where(t => !lv.LiveOut.Contains(t))
            .ToList();

        foreach (IrTemp t in deadTemps)
        {
            RegOperand reg = currentContext.tempLocations[t].Reg;
            ScratchAllocator.Release(reg);
            currentContext.tempLocations.Remove(t);
        }
    }


    public AsmOperand GenerateIrOperand(IrOperand irOperand)
    {
        if (irOperand == null)
        {
            Console.WriteLine("unkownen operand");
            throw new InvalidOperationException("Null IR operand");
        }

        if (irOperand.GetOperand(out IrLocal local))
        {
            if (currentContext.GetLocalVariable(local, out Variable localVar))
            {
                return new MemOperand(callingConvention.GetRegister(RegisterFunction.BasePointer), localVar.StackOffset);
            }
        }
        else if (irOperand.GetOperand(out IrTemp temp))
        {
            if (currentContext.tempLocations.TryGetValue(temp, out var loc))
                return loc.Reg;

            // allocate new register
            RegOperand reg = ScratchAllocator.Allocate();
            currentContext.tempLocations[temp] = new TempLocation { Reg = reg };
            return reg;
        }

        else if (irOperand.GetOperand(out IrConstInt constInt))
        {
            return new ImmOperand(constInt.Value);
        }

        throw new InvalidOperationException($"Unsupported IR operand: {irOperand.Dump()}");
    }
    public void GenerateIrInstr(IrInstr instr, bool isLastInstr)
    {
        string instrName = instr.Instr.ToLower();

        switch (instrName)
        {
            case "move":
                if (instr.Result != null)
                {
                    Output.Add($";   move with {instr.Operands.Count} operands {string.Join(", ", instr.Operands.Select(GenerateIrOperand))} + {GenerateIrOperand(instr.Result)}");
                    Output.Add(TFAsmGen.Move(GenerateIrOperand(instr.Result), GenerateIrOperand(instr.Operands[0])));
                }
                else
                {
                    Output.Add($";   move with {instr.Operands.Count} operands {string.Join(", ", instr.Operands.Select(GenerateIrOperand))}");
                    Output.Add(TFAsmGen.Move(GenerateIrOperand(instr.Operands[0]), GenerateIrOperand(instr.Operands[1])));
                }
                break;
            case "ret":
                Output.Add($";   ret with {instr.Operands.Count} operands {string.Join(", ", instr.Operands.Select(GenerateIrOperand))}");
                if (instr.Operands.Count > 0)
                {
                    Output.Add(TFAsmGen.Move(new RegOperand(callingConvention.GetRegister(RegisterFunction.ReturnInt)), GenerateIrOperand(instr.Operands[0])));
                }
                else
                {
                    Output.Add(TFAsmGen.Move(new RegOperand(callingConvention.GetRegister(RegisterFunction.ReturnInt)), new ImmOperand(0)));
                }
                if (!isLastInstr)
                {
                    Output.Add($";   jmp to {currentFrame.FunctionName}_end");
                    Output.Add(TFAsmGen.Jump(new LableOperand($"{currentFrame.FunctionName}_end")));
                }
                break;
            default:
                Output.Add($";   {instr.Dump()}");
                break;
        }
    }

}

internal class Variable
{
    public IrTemp Temp;
    public IrLocal Local;

    public string Name { get; set; }
    public int StackOffset { get; internal set; }
    public int AlignedSize { get; set; }
}
