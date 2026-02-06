
using TangoFlexCompiler.Generator.Backend.CallingConventions.Registers;

namespace TangoFlexCompiler.Generator.Backend.CallingConventions
{
    public class ReturnRule
    {
        public RegisterClass Class;
        public PhysicalRegister Low;
        public PhysicalRegister High; // optional (for i128)
    }
}