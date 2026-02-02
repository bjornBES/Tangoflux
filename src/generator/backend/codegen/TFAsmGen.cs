
using System.Net.NetworkInformation;

public enum SectionStatus
{
    NONE,
    TEXT,
    DATA,
    RODATA,
    BSS,
}

public class TFAsmGen
{
    ICallingConvention CallingConvention;
    SectionStatus status = SectionStatus.NONE;
    private FunctionFrame currentFrame;
    private RegisterTracker currentRegisterTracker;
    public ScratchRegisterAllocator RegisterAllocator;

    public TFAsmGen(ICallingConvention callingConvention, ScratchRegisterAllocator scratchRegister)
    {
        CallingConvention = callingConvention;
        RegisterAllocator = scratchRegister;
    }

    public string Text()
    {
        if (status != SectionStatus.TEXT)
        {
            status = SectionStatus.TEXT;
            return ".text";
        }
        return "";
    }

    public string Data()
    {
        if (status != SectionStatus.DATA)
        {
            status = SectionStatus.DATA;
            return ".data";
        }
        return "";
    }
    public string Rodata()
    {
        if (status != SectionStatus.RODATA)
        {
            status = SectionStatus.RODATA;
            return ".rodata";
        }
        return "";
    }
    public string Bss()
    {
        if (status != SectionStatus.BSS)
        {
            status = SectionStatus.BSS;
            return ".bss";
        }
        return "";
    }

    public string Global(string name) => $".global {name}";
    public string Local(string name) => $".local {name}";
    public string Align(int align) => $".align {align}";
    public string Comm(string name, int size, int align) => $".comm {name},{size},{align}";
    public string Byte(int value) => $".byte {value}";
    public string Zero(int size) => $".zero {size}";
    public string Value(int value) => $".value {value}";
    public string Long(int value) => $".qword {value}";
    public string Comment(string comment) => $"    ; {comment}";

    public FunctionFrame EnterFunction(string name, int locals, ICallingConvention convention, RegisterProfile profile)
    {
        FunctionFrame frame = new FunctionFrame
        {
            FunctionName = name,
            LocalSize = locals,
            StackAlignment = convention.Stack.Alignment,
            UseFramePointer = true // or decide later
        };

        currentFrame = frame;
        currentRegisterTracker = new RegisterTracker(frame, convention, profile);

        return frame;
    }

    public string LeaveFunction(string name, int locals, ICallingConvention convention, RegisterProfile profile)
    {
        FunctionFrame functionFrame = new FunctionFrame()
        {
            FunctionName = name,
            LocalSize = locals,
            UseFramePointer = true,
            StackAlignment = 16
        };
        functionFrame.CalleeSavedUsed.Add(new RegisterInfo("rbx", 0));
        functionFrame.CalleeSavedUsed.Add(new RegisterInfo("r12", 12));
        return PrologueEpilogueGenerator.GenerateEpilogue(functionFrame, convention, profile);
    }

    public string Move(AsmOperand dst, AsmOperand src, int forceSize = -1)
    {
        if (dst is RegOperand r)
        {
            currentRegisterTracker.Use(r.Register);
        }

        // MemOperand, MemOperand
        // RegOperand, RegOperand
        // MemOperand, PtrOperand
        // MemOperand, AsmOperand
        // [rel LabelOperand], AsmOperand
        // AsmOperand, [rel LabelOperand]
        // AsmOperand, ImmOperand.ToStringWithoutPrefix()

        if (src is MemOperand srcMem && dst is MemOperand dstMem)
        {
            RegOperand tempReg = RegisterAllocator.Allocate();
            if (tempReg == null)
            {
                throw new InvalidOperationException("");
            }
            currentRegisterTracker.Use(tempReg.Register);
            RegOperand srcReg = new RegOperand(tempReg.Register).GetRegisterOpBySize(dstMem.Size);
            string code = $"    mov {srcReg}, {srcMem}{Environment.NewLine}" +
                   $"    mov {dstMem}, {srcReg}";
            RegisterAllocator.Release(tempReg);
            return code;
        }
        if (dst is RegOperand reg && src is ImmOperand imm1)
        {
            if (imm1.Value == 0)
            {
                return ZeroReg(reg);
            }
            return $"    mov {reg}, {imm1.ToStringWithoutPrefix()}";
        }
        else if (dst is MemOperand mem && src is ImmOperand imm2)
        {
            return $"    mov {mem}, {imm2.ToStringWithoutPrefix()}";
        }
        else if (dst is RegOperand dstReg && src is RegOperand srcReg)
        {
            // check if they are the same register
            if (dstReg.GetRegisterOpBySize(8).Register == srcReg.GetRegisterOpBySize(8).Register)
            {
                int dstRegSize = dstReg.GetSize();
                int srcRegSize = srcReg.GetSize();
                if (srcRegSize < dstRegSize)
                {
                    // TODO: add signed
                    if (srcRegSize == 32 && dstRegSize == 64)
                    {
                        return $"    mov {dstReg}, {srcReg}";
                    }
                    return $"    movzx {dstReg}, {srcReg}";
                }
            }
            return $"    mov {dstReg}, {srcReg.GetRegisterBySize(dstReg.GetSize())}";
        }
        else if (dst is MemOperand mem4 && src is PtrOperand srcPtr1)
        {
            return $"    mov {mem4}, qword {srcPtr1}";
        }
        else if (dst is MemOperand mem3)
        {
            return $"    mov {mem3.ToStringWithoutPrefix()}, {src}";
        }
        else if (dst.IsType(out LabelOperand label))
        {
            return $"    mov {label.ToStringWithPrefix()}, {src}";
        }

        return $"    mov {dst}, {src}";
    }

