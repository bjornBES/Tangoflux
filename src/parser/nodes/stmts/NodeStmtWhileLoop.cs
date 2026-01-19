
public class NodeStmtWhileLoop : IStmt
{
    public NodeExpr Condition { get; set; }
    public NodeStmtScope Scope { get; set; }
}