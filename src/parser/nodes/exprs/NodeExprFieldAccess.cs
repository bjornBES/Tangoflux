
using CompilerTangoFlex.lexer;

public enum FieldAccessKind
{
    Direct,   // .
    Indirect  // ->
};

public class NodeExprFieldAccess : IExpr
{
    public string structName { get; set; }
    public NodeExpr baseExpr { get; set; }
    public FieldAccessKind fieldAccessKind{ get; set; }
    public NodeExpr fieldExpr { get; set; }
}