using TangoFlexCompiler.Generator.Backend.CallingConventions.Registers;

namespace TangoFlexCompiler.Generator.Backend.CallingConventions
{
    public class ArgumentRule
    {
        public RegisterClass AcceptedClass;   // GP / Float / Vector
        public PhysicalRegister Register;      // rdi, xmm0, etc
    }
}