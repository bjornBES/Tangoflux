public sealed class LabelOperand : AsmOperand
{
    public string Base;
    public int Offset;

    public LabelOperand(string baseAddress, int offset = 0)
    {
        Base = baseAddress;
        Offset = offset;
    }

    public override string GetByteRepresentation()
    {
        return ToString();
    }

    public override string GetLongRepresentation()
    {
        return ToString();
    }

    public override string ToString()
    {
        if (Offset == 0)
            return $"{Base}";
        return $"[{Base}{(Offset > 0 ? "+" : "")}{Offset}]";
    }
    public string ToStringWithPrefix()
    {
        if (Offset == 0)
        {
            return ToString();
        }
        return $"[rel {Base}]";
    }
}
