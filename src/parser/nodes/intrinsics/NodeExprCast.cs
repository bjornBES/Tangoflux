
public enum CastKind {
    Explicit,
    BitCast,
    Implicit
};


public class NodeExprCast : IExpr
{
    public NodeType type { get; set; }
    public NodeExpr expr{ get; set; }
    public CastKind castKind{ get; set; }
}