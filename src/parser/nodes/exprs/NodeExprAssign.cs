
public class NodeExprAssign : IExpr
{
    public string varName { get; set; }
    public NodeExpr value { get; set; }
}