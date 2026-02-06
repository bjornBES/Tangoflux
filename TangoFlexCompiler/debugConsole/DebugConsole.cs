using Common;

namespace TangoFlexCompiler.debugConsole
{
    public static class DebugConsole
    {
        static Arguments Arguments;
        public static void InitDebugging(Arguments arguments)
        {
            Arguments = arguments;
        }
        public static void WriteLine(string line)
        {
            if (!Arguments.disableDebugPrinting)
            {
                Console.WriteLine(line);
            }
        }
    }
}