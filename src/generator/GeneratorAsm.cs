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
    Dictionary<IrTemp, IrConstInt> constTemps = new Dictionary<IrTemp, IrConstInt>();
    Dictionary<string, string> stringLiterals = new Dictionary<string, string>();

    List<string> functionList = new List<string>();

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

        ScratchAllocator = new ScratchRegisterAllocator(callingConvention, registerProfile);
        TFAsmGen = new TFAsmGen(callingConvention, ScratchAllocator);
    }

    public override void GeneratePass()
    {
        foreach (IrFunction f in Module.Functions)
        {
            functionList.Add(f.Name);
            int offset = 0;
            currentContext = new FunctionContext();

            foreach (IrLocal l in f.Locals)
            {
                int size = Math.Max(1, l.Type.SizeInBits / 8);
                int alignedSize = align(size, 8);
                offset += alignedSize;

                var v = new Variable
                {
                    Local = l,
                    StackOffset = -offset,
                    AlignedSize = alignedSize,
                    Size = size,
#if DEBUG
                    Name = l.Name,
#endif
                };


                currentContext.AddLocal(l, v);
            }
            foreach (IrTemp t in f.Temps)
            {
                int size = Math.Max(1, t.Type.SizeInBits / 8);
                int alignedSize = align(size, 8);
                offset += alignedSize;

                var v = new Variable
                {
                    Temp = t,
                    StackOffset = -offset,
                    AlignedSize = alignedSize,
                    Size = size,
#if DEBUG
                    Name = t.Dump(),
#endif
                };

                currentContext.AddTemp(t, v);
            }

            int frameSize = align(offset, callingConvention.Stack.Alignment);

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
        foreach (IrInstr instr in block.Instructions)
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
            Output.Add(TFAsmGen.Rodata());
            foreach (IrConstStr s in Module.Strings)
            {
                // Output.Add($"const_str {s.Label} = \"{s.Value}\"");
                stringLiterals.Add(s.Label, s.Value);
                Output.Add($"{s.Label}: db {s.Value}");
                if (Program.Arguments.UseFatStrings == true)
                {
                    Output.Add($"{s.Label}_Length: dw $-{s.Label}");
                }
            }
        }

        Output.Add(TFAsmGen.Text());
        foreach (IrFunction f in Module.Functions)
        {
            if (f.isExternal)
            {
                Output.Add($"extern {f.Name}");
                continue;
            }
            currentContext = contexts[f];
            currentFrame = frames[f];

            string paramList = "";
            foreach (IrLocal param in f.Parameters)
            {
                paramList += $"{param.Name} : {param.Type.Dump()}, ";
            }
            paramList = paramList.TrimEnd(' ', ',');
            Output.Add($"; func @{f.Name}({paramList}) : {f.ReturnType.Dump()}");
            Output.Add(PrologueEpilogueGenerator.GeneratePrologue(currentFrame, callingConvention, registerProfile));

            foreach (IrBlock b in f.Blocks)
            {
                // Output.Add($"; {b.Label}:");
                // Console.WriteLine($"New block {b.Label} with {b.Instructions.Count} Instructions");
                // Output.Add($"; new Block");
                BlockLiveness lv = liveness[b];
                if (f.Blocks.Last().Instructions.Count != 0)
                {
                    int instrIndex = 0;
                    foreach (IrInstr i in b.Instructions)
                    {
                        IrInstr lastInstr = null;
                        if (instrIndex > 0)
                        {
                            lastInstr = b.Instructions[instrIndex - 1];
                        }
                        bool isLastInstrInFunction = f.Blocks.Last().Instructions.Last() == i;
                        GenerateIrInstr(i, isLastInstrInFunction, lastInstr);
                        Output.Add("");
                        instrIndex++;
                        // FreeDeadTemps(lv);
                    }
                }

                FreeDeadTemps(lv);
                // Output.Add($"; Block end");
                // Console.WriteLine($"Block end {b.Label}");
            }

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
                foreach (IrBlock successors in GetSuccessors(f, b))
                    lv.LiveOut.UnionWith(liveness[successors].LiveIn);

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
            .Where(kv => kv.Value.Reg != null && !lv.LiveOut.Contains(kv.Key))
            .Select(kv => kv.Key)
            .ToList();

        foreach (IrTemp t in deadTemps)
        {
            RegOperand reg = currentContext.tempLocations[t].Reg;
            ScratchAllocator.Release(reg);
            currentContext.tempLocations.Remove(t);
            Output.Add(TFAsmGen.Comment($"[DEBUG] Temp {t.Dump()} freed from {reg}"));
        }
    }


    public AsmOperand GenerateIrOperand(IrOperand irOperand, bool forceNonReg = false, bool forceNonAlloc = false)
    {
        if (irOperand == null)
        {
            Console.WriteLine("unknown operand");
            throw new InvalidOperationException("Null IR operand");
        }

        if (irOperand.GetOperand(out IrLocal local))
        {
            if (currentContext.GetLocalVariable(local, out Variable localVar))
            {
                return new MemOperand(callingConvention.GetRegister(RegisterRole.BasePointer, "BasePointer"), localVar.Size, localVar.StackOffset);
            }
        }
        else if (irOperand.GetOperand(out IrTemp temp))
        {
            if (constTemps.TryGetValue(temp, out IrConstInt constInt))
            {
                return new ImmOperand(constInt.Value) { Type = constInt.Type };
            }
            Variable tempVar;
            if (!currentContext.GetTempVariable(temp, out tempVar))
            {
                throw new InvalidOperationException($"Temp variable not found: {temp.Dump()}");
            }
            if (currentContext.tempLocations.TryGetValue(temp, out var loc))
            {
                // Load from stack into a scratch register if needed
                if (loc.Reg == null && forceNonReg == false && forceNonAlloc == false)
                {
                    RegOperand newReg = ScratchAllocator.AllocateTemp();
                    if (newReg != null)
                    {
                        Output.Add(TFAsmGen.Move(newReg, new MemOperand(callingConvention.GetRegister(RegisterRole.BasePointer, "BasePointer"), tempVar.Size, loc.StackOffset.Value)));
                        Output.Add(TFAsmGen.Comment($"[DEBUG] Allocated temp {temp.Dump()} to {newReg} from stack"));
                        loc.Reg = newReg;
                    }
                }

                if (loc.Reg == null || forceNonReg == true)
                {
                    return new MemOperand(callingConvention.GetRegister(RegisterRole.BasePointer, "BasePointer"), tempVar.Size, loc.StackOffset.Value);
                }
                return loc.Reg.GetRegisterOpBySize(tempVar.Size);
            }

            // allocate new register
            RegOperand reg = null;
            if (forceNonReg == false)
            {
                reg = ScratchAllocator.AllocateTemp();
            }
            if (reg != null)
            {
                currentContext.tempLocations[temp] = new TempLocation { Reg = reg, StackOffset = currentContext.temps[temp].StackOffset };
                Output.Add(TFAsmGen.Comment($"[DEBUG] Allocated temp {temp.Dump()} to {reg}"));
                return reg.GetRegisterOpBySize(tempVar.Size);
            }
            else
            {
                // No free registers → spill to stack
                tempVar = currentContext.temps[temp];
                currentContext.tempLocations[temp] = new TempLocation { Reg = null, StackOffset = tempVar.StackOffset };
                Output.Add(TFAsmGen.Comment($"[DEBUG] Allocated temp {temp.Dump()} to {new MemOperand(callingConvention.GetRegister(RegisterRole.BasePointer, "BasePointer"), tempVar.Size, tempVar.StackOffset)} on stack"));
                return new MemOperand(callingConvention.GetRegister(RegisterRole.BasePointer, "BasePointer"), tempVar.Size, tempVar.StackOffset);
            }
        }

        else if (irOperand.GetOperand(out IrConstInt constInt))
        {
            return new ImmOperand(constInt.Value);
        }
        else if (irOperand.GetOperand(out IrLabel label))
        {
            return new LabelOperand(label.Name);
        }
        else if (irOperand.GetOperand(out IrSymbol symbol))
        {
            if (stringLiterals.TryGetValue(symbol.Name, out string labelName))
            {
                return new PtrOperand(symbol.Name);
            }
            else if (functionList.Contains(symbol.Name))
            {
                return new LabelOperand(symbol.Name);
            }
            else
            {
                throw new InvalidOperationException($"String literal not found: {symbol.Name}");
            }
        }

        throw new InvalidOperationException($"Unsupported IR operand: {irOperand.Dump()} {irOperand.GetType()}");
    }
    public void GenerateIrInstr(IrInstr instr, bool isLastInstr, IrInstr? lastInstr = null)
    {
        Output.Add(TFAsmGen.Comment(instr.Dump()));
        string instrName = instr.Instructions.ToLower();
        switch (instrName)
        {
            case "move":
                if (instr.Result != null)
                {
                    if (instr.Result is IrTemp rt && instr.Operands[0] is IrConstInt ci)
                    {
                        constTemps.Add(rt, ci);
                        break;
                    }
                    // Output.Add($";   move with {instr.Operands.Count} operands {string.Join(", ", instr.Operands.Select(GenerateIrOperand))} + {GenerateIrOperand(instr.Result)}");
                    Output.Add(TFAsmGen.Move(GenerateIrOperand(instr.Result), GenerateIrOperand(instr.Operands[0])));
                }
                else
                {
                    // Output.Add($";   move with {instr.Operands.Count} operands {string.Join(", ", instr.Operands.Select(GenerateIrOperand))}");
                    Output.Add(TFAsmGen.Move(GenerateIrOperand(instr.Operands[0]), GenerateIrOperand(instr.Operands[1])));
                }
                break;
            case "ret":
                // Output.Add($";   ret with {instr.Operands.Count} operands {string.Join(", ", instr.Operands.Select(GenerateIrOperand))}");
                if (instr.Operands.Count > 0)
                {
                    Output.Add(TFAsmGen.Move(new RegOperand(callingConvention.GetRegister(RegisterRole.ReturnLow, "Return")), GenerateIrOperand(instr.Operands[0])));
                }
                else
                {
                    Output.Add(TFAsmGen.Move(new RegOperand(callingConvention.GetRegister(RegisterRole.ReturnLow, "Return")), new ImmOperand(0)));
                }
                if (!isLastInstr)
                {
                    // Output.Add($";   jmp to {currentFrame.FunctionName}_end");
                    Output.Add(TFAsmGen.Jump(new LabelOperand($"{currentFrame.FunctionName}_end")));
                }
                break;
            case "neq": // not equal
            case "eq":  // equal
            case "leq": // less than or equal
            case "lt":  // less than
            case "geq": // greater than or equal
            case "gt":  // greater than
                GenerateIrCompareInstr(instr);
                break;
            case "cjump":
                // Output.Add($";   jumpc with {instr.Operands.Count} operands {string.Join(", ", instr.Operands.Select(GenerateIrOperand))}");
                Output.Add(TFAsmGen.Test(GenerateIrOperand(instr.Operands[1])));
                AsmOperand op0 = GenerateIrOperand(instr.Operands[0]);
                if (op0 is ImmOperand imm)
                {
                    int value = (int)imm.Value;
                    if (value == 1)
                    {
                        Output.Add(TFAsmGen.JumpIfZero(GenerateIrOperand(instr.Operands[2])));
                    }
                    else
                    {
                        Output.Add(TFAsmGen.JumpIfNotZero(GenerateIrOperand(instr.Operands[2])));
                    }
                }
                break;
            case "jump":
                // Output.Add($";   jump with {instr.Operands.Count} operands {string.Join(", ", instr.Operands.Select(GenerateIrOperand))}");
                Output.Add(TFAsmGen.Jump(GenerateIrOperand(instr.Operands[0])));
                break;
            case "label":
                // Output.Add($";   label {instr.Operands[0].Dump()}");
                Output.Add($"{((IrLabel)instr.Operands[0]).Name}:");
                break;
            case "add":
            case "sub":
            case "mul":
            case "div":
                GenerateIrArithmeticInstr(instr);
                break;
            case "load":

                RegOperand baseReg = new RegOperand(callingConvention.GetRegister(RegisterRole.AddressBase, "address base"));
                RegOperand indexReg = new RegOperand(callingConvention.GetRegister(RegisterRole.AddressIndex, "address index"));

                Output.Add(TFAsmGen.Move(baseReg, GenerateIrOperand(instr.Operands[0], forceNonAlloc: true)));
                Output.Add(TFAsmGen.Move(indexReg, GenerateIrOperand(instr.Operands[1], forceNonAlloc: true)));
                Output.Add(TFAsmGen.Move(GenerateIrOperand(instr.Result), new MemRegOperand(baseReg.Register, instr.Result.Type.SizeInBits / 8, indexReg.Register)));
                break;
            case "addr_of":
                if (instr.Operands[0].GetType() == typeof(IrLocal) || (!callingConvention.SupportsRipRelative))
                {
                    RegOperand baseAddrReg = new RegOperand(callingConvention.GetRegister(RegisterRole.AddressBase, "address base"));
                    Output.Add(TFAsmGen.Lea(baseAddrReg, GenerateIrOperand(instr.Operands[0], forceNonAlloc: true)));
                    Output.Add(TFAsmGen.Move(GenerateIrOperand(instr.Result), baseAddrReg));
                }
                else
                {
                    Output.Add(TFAsmGen.Lea(GenerateIrOperand(instr.Result), GenerateIrOperand(instr.Operands[0], forceNonAlloc: true)));
                }
                break;

            case "call":
                {
                    List<AsmOperand> args = new List<AsmOperand>();
                    for (int i = 1; i < instr.Operands.Count; i++)
                    {
                        args.Add(GenerateIrOperand(instr.Operands[i]));
                    }
                    Output.Add(callingConvention.EmitCall(TFAsmGen, GenerateIrOperand(instr.Operands[0]), args));
                    if (instr.Result != null)
                    {
                        AsmOperand result = GenerateIrOperand(instr.Result);
                        Output.Add(TFAsmGen.Move(result, new RegOperand(callingConvention.GetRegister(RegisterRole.ReturnLow, "Return"))));
                    }
                }
                break;
            case "syscall":
                {
                    List<AsmOperand> args = new List<AsmOperand>();
                    for (int i = 1; i < instr.Operands.Count; i++)
                    {
                        args.Add(GenerateIrOperand(instr.Operands[i], forceNonAlloc: true));
                    }
                    Output.Add(callingConvention.EmitSyscall(TFAsmGen, GenerateIrOperand(instr.Operands[0], forceNonAlloc: true), args));
                    if (instr.Result != null)
                    {
                        AsmOperand result = GenerateIrOperand(instr.Result);
                        Output.Add(TFAsmGen.Move(result, new RegOperand(callingConvention.GetRegister(RegisterRole.ReturnLow, "Return"))));
                    }
                }
                break;
            default:
                Output.Add($";   need to implement {instr.Dump()}");
                break;
        }
    }

    private void GenerateIrArithmeticInstr(IrInstr instr)
    {
        // Output.Add($";   {instr.Instr} with {instr.Operands.Count} operands {string.Join(", ", instr.Operands.Select(GenerateIrOperand))} + {GenerateIrOperand(instr.Result)}");
        AsmOperand org = GenerateIrOperand(instr.Operands[0]);
        AsmOperand lhs = org;
        if (lhs is ImmOperand _)
        {
            RegOperand scratch = ScratchAllocator.Allocate();
            Output.Add(TFAsmGen.Comment($"[DEBUG] Allocated scratch register {scratch}"));
            Output.Add(TFAsmGen.Move(scratch, lhs));
            lhs = scratch;
        }
        switch (instr.Instructions.ToLower())
        {
            case "add":
                Output.Add(TFAsmGen.Add(lhs, GenerateIrOperand(instr.Operands[1])));
                Output.Add(TFAsmGen.Move(GenerateIrOperand(instr.Result, forceNonAlloc: true), lhs));
                break;
            case "sub":
                Output.Add(TFAsmGen.Sub(lhs, GenerateIrOperand(instr.Operands[1])));
                Output.Add(TFAsmGen.Move(GenerateIrOperand(instr.Result), lhs));
                break;
            case "mul":
                Output.Add(TFAsmGen.Move(new RegOperand(callingConvention.GetRegister(RegisterRole.MultiplySource1, "Multiply not supported on this target")), GenerateIrOperand(instr.Operands[0])));
                Output.Add(TFAsmGen.Mul(GenerateIrOperand(instr.Operands[1])));
                Output.Add(TFAsmGen.Move(GenerateIrOperand(instr.Result), new RegOperand(callingConvention.GetRegister(RegisterRole.MultiplyResultLow, "Multiply not supported on this target"))));
                break;
            case "div":
                Output.Add(TFAsmGen.Move(new RegOperand(callingConvention.GetRegister(RegisterRole.DivideDividendLow, "Divide not supported on this target")), GenerateIrOperand(instr.Operands[0])));
                Output.Add(TFAsmGen.Div(GenerateIrOperand(instr.Operands[1])));
                Output.Add(TFAsmGen.Move(GenerateIrOperand(instr.Result), new RegOperand(callingConvention.GetRegister(RegisterRole.DivideQuotient, "Divide not supported on this target"))));
                break;
            case "mod":
                Output.Add(TFAsmGen.Move(new RegOperand(callingConvention.GetRegister(RegisterRole.DivideDividendLow, "Divide not supported on this target")), GenerateIrOperand(instr.Operands[0])));
                Output.Add(TFAsmGen.Div(GenerateIrOperand(instr.Operands[1])));
                Output.Add(TFAsmGen.Move(GenerateIrOperand(instr.Result), new RegOperand(callingConvention.GetRegister(RegisterRole.DivideRemainder, "Divide not supported on this target"))));
                break;
        }

        if (lhs is RegOperand reg && org is not RegOperand)
        {
            Output.Add(TFAsmGen.Comment($"[DEBUG] Releasing arithmetic scratch register {reg}"));
            ScratchAllocator.Release(reg);
        }
    }

    private void GenerateIrCompareInstr(IrInstr instr)
    {
        // Output.Add($";   {instr.Instr} with {instr.Operands.Count} operands {string.Join(", ", instr.Operands.Select(GenerateIrOperand))} + {GenerateIrOperand(instr.Result)}");
        AsmOperand dst = GenerateIrOperand(instr.Result);
        AsmOperand operand = GetScratch(dst);

        Output.Add(TFAsmGen.ZeroReg(operand));
        switch (instr.Instructions.ToLower())
        {
            case "eq":
                Output.Add(TFAsmGen.Compare(GenerateIrOperand(instr.Operands[0]), GenerateIrOperand(instr.Operands[1])));
                Output.Add(TFAsmGen.Sete(operand));
                break;
            case "neq":
                Output.Add(TFAsmGen.Compare(GenerateIrOperand(instr.Operands[0]), GenerateIrOperand(instr.Operands[1])));
                Output.Add(TFAsmGen.Setne(operand));
                break;
            case "leq":
                Output.Add(TFAsmGen.Compare(GenerateIrOperand(instr.Operands[0]), GenerateIrOperand(instr.Operands[1])));
                Output.Add(TFAsmGen.Setle(operand));
                break;
            case "geq":
                Output.Add(TFAsmGen.Compare(GenerateIrOperand(instr.Operands[0]), GenerateIrOperand(instr.Operands[1])));
                Output.Add(TFAsmGen.Setge(operand));
                break;
            case "lt":
                Output.Add(TFAsmGen.Compare(GenerateIrOperand(instr.Operands[0]), GenerateIrOperand(instr.Operands[1])));
                Output.Add(TFAsmGen.Setl(operand));
                break;
            case "gt":
                Output.Add(TFAsmGen.Compare(GenerateIrOperand(instr.Operands[0]), GenerateIrOperand(instr.Operands[1])));
                Output.Add(TFAsmGen.Setg(operand));
                break;
        }
        if (operand is RegOperand reg && dst is not RegOperand)
        {
            Output.Add(TFAsmGen.Comment($"[DEBUG] Releasing compare scratch register {reg}"));
            ScratchAllocator.Release(reg);
        }
    }

    private AsmOperand GetScratch(AsmOperand result)
    {
        if (result is RegOperand r)
        {
            return r;
        }
        else
        {
            RegOperand scratch = ScratchAllocator.Allocate();
            Output.Add(TFAsmGen.Comment($"[DEBUG] Allocated scratch register {scratch}"));
            Output.Add(TFAsmGen.Comment(TFAsmGen.Move(scratch, result)));
            return scratch;
        }
    }
    private AsmOperand GetScratch(AsmOperand dst, AsmOperand dstValue, AsmOperand src)
    {
        if (dst is MemOperand && src is MemOperand && dstValue is MemOperand)
        {
            RegOperand scratch = ScratchAllocator.Allocate();
            Output.Add(TFAsmGen.Move(scratch, dstValue));
            return scratch;
        }
        Output.Add(TFAsmGen.Move(dst, dstValue));
        return dst;
    }
}

internal class Variable
{
    public IrTemp Temp;
    public IrLocal Local;

    public string Name { get; set; }
    public int StackOffset { get; internal set; }
    public int AlignedSize { get; set; }
    public int Size { get; set; }
}
