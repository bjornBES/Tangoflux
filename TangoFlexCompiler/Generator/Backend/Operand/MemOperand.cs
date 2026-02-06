using TangoFlexCompiler.Generator.Backend.Registers;

namespace TangoFlexCompiler.Generator.Backend.Operand
{
    public sealed class MemOperand : AsmOperand
    {
        public RegisterInfo Base;
        public int Offset;
        public int Size;

        public MemOperand(RegisterInfo baseReg, int size, int offset = 0)
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
            if (Offset == 0)
                return $"[{Base.Name}]";
            return $"[{Base.Name}{(Offset > 0 ? "+" : "")}{Offset}]";
        }

        public override string ToString()
        {
            if (Offset == 0)
                return $"{SizePrefix()} [{Base.Name}]";
            return $"{SizePrefix()} [{Base.Name}{(Offset > 0 ? "+" : "")}{Offset}]";
        }

        public string SizePrefix()
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
}