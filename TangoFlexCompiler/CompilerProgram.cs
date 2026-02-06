using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using BjornBEs.Libs.EasyArgs;
using Common;
using TangoFlexCompiler.debugConsole;
using TangoFlexCompiler.Generator;
using TangoFlexCompiler.IRParser;
using TangoFlexCompiler.Lexer;
using TangoFlexCompiler.Parser;
using TangoFlexCompiler.Preprocessor;
using TangoFlexCompiler.SymbolParser;

namespace TangoFlexCompiler
{
    public class CompilerProgram
    {
        public static Arguments Arguments;
        public static CompilerState InitializeCompiler(Arguments arguments)
        {
            CompilerState compilerState = new CompilerState();
            Arguments = arguments;
            DebugConsole.InitDebugging(Arguments);

            compilerState.TangoFlexPreprocessor = new TangoFlexPreprocessor(Arguments);
            compilerState.TangoFlexLexer = new TangoFlexLexer(Arguments);
            compilerState.TangoFlexPreprocessorEval = new TangoFlexPreprocessorEval(compilerState.TangoFlexLexer, compilerState.TangoFlexPreprocessor, Arguments);
            compilerState.TangoFlexParser = new TangoFlexParser(Arguments);
            compilerState.TangoFlexSymbol = new TangoFlexSymbolParser(Arguments);
            compilerState.TangoFlexIRParser = new TangoFlexIRParser(Arguments);
            compilerState.TangoFlexGenerator = new TangoFlexGenerator(Arguments);
            return compilerState;
        }

        public static void Main(string[] args)
        {
            Arguments arguments = EasyArgs.Parse<Arguments>(args);
            CompilerState compilerState = InitializeCompiler(arguments);
            string src = File.ReadAllText(arguments.InputFile);
            compilerState.Build(src, arguments);
        }
    }
}