public sealed class MemRegOperand : AsmOperand
{
    public RegisterInfo Base;
    public RegisterInfo Offset;
    public int Size;

    public MemRegOperand(RegisterInfo baseReg, int size, RegisterInfo offset)
    {
        Base = baseReg;
        Offset = offset;
        Size = size;
    }

    public override string GetByteRepresentation()
    {
        return ToString();
    }

    public override string GetLongRepresentation()
    {
        return ToString();
    }

    public string ToStringWithoutPrefix()
    {
        if (Offset == null)
        {
            return $"[{Base.Name}]";
        }
        else
        {
            return $"[{Base.Name}+{Offset}]";
        }
    }

    public override string ToString()
    {
        if (Offset == null)
        {
            return $"{SizePrefix()} [{Base.Name}]";
        }
        else
        {
            return $"{SizePrefix()} [{Base.Name}+{Offset}]";
        }
    }

    string SizePrefix()
    {
        switch (Size)
        {
            case 1: return "byte";
            case 2: return "word";
            case 4: return "dword";
            case 8: return "qword";
            default: throw new Exception("invalid size");
        }
    }

}
