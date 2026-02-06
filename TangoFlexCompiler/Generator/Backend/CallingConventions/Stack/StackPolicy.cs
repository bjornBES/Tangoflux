namespace TangoFlexCompiler.Generator.Backend.CallingConventions.Stack
{
    public class StackPolicy
    {
        public int Alignment;
        public StackCleanup Cleanup;
        public bool RedZone;   // SysV yes, Win64 no
    }
}