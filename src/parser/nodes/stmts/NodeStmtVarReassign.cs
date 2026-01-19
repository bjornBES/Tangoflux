
public class NodeStmtVarReassign : IStmt
{
    public string Name { get; set; }
    public NodeOperator Op { get; set; }
    public NodeExpr Expr { get; set; }
}