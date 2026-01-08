public sealed class MemOperand : AsmOperand
{
    public RegisterInfo Base;
    public int Offset;

    public MemOperand(RegisterInfo baseReg, int offset = 0)
    {
        Base = baseReg;
        Offset = offset;
    }

    public override string ToString()
    {
        if (Offset == 0)
            return $"[{Base.Name}]";
        return $"[{Base.Name}{(Offset > 0 ? "+" : "")}{Offset}]";
    }
}
