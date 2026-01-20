
using CompilerTangoFlex.lexer;

public enum FieldAccessKind
{
    Direct,   // .
    Indirect  // ->
};

public class NodeExprFieldAccess : IExpr
{
    public NodeExpr baseExpr { get; set; }
    public FieldAccessKind fieldAccessKind{ get; set; }
    public NodeExpr fieldExpr { get; set; }
}