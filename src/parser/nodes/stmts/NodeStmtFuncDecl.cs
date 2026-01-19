
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
    public NodeType returnType {get; set;}
    public List<FuncArguments> parameters {get; set;} = new List<FuncArguments>();
    public NodeStmtScope scope {get; set;}
}
