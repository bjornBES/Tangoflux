using TangoFlexCompiler.Lexer;
using TangoFlexCompiler.Parser.Nodes;

namespace TangoFlexCompiler.Parser.Nodes.exprs
{
    public enum FieldAccessKind
    {
        Direct,   // .
        Indirect  // ->
    };

    public class NodeExprFieldAccess : IExpr
    {
        public string StructName { get; set; }
        public NodeExpr BaseExpr { get; set; }
        public FieldAccessKind FieldAccessKind { get; set; }
        public NodeExpr FieldExpr { get; set; }
    }
}