    public string Lea(AsmOperand dst, AsmOperand src)
    {
        RegOperand reg;
        if (src.IsType(out reg))
        {
            return $"    lea {dst}, [{reg}]";
        }
        return $"    lea {dst}, {src}";
    }

    public string ZeroReg(AsmOperand src)
    {
        if (src is RegOperand r)
        {
            currentRegisterTracker.Use(r.Register);
            return $"    xor {src.GetLongRepresentation()}, {src.GetLongRepresentation()}";
        }
        return "   ; Invalid operand for zeroing register";
    }

    public string Compare(AsmOperand src1, AsmOperand src2)
    {
        string cmpInstr = "";
        if (src1 is RegOperand r && src2 is ImmOperand imm)
        {
            cmpInstr = $"    cmp {r.GetRegisterBySize(imm.GetSize())}, {imm}";
        }
        else if (src1 is MemOperand mem && src2 is ImmOperand imm1)
        {
            cmpInstr = $"    cmp {mem}, {imm1.ToStringWithoutPrefix()}";
        }
        else
        {
            cmpInstr = $"    cmp {src1}, {src2}";
        }
        return $"{cmpInstr}";
    }
    public string Test(AsmOperand src)
    {
        if (src is RegOperand reg)
        {
            return $"    test {reg}, {reg}";
        }
        else if (src is MemOperand mem)
        {
            return $"    test {mem}, {mem}";
        }
        else
        {
            return $"    test {src}, {src}";
        }
    }
    public string Add(AsmOperand dst, AsmOperand src)
    {
        if (dst is RegOperand r)
        {
            currentRegisterTracker.Use(r.Register);
        }
        if (dst is ImmOperand _ || (dst is MemOperand && src is MemOperand))
        {
            return "   ; Invalid operand for add";
        }
        if (dst is MemOperand mem && src is ImmOperand imm)
        {
            return $"    add {mem}, {imm.ToStringFromSize(mem.Size)}";
        }
        else if (src is ImmOperand imm1)
        {
            return $"    add {dst}, {imm1}";
        }
        return $"    add {dst}, {src}";
    }
    public string Sub(AsmOperand dst, AsmOperand src)
    {
        if (dst is RegOperand r)
        {
            currentRegisterTracker.Use(r.Register);
        }
        if (dst is ImmOperand _ or MemOperand)
        {
            return "   ; Invalid operand for sub";
        }
        
        RegOperand reg;
        ImmOperand imm;

        if (src.IsType(out imm) && dst.IsType(out reg))
        {
            return $"    sub {reg}, {reg.GetPrefix(reg.GetSize())} {imm.ToStringWithoutPrefix()}";
        }
        else if (src is MemOperand)
        {
            return $"    sub {dst}, qword {src}";
        }
        return $"    sub {dst}, {src}";
    }

