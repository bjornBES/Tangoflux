using OmniSharp.Extensions.JsonRpc;
using TangoFlexCompiler;
using TangoFlexCompiler.Lexer;
using TangoFlexCompiler.Parser.Nodes;
using TangoFlexCompiler.SymbolParser;

namespace TangoFlexLSP
{
    public sealed class WorkspaceState
    {
        public CompilerState Compiler { get; set; }
        private readonly Dictionary<string, Document> documents =
            new Dictionary<string, Document>();

        public WorkspaceState(CompilerState compiler)
        {
            Compiler = compiler;
        }

        public void Open(string uri, string text)
        {
            Document doc = new Document();
            doc.Uri = uri;
            doc.Text = text;

            Reparse(doc);
            documents[uri] = doc;
        }

        public void Change(string uri, string newText)
        {
            Document doc = documents[uri];
            doc.Text = newText;
            Reparse(doc);
        }

        private void Reparse(Document doc)
        {
            // THIS IS WHERE TANGOFLEX COMPILER LIVES
            Compiler.BuildFile(doc.Text, doc.Uri, out Token[] tokens, out NodeProg nodeProg, out Symbol[] symbols);
            SymbolTable symbolTable= new SymbolTable()
            {
                Symbols = symbols
            };
            doc.Tokens = tokens;
            doc.Ast = nodeProg;
            doc.Symbols = symbolTable;
        }

        public Document Get(string uri)
        {
            return documents[uri];
        }
    }

}