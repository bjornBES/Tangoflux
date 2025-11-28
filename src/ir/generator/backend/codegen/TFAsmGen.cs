
public enum SectionStatus
{
    NONE,
    TEXT,
    DATA
}

public class TFAsmGen
{
    ICallingConvention CallingConvention;
    public TFAsmGen(ICallingConvention callingConvention)
    {
        CallingConvention = callingConvention;
    }
    SectionStatus status = SectionStatus.NONE;
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

    public string EnterFunction(string name, int locals, ICallingConvention conv, RegisterProfile profile)
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

        return PrologueEpilogueGenerator.GeneratePrologue(functionFrame, conv, profile);
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
}