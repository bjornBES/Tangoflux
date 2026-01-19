public enum RegisterFunction
{
    BasePointer,
    StackPointer,
    ProgramPointer,

    SyscallNumber,

    ReturnInt,
    Return128L,
    Return128H,
    ReturnFloat,
    ReturnDouble,

    // long-lived temp
    GeneralPurpose,
    // short-lived, clobberable
    Scratch,
    // for register allocator
    Spill,

    BinaryLeft,
    BinaryRight,
    BinaryResult,

    AddressBase,
    AddressIndex,

    Arg0,
    Arg1,
    Arg2,
    Arg3,
    Arg4,
    Arg5,


    // Fuck you x86
    MultiplyResultLow,
    MultiplyResultHigh,
    MultiplySource1,

    DivideDividendLow,
    DivideDividendHigh,

    DivideQuotient,
    DivideRemainder,
}
