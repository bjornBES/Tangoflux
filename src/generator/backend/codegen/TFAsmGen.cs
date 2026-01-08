
public enum SectionStatus
{
    NONE,
    TEXT,
    DATA
}

public class TFAsmGen
{
    ICallingConvention CallingConvention;
    SectionStatus status = SectionStatus.NONE;
    private FunctionFrame currentFrame;
    private RegisterTracker currentRegisterTracker;

    public TFAsmGen(ICallingConvention callingConvention)
    {
        CallingConvention = callingConvention;
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

    public string Globl(string name) => $".global {name}";
    public string Local(string name) => $".local {name}";
    public string Align(int align) => $".align {align}";
    public string Comm(string name, int size, int align) => $".comm {name},{size},{align}";
    public string Byte(int value) => $".byte {value}";
    public string Zero(int size) => $".zero {size}";
    public string Value(int value) => $".value {value}";
    public string Long(int value) => $".long {value}";

    public FunctionFrame EnterFunction(string name, int locals, ICallingConvention conv, RegisterProfile profile)
    {
        FunctionFrame frame = new FunctionFrame
        {
            FunctionName = name,
            LocalSize = locals,
            StackAlignment = conv.StackAlignment,
            UseFramePointer = true // or decide later
        };

        currentFrame = frame;
        currentRegisterTracker = new RegisterTracker(frame, conv, profile);

        return frame;
    }

    public string LeaveFunction(string name, int locals, ICallingConvention conv, RegisterProfile profile)
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
        return PrologueEpilogueGenerator.GenerateEpilogue(functionFrame, conv, profile);
    }

    public string Move(AsmOperand dst, AsmOperand src)
    {
        if (dst is RegOperand r)
        {
            currentRegisterTracker.Use(r.Register);
        }

        return $"    mov {dst}, {src}";
    }

    public string ZeroReg(RegOperand src)
    {
        return $"    xor {src}, {src}";
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
        else if (src is LableOperand lable)
        {
            address = lable.ToString();
        }
        
        return $"    jmp {address}";
    }
}