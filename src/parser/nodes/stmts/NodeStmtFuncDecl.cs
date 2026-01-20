
using CompilerTangoFlex.lexer;

public class NodeVisibility
{
    public KeywordVal visibility { get; set; }
}

public class NodeCallingConventions
{
    public TokenIdentifier cc { get; set; }
}

public class FuncArguments
{
    public string Name {get; set;}
    public NodeType Type {get; set;}

    public FuncArguments(string name, NodeType type)
    {
        Name = name;
        Type = type;
    }
}

public class NodeStmtFuncDecl : IStmt
{
    public string funcName {get; set;}
    public NodeVisibility nodeVisibility{get; set;}
    public NodeCallingConventions nodeCallingConventions{get; set;}
    public string callingConvention {get; set;}
    public NodeType returnType {get; set;}
    public List<FuncArguments> parameters {get; set;} = new List<FuncArguments>();
    public NodeStmtScope scope {get; set;}
}
