
public class NodeExprArrayAccess : IExpr
{
    public NodeExpr array { get; set; }
    public NodeExpr index { get; set; }
}