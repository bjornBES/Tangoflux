namespace TangoFlexCompiler.Generator.Backend.Operand
{
    public sealed class ImmOperand : AsmOperand
    {
        public IrType Type { get; set; }
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
            long intValue = Convert.ToInt64(Value);
            return $"0x{Convert.ToString(intValue, 16)}";
        }

        public override string ToString()
        {
            if (Value <= byte.MaxValue && Value >= byte.MinValue)
            {
                sbyte intValue = Convert.ToSByte(Value);
                return $"byte 0x{Convert.ToString(intValue, 16)}";
            }
            else if (Value <= ushort.MaxValue && Value >= ushort.MinValue)
            {
                short intValue = Convert.ToInt16(Value);
                return $"word 0x{Convert.ToString(intValue, 16)}";
            }
            else if (Value <= uint.MaxValue && Value >= uint.MinValue)
            {
                int intValue = Convert.ToInt32(Value);
                return $"dword 0x{Convert.ToString(intValue, 16)}";
            }
            else
            {
                long intValue = Convert.ToInt64(Value);
                return $"qword 0x{Convert.ToString(intValue, 16)}";
            }
        }

        public string ToStringFromSize(int size)
        {
            long intValue = Convert.ToInt64(Value);
            switch (size)
            {
                case 1: return $"byte 0x{Convert.ToString(intValue & 0xFF, 16)}";
                case 2: return $"word 0x{Convert.ToString(intValue & 0xFFFF, 16)}";
                case 4: return $"dword 0x{Convert.ToString(intValue & 0xFFFFFFFF, 16)}";
                case 8: return $"qword 0x{Convert.ToString(intValue, 16)}";
                default: throw new Exception("invalid size");
            }
        }
    }
}