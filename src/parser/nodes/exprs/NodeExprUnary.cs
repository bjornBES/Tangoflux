
public class NodeExprUnary : IExpr
{
    public NodeOperator Operator { get; set; }
    public NodeExpr Operand { get; set; }
}
