
using System.Text.Json;
using System.Text.Json.Serialization;
using Common;
using TangoFlexCompiler;
using TangoFlexCompiler.Lexer;
using TangoFlexCompiler.Parser.Nodes;
using TangoFlexCompiler.Parser.Nodes.Externals;
using TangoFlexCompiler.Parser.Nodes.NodeData;
using TangoFlexCompiler.Parser.Nodes.Stmts;

namespace TangoFlexCompiler.SymbolParser
{
    public enum SymbolKind
    {
        Unknown = 0,
        Class = 1,
        Interface = 2,
        Function = 3,
        Variable = 4,
        Namespace = 5,
    }

    [Flags]
    public enum VisibilityModifiers
    {
        Public = 1,
        Protected = 2,
        Private = 4,
        Static = 8,
        Const = 16,
        Internal = 32,
    }

    [Flags]
    public enum SymbolFlags
    {
        Deprecated = 1,
        Inline = 2,
        External = 4,
    }

    public record SymbolId(int id);

    public class Symbol
    {
        public string Name { get; set; }
        public SymbolKind Kind { get; set; }
        public VisibilityModifiers VisibilityModifiers { get; set; }
        public Symbol ParentSymbol { get; set; }
        public SourceSpan Location { get; set; }
        public SymbolId SymbolId { get; set; }
        public List<Symbol> Children { get; set; } = new List<Symbol>();
        public SymbolFlags Flags { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Data { get; set; }
    }

    public class TangoFlexSymbolParser
    {
        bool debug = true;

        public List<Symbol> SymbolTable = new List<Symbol>();

        private int nextId = 0;
        private Symbol RootSymbol;
        private Arguments arguments;

        public TangoFlexSymbolParser(Arguments arguments)
        {
            this.arguments = arguments;
            SymbolTable.Clear();

            // Create a global/root symbol to hold all top-level symbols
            RootSymbol = new Symbol
            {
                Name = "<global>",
                Kind = SymbolKind.Namespace,
                SymbolId = GenerateId()
            };
        }

        public Symbol[] GetTable(NodeProg prog)
        {
            SymbolTable.Clear();
            nextId = 0;
            RootSymbol = new Symbol
            {
                Name = "<global>",
                Kind = SymbolKind.Namespace,
                SymbolId = GenerateId()
            };
            ExtractSymbols(prog);
            return SymbolTable.ToArray();
        }

        private SymbolId GenerateId() => new SymbolId(nextId++);


        VisibilityModifiers GetVisibilityModifiers(NodeVisibility nodeVisibility)
        {
            switch (nodeVisibility.Visibility)
            {
                case KeywordVal.PUBLIC:
                    return VisibilityModifiers.Public;
                case KeywordVal.PRIVATE:
                    return VisibilityModifiers.Private;
                case KeywordVal.INTERNAL:
                    return VisibilityModifiers.Internal;
            }
            return 0;
        }

        void extractScope(NodeStmt[] stmts, Symbol parent)
        {
            foreach (var stmt in stmts)
            {
                Symbol sym = null;

                if (stmt.GetTrueType(out NodeStmtVarDecl varDecl))
                {
                    sym = new Symbol
                    {
                        SymbolId = GenerateId(),
                        Name = varDecl.Name,
                        Kind = SymbolKind.Variable,
                        ParentSymbol = parent,
                        Location = varDecl.SourceSpan,
                        VisibilityModifiers = GetVisibilityModifiers(varDecl.NodeVisibility)
                    };
                }
                else if (stmt.GetTrueType(out NodeStmtFuncDecl funcDecl))
                {
                    sym = new Symbol
                    {
                        SymbolId = GenerateId(),
                        Name = funcDecl.FuncName,
                        Kind = SymbolKind.Function,
                        ParentSymbol = parent,
                        Location = funcDecl.SourceSpan,
                        VisibilityModifiers = GetVisibilityModifiers(funcDecl.NodeVisibility),
                        Data = funcDecl.Parameters
                    };
                }
                else if (stmt.GetTrueType(out NodeStmtExternal external))
                {
                    if (external.GetTrueType(out ExternalFunc externalFunc))
                    {
                        sym = new Symbol
                        {
                            SymbolId = GenerateId(),
                            Name = externalFunc.FuncName,
                            Kind = SymbolKind.Function,
                            ParentSymbol = parent,
                            Location = external.SourceSpan,
                            VisibilityModifiers = GetVisibilityModifiers(externalFunc.NodeVisibility),
                            Data = externalFunc.Parameters
                        };
                    }
                }
                else if (stmt.GetTrueType(out NodeStmtNamespace nsStmt))
                {
                    sym = new Symbol
                    {
                        SymbolId = GenerateId(),
                        Name = nsStmt.Name,
                        Kind = SymbolKind.Namespace,
                        ParentSymbol = parent,
                        Location = nsStmt.SourceSpan,
                        VisibilityModifiers = VisibilityModifiers.Public,
                    };

                    extractScope(nsStmt.Scope.Stmts.ToArray(), sym);
                }

                if (sym != null)
                {
                    parent.Children.Add(sym);
                    SymbolTable.Add(sym);

                    // Recurse into children (e.g., class or namespace members)
                    if (stmt.Stmt is NodeStmtScope scopeStmt)
                    {
                        extractScope(scopeStmt.Stmts.ToArray(), sym);
                    }
                }
            }
        }


        public void ExtractSymbols(NodeProg prog)
        {
            extractScope(prog.Stmts.ToArray(), RootSymbol);

            if (debug && arguments.debug && arguments.WriteDebugFiles)
            {
                JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    MaxDepth = 64,
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };
                serializerOptions.Converters.Add(new JsonStringEnumConverter());

                string json = JsonSerializer.Serialize(RootSymbol, serializerOptions);
                File.WriteAllText("./debug/Symbols.json", json);
            }
        }
    }
}