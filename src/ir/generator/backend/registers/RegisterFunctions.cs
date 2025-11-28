public enum RegisterFunction
{
    BasePointer,
    StackPointer,
    ProgramPointer,

    SyscallNumber,
    SyscallArg0,
    SyscallArg1,
    SyscallArg2,
    SyscallArg3,
    SyscallArg4,
    SyscallArg5,

    SyscallReturn64,
    SyscallReturn128L,
    SyscallReturn128H,
    SyscallReturnFloat,
    SyscallReturnDouble,
}
