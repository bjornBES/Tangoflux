using TangoFlexCompiler.Parser.Nodes;
using TangoFlexCompiler.Parser.Nodes.NodeData;

namespace TangoFlexCompiler.Parser.Nodes.Intrinsics
{
    public enum CastKind
    {
        Explicit,
        BitCast,
        Implicit
    };


    public class NodeExprCast : IExpr
    {
        public NodeType Type { get; set; }
        public NodeExpr Expr { get; set; }
        public CastKind CastKind { get; set; }
    }
}