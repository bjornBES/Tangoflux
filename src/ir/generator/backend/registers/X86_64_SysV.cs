using System.Collections.Generic;

public class X86_64_SysV : ICallingConvention
{
    public int StackAlignment { get; set; }
    private readonly Dictionary<RegisterFunction, RegisterInfo> funcToReg =
        new Dictionary<RegisterFunction, RegisterInfo>()
    {
        { RegisterFunction.BasePointer, new RegisterInfo("rbp", 0) },
        { RegisterFunction.StackPointer, new RegisterInfo("rsp", 1) },
        { RegisterFunction.ProgramPointer, new RegisterInfo("rip", 2) },

        { RegisterFunction.SyscallNumber, new RegisterInfo("rax", 3) },
        { RegisterFunction.SyscallArg0,   new RegisterInfo("rdi", 4) },
        { RegisterFunction.SyscallArg1,   new RegisterInfo("rsi", 5) },
        { RegisterFunction.SyscallArg2,   new RegisterInfo("rdx", 6) },
        { RegisterFunction.SyscallArg3,   new RegisterInfo("r10", 7) },
        { RegisterFunction.SyscallArg4,   new RegisterInfo("r8",  8) },
        { RegisterFunction.SyscallArg5,   new RegisterInfo("r9",  9) },

        { RegisterFunction.SyscallReturn64, new RegisterInfo("rax", 3) },
    };

    public RegisterInfo GetRegister(RegisterFunction f) => funcToReg[f];
    public string GetRegisterName(RegisterInfo r) => r.Name;
}
