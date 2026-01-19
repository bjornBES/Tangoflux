
public class NodeExprCall : IExpr
{
    public string FuncName { get; set; }
    public NodeExpr Callee { get; set; }
    public List<NodeExpr> Args { get; set; } = new List<NodeExpr>();
}
