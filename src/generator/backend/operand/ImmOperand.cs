public sealed class ImmOperand : AsmOperand
{
    public long Value;

    public ImmOperand(long value)
    {
        Value = value;
    }

    public override string GetByteRepresentation()
    {
        return $"byte {Value}";
    }

    public override string GetLongRepresentation()
    {
        return $"qword {Value}";
    }

    public string GetPrefix()
    {
        if (Value <= byte.MaxValue && Value >= byte.MinValue)
        {
            return "byte";
        }
        else if (Value <= ushort.MaxValue && Value >= ushort.MinValue)
        {
            return "word";
        }
        else if (Value <= uint.MaxValue && Value >= uint.MinValue)
        {
            return "dword";
        }
        else
        {
            return "qword";
        }
    }
    public int GetSize()
    {
        if (Value <= byte.MaxValue && Value >= byte.MinValue)
        {
            return 1;
        }
        else if (Value <= ushort.MaxValue && Value >= ushort.MinValue)
        {
            return 2;
        }
        else if (Value <= uint.MaxValue && Value >= uint.MinValue)
        {
            return 4;
        }
        else
        {
            return 8;
        }
    }

    public string ToStringWithoutPrefix()
    {
        return $"{Value}";
    }

    public override string ToString()
    {
        if (Value <= byte.MaxValue && Value >= byte.MinValue)
        {
            return $"byte {Value}";
        }
        else if (Value <= ushort.MaxValue && Value >= ushort.MinValue)
        {
            return $"word {Value}";
        }
        else if (Value <= uint.MaxValue && Value >= uint.MinValue)
        {
            return $"dword {Value}";
        }
        else
        {
            return $"qword {Value}";
        }
    }

    public string ToStringFromSize(int size)
    {
        switch (size)
        {
            case 1: return $"byte {Value}";
            case 2: return $"word {Value}";
            case 4: return $"dword {Value}";
            case 8: return $"qword {Value}";
            default: throw new Exception("invalid size");
        }
    }
}
