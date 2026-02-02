
using CompilerTangoFlex.lexer;

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

    public object Data { get; set; }
}

public class TangoFlexSymbolParser
{
    public List<Symbol> SymbolTable = new List<Symbol>();

    NodeProg Prog;
    private int nextId = 0;
    private Symbol RootSymbol;

    public TangoFlexSymbolParser(NodeProg prog)
    {
        Prog = prog;
        SymbolTable.Clear();

        // Create a global/root symbol to hold all top-level symbols
        RootSymbol = new Symbol
        {
            Name = "<global>",
            Kind = SymbolKind.Namespace,
            SymbolId = GenerateId()
        };

        ExtractSymbols();
    }

    private SymbolId GenerateId() => new SymbolId(nextId++);


    VisibilityModifiers GetVisibilityModifiers(NodeVisibility nodeVisibility)
    {
        switch (nodeVisibility.visibility)
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
                    Name = varDecl.name,
                    Kind = SymbolKind.Variable,
                    ParentSymbol = parent,
                    Location = varDecl.SourceSpan,
                    VisibilityModifiers = GetVisibilityModifiers(varDecl.nodeVisibility)
                };
            }
            else if (stmt.GetTrueType(out NodeStmtFuncDecl funcDecl))
            {
                sym = new Symbol
                {
                    SymbolId = GenerateId(),
                    Name = funcDecl.funcName,
                    Kind = SymbolKind.Function,
                    ParentSymbol = parent,
                    Location = funcDecl.SourceSpan,
                    VisibilityModifiers = GetVisibilityModifiers(funcDecl.nodeVisibility),
                    Data = funcDecl.parameters
                };
            }
            else if (stmt.GetTrueType(out NodeStmtExternal external))
            {
                if (external.GetTrueType(out ExternalFunc externalFunc))
                {
                    sym = new Symbol
                    {
                        SymbolId = GenerateId(),
                        Name = externalFunc.funcName,
                        Kind = SymbolKind.Function,
                        ParentSymbol = parent,
                        Location = external.SourceSpan,
                        VisibilityModifiers = GetVisibilityModifiers(externalFunc.nodeVisibility),
                        Data = externalFunc.parameters
                    };
                }
            }
            else if (stmt.GetTrueType(out NodeStmtNamespace nsStmt))
            {
                sym = new Symbol
                {
                    SymbolId = GenerateId(),
                    Name = nsStmt.name,
                    Kind = SymbolKind.Namespace,
                    ParentSymbol = parent,
                    Location = nsStmt.SourceSpan,
                    VisibilityModifiers = VisibilityModifiers.Public,
                };

                extractScope(nsStmt.scope.stmts.ToArray(), sym);
            }

            if (sym != null)
            {
                parent.Children.Add(sym);
                SymbolTable.Add(sym);

                // Recurse into children (e.g., class or namespace members)
                if (stmt.stmt is NodeStmtScope scopeStmt)
                {
                    extractScope(scopeStmt.stmts.ToArray(), sym);
                }
            }
        }
    }


    public void ExtractSymbols()
    {
        extractScope(Prog.stmts.ToArray(), RootSymbol);
    }
}