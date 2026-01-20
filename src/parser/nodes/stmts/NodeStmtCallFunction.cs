
public class NodeStmtCallFunction : IStmt
{
    public string functionName { get; set; }
    public NodeExpr[] funcArguments{ get; set; }
}