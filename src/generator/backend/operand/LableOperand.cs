public sealed class LableOperand : AsmOperand
{
    public string Base;
    public int Offset;

    public LableOperand(string baseAddress, int offset = 0)
    {
        Base = baseAddress;
        Offset = offset;
    }

    public override string ToString()
    {
        if (Offset == 0)
            return $"{Base}";
        return $"[{Base}{(Offset > 0 ? "+" : "")}{Offset}]";
    }
}
