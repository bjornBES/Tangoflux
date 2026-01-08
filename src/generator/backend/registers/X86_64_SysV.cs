using System.Collections.Generic;

public class X86_64_SysV : ICallingConvention
{
    public int StackAlignment { get; set; } = 16;
    private readonly Dictionary<RegisterFunction, RegisterInfo> funcToReg =
        new Dictionary<RegisterFunction, RegisterInfo>()
    {
        // Special
        { RegisterFunction.BasePointer,     new RegisterInfo("rbp", 0) },
        { RegisterFunction.StackPointer,    new RegisterInfo("rsp", 1) },
        { RegisterFunction.ProgramPointer,  new RegisterInfo("rip", 2) },
    
        // Return / Syscall
        { RegisterFunction.SyscallNumber,   new RegisterInfo("rax", 3) },
        { RegisterFunction.ReturnInt,       new RegisterInfo("rax", 3) },
        { RegisterFunction.Return128L,      new RegisterInfo("rax", 3) },
        { RegisterFunction.Return128H,      new RegisterInfo("rdx", 6) },
        { RegisterFunction.ReturnFloat,     new RegisterInfo("xmm0", 0) },
        { RegisterFunction.ReturnDouble,    new RegisterInfo("xmm0", 0) },
    
        // Function arguments (SysV)
        { RegisterFunction.Arg0,            new RegisterInfo("rdi", 4) },
        { RegisterFunction.Arg1,            new RegisterInfo("rsi", 5) },
        { RegisterFunction.Arg2,            new RegisterInfo("rdx", 6) },
        { RegisterFunction.Arg3,            new RegisterInfo("rcx", 7) },
        { RegisterFunction.Arg4,            new RegisterInfo("r8",  8) },
        { RegisterFunction.Arg5,            new RegisterInfo("r9",  9) },
    
        // Binary op / scratch / general-purpose (caller-saved)
        { RegisterFunction.Scratch,         new RegisterInfo("rax", 3) }, // start of pool
        { RegisterFunction.GeneralPurpose,  new RegisterInfo("rbx", 11) }, // callee-saved GP
        { RegisterFunction.BinaryLeft,      new RegisterInfo("rcx", 7) },
        { RegisterFunction.BinaryRight,     new RegisterInfo("rdx", 6) },
        { RegisterFunction.BinaryResult,    new RegisterInfo("rax", 3) },
    
        // Addressing
        { RegisterFunction.AddressBase,     new RegisterInfo("rbp", 0) },
        { RegisterFunction.AddressIndex,    new RegisterInfo("rsi", 5) },
    };

    public RegisterInfo GetRegister(RegisterFunction f) => funcToReg[f];
    public string GetRegisterName(RegisterInfo r) => r.Name;
}
