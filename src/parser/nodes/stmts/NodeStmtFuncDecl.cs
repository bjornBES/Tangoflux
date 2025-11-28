
public class FuncArguments
{
    public string name {get; set;}
    public NodeType type {get; set;}
}

public class NodeStmtFuncDecl : IStmt
{
    public string funcName {get; set;}
    public NodeType returnType {get; set;}
    public List<FuncArguments> arguments {get; set;} = new List<FuncArguments>();
    public NodeStmtScope scope {get; set;}
}
