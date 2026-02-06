using System.Text;
using BjornBEs.Libs.EasyArgs;
using Common;
using TangoFlexCompiler;

namespace TangoFlexLSP
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            LSPArguments lspArguments = EasyArgs.Parse<LSPArguments>(args);
            Arguments arguments = new Arguments()
            {
                InputFile = "",
                UseLSP = lspArguments.UseLSP,
                json = lspArguments.json,
                debug = lspArguments.debug,
                WriteDebugFiles = false,
            };
            CompilerState compilerState = CompilerProgram.InitializeCompiler(arguments);

            LspServer lspServer = new LspServer(Console.OpenStandardInput(), Console.OpenStandardOutput(), compilerState);
            lspServer.Run();
            Console.Error.WriteLine("exit");
        }
    }
}