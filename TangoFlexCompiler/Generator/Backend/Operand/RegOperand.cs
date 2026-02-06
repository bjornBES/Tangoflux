using TangoFlexCompiler.Generator.Backend.Registers;

namespace TangoFlexCompiler.Generator.Backend.Operand
{
    public sealed class RegOperand : AsmOperand
    {
        public RegisterInfo Register;

        public RegOperand(RegisterInfo reg)
        {
            Register = reg;
        }

        public RegOperand GetByte()
        {
            return new RegOperand(X86Registers.GetRegisterByte(Register));
        }

        public override string GetByteRepresentation()
        {
            return new RegOperand(X86Registers.GetRegisterByte(Register)).Register.Name;
        }

        public override string GetLongRepresentation()
        {
            return new RegOperand(X86Registers.GetRegisterLong(Register)).Register.Name;
        }

        public RegOperand GetRegisterOpBySize(int size)
        {
            switch (size)
            {
                case 1: return new RegOperand(X86Registers.GetRegisterByte(Register));
                case 2: return new RegOperand(X86Registers.GetRegisterWord(Register));
                case 4: return new RegOperand(X86Registers.GetRegisterDword(Register));
                case 8: return new RegOperand(X86Registers.GetRegisterLong(Register));
                default: throw new Exception("invalid size");
            }
        }
        public int GetSize()
        {
            if (X86Registers.IsByteRegister(Register))
                return 1;
            if (X86Registers.IsWordRegister(Register))
                return 2;
            if (X86Registers.IsDwordRegister(Register))
                return 4;
            if (X86Registers.IsLongRegister(Register))
                return 8;
            throw new Exception("invalid register size");
        }

        public string GetPrefix(int size)
        {
            switch (size)
            {
                case 1: return "byte";
                case 2: return "word";
                case 4: return "dword";
                case 8: return "qword";
                default: throw new Exception("invalid size");
            }
        }
        public string GetRegisterBySize(int size)
        {
            switch (size)
            {
                case 1: return X86Registers.GetRegisterByte(Register).Name;
                case 2: return X86Registers.GetRegisterWord(Register).Name;
                case 4: return X86Registers.GetRegisterDword(Register).Name;
                case 8: return X86Registers.GetRegisterLong(Register).Name;
                default: throw new Exception("invalid size");
            }
        }

        public override string ToString() => Register.Name;
    }
}