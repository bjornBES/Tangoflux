using System.Collections.Generic;

public class X86_32_SysV : ICallingConvention
{
    public int StackAlignment { get; set; }
    private readonly Dictionary<RegisterFunction, RegisterInfo> funcToReg =
        new Dictionary<RegisterFunction, RegisterInfo>()
    {
        { RegisterFunction.BasePointer, new RegisterInfo("ebp", 0) },
        { RegisterFunction.StackPointer, new RegisterInfo("esp", 1) },
        { RegisterFunction.ProgramPointer, new RegisterInfo("eip", 2) },

        { RegisterFunction.SyscallNumber, new RegisterInfo("eax", 3) },
        { RegisterFunction.SyscallArg0,   new RegisterInfo("edi", 4) },
        { RegisterFunction.SyscallArg1,   new RegisterInfo("esi", 5) },
        { RegisterFunction.SyscallArg2,   new RegisterInfo("edx", 6) },

        { RegisterFunction.SyscallReturn64, new RegisterInfo("eax", 3) },
    };

    public RegisterInfo GetRegister(RegisterFunction f) => funcToReg[f];
    public string GetRegisterName(RegisterInfo r) => r.Name;
}