    public string Mul(AsmOperand src)
    {
        currentRegisterTracker.Use(CallingConvention.GetRegister(RegisterRole.MultiplyResultLow, "Multiply not supported on this target"));
        currentRegisterTracker.Use(CallingConvention.GetRegister(RegisterRole.MultiplyResultHigh, "Multiply not supported on this target"));

        if (src is RegOperand r)
        {
            currentRegisterTracker.Use(r.Register);
            return $"    mul {src}";
        }
        return "   ; Invalid operand for mul";
    }
    public string Div(AsmOperand src)
    {
        currentRegisterTracker.Use(CallingConvention.GetRegister(RegisterRole.DivideDividendLow, "Divide"));
        currentRegisterTracker.Use(CallingConvention.GetRegister(RegisterRole.DivideDividendHigh, "Divide"));

        if (src is RegOperand r)
        {
            currentRegisterTracker.Use(r.Register);
            return $"    div {src}";
        }
        return "   ; Invalid operand for div";
    }
    public string Sete(AsmOperand dst)
    {
        if (dst is RegOperand r)
        {
            currentRegisterTracker.Use(r.Register);
            return $"    sete {r.GetByte()}";
        }
        return "   ; Invalid operand for sete";
    }
    public string Setne(AsmOperand dst)
    {
        if (dst is RegOperand r)
        {
            currentRegisterTracker.Use(r.Register);
            return $"    setne {r.GetByte()}";
        }
        return "   ; Invalid operand for setne";
    }
    public string Setle(AsmOperand dst)
    {
        if (dst is RegOperand r)
        {
            currentRegisterTracker.Use(r.Register);
            return $"    setle {r.GetByte()}";
        }
        return "   ; Invalid operand for setle";
    }
    public string Setge(AsmOperand dst)
    {
        if (dst is RegOperand r)
        {
            currentRegisterTracker.Use(r.Register);
            return $"    setge {r.GetByte()}";
        }
        return "   ; Invalid operand for setge";
    }
    public string Setl(AsmOperand dst)
    {
        if (dst is RegOperand r)
        {
            currentRegisterTracker.Use(r.Register);
            return $"    setl {r.GetByte()}";
        }
        return "   ; Invalid operand for setle";
    }
    public string Setg(AsmOperand dst)
    {
        if (dst is RegOperand r)
        {
            currentRegisterTracker.Use(r.Register);
            return $"    setg {r.GetByte()}";
        }
        return "   ; Invalid operand for setge";
    }

    public string Push(AsmOperand src)
    {
        return $"    push {src}";
    }

    public string Jump(AsmOperand src)
    {
        string address = "";
        if (src is RegOperand r)
        {

        }
        else if (src is MemOperand mem)
        {

        }
        else if (src is LabelOperand label)
        {
            address = label.ToString();
        }

        return $"    jmp {address}";
    }

    public string JumpIfEqual(AsmOperand src)
    {
        string address = "";
        if (src is RegOperand r)
        {

        }
        else if (src is MemOperand mem)
        {

        }
        else if (src is LabelOperand label)
        {
            address = label.ToString();
        }

        return $"    je {address}";
    }

    public string JumpIfNotEqual(AsmOperand src)
    {
        string address = "";
        if (src is RegOperand r)
        {

        }
        else if (src is MemOperand mem)
        {

        }
        else if (src is LabelOperand label)
        {
            address = label.ToString();
        }

        return $"    jne {address}";
    }
    public string JumpIfZero(AsmOperand src)
    {
        string address = "";
        if (src is RegOperand r)
        {

        }
        else if (src is MemOperand mem)
        {

        }
        else if (src is LabelOperand label)
        {
            address = label.ToString();
        }

        return $"    jz {address}";
    }
    public string JumpIfNotZero(AsmOperand src)
    {
        string address = "";
        if (src is RegOperand r)
        {

        }
        else if (src is MemOperand mem)
        {

        }
        else if (src is LabelOperand label)
        {
            address = label.ToString();
        }

        return $"    jnz {address}";
    }

    public string SystemCall(AsmOperand intNumber)
    {
        if (!CallingConvention.TryGetRole(RegisterRole.SyscallNumber, out PhysicalRegister intNumberReg))
        {
            
        }
        if (RegisterAllocator.GetUnavailable().Contains(intNumberReg))
        {
            RegisterAllocator.Release(new RegOperand(intNumberReg.GetInfo()));
        }
        string lines = Move(new RegOperand(intNumberReg.GetInfo()), intNumber) + Environment.NewLine;
        currentRegisterTracker.Use(intNumberReg.GetInfo());
        // TODO: change for other arch/OS
        lines += "    syscall";
        return lines;
    }

    public string Call(AsmOperand dst)
    {
        return $"    call {dst}";
    }

    public string Label(string name)
    {
        return $"{name}:";
    }
}