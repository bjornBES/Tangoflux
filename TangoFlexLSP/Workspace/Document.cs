using TangoFlexCompiler.Lexer;
using TangoFlexCompiler.Parser.Nodes;
using TangoFlexCompiler.SymbolParser;

namespace TangoFlexLSP
{
    public sealed class Document
    {
        public string Uri;
        public string Text;
        public Token[] Tokens;
        public NodeProg Ast;
        public SymbolTable Symbols;
    }

}