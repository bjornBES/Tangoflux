using System.Text.Json;
using System.Text.Json.Serialization;
using Common;
using TangoFlexCompiler.Generator;
using TangoFlexCompiler.IRParser;
using TangoFlexCompiler.Lexer;
using TangoFlexCompiler.Parser;
using TangoFlexCompiler.Parser.Nodes;
using TangoFlexCompiler.Preprocessor;
using TangoFlexCompiler.SymbolParser;

namespace TangoFlexCompiler
{
    public class CompilerState
    {
        public TangoFlexPreprocessor TangoFlexPreprocessor;
        public TangoFlexLexer TangoFlexLexer;
        public TangoFlexPreprocessorEval TangoFlexPreprocessorEval;
        public TangoFlexParser TangoFlexParser;
        public TangoFlexSymbolParser TangoFlexSymbol;
        public TangoFlexIRParser TangoFlexIRParser;
        public TangoFlexGenerator TangoFlexGenerator;

        public void BuildFile(string src, string file, out Token[] tokens, out NodeProg nodeProg, out SymbolParser.Symbol[] table)
        {
            tokens = TangoFlexLexer.Lex(src, file).ToArray();
            TangoFlexParser.Process(tokens);
            nodeProg = TangoFlexParser.AST;
            table = TangoFlexSymbol.GetTable(nodeProg);
        }

        public string Build(string src, Arguments arguments)
        {
            // full compilation w dumping
            TangoFlexPreprocessor.Process(src);
            TangoFlexLexer.Process(src);
            if (arguments.UseLSP)
            {
                if (arguments.dumpTokens == true && arguments.json == true)
                {
                    arguments.disableDebugPrinting = true;
                    arguments.debug = false;

                    JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
                    {
                        MaxDepth = 64,
                        ReferenceHandler = ReferenceHandler.IgnoreCycles
                    };
                    serializerOptions.Converters.Add(new JsonStringEnumConverter());
                    string json = JsonSerializer.Serialize(TangoFlexLexer.Tokens, serializerOptions);

                    Console.Out.WriteLine(json);
                }
            }
            // later if we don't want the preprocessor to delete #if and so on
            // it also makes it faster
            Token[] tokens;
            if (arguments.UseLSP)
            {
                tokens = TangoFlexLexer.Tokens.ToArray();
            }
            else
            {
                TangoFlexPreprocessorEval.Process(TangoFlexLexer.Tokens.ToArray());
                tokens = TangoFlexPreprocessorEval.tokenOut;
            }

            if (arguments.UseLSP)
            {
                // print AST out
            }

            TangoFlexParser.Process(tokens);
            {
                // Print Table out
            }

            TangoFlexSymbol.ExtractSymbols(TangoFlexParser.AST);
            TangoFlexIRParser.Process(TangoFlexParser.AST);
            TangoFlexGenerator.Process(TangoFlexIRParser.Module);
            return string.Join(Environment.NewLine, TangoFlexGenerator.Output);
        }
    }
}