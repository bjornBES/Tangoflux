public enum RegisterRole
{
    // Generic
    GeneralPurpose,
    AddressBase,
    AddressIndex,

    // ABI
    ReturnLow,
    ReturnHigh,
    SyscallNumber,
    ReturnFloat,
    ReturnDouble,
    VectorReturn,

    // Special
    StackPointer,
    BasePointer,
    ProgramCounter,


    // Fuck you x86
    MultiplySource1,
    MultiplyResultLow,
    MultiplyResultHigh,

    DivideDividendLow,
    DivideDividendHigh,
    DivideQuotient,
    DivideRemainder,
}
