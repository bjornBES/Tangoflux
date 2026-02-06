using System.Collections.Generic;

/*
public class X86_64_Cdecl : ICallingConvention
{
    public string Name { get; set; } = "cdecl";
    public bool UseRegisters { get; set; } = false;
    public int StackAlignment { get; set; }
    private readonly Dictionary<RegisterRole, RegisterInfo> funcToReg =
        new Dictionary<RegisterRole, RegisterInfo>()
    {
        { RegisterRole.BasePointer, new RegisterInfo("rbp", 0) },
        { RegisterRole.StackPointer, new RegisterInfo("rsp", 1) },
        { RegisterRole.ProgramPointer, new RegisterInfo("rip", 2) },

        { RegisterRole.SyscallNumber, new RegisterInfo("rax", 3) },
        { RegisterRole.Arg0,   new RegisterInfo("rbx", 4) },
        { RegisterRole.Arg1,   new RegisterInfo("rcx", 5) },
        { RegisterRole.Arg2,   new RegisterInfo("rdx", 6) },
        { RegisterRole.Arg3,   new RegisterInfo("rsi", 7) },
        { RegisterRole.Arg4,   new RegisterInfo("rdi", 8) },

        { RegisterRole.ReturnInt, new RegisterInfo("rax", 3) },
        // No SyscallArg5 in 32-bit Linux ABI
    };

    public RegisterInfo GetRegister(RegisterRole f) => funcToReg[f];
    public string GetRegisterName(RegisterInfo r) => r.Name;
}

*/