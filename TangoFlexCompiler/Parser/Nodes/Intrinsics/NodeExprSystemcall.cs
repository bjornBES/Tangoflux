using TangoFlexCompiler.Parser.Nodes;

namespace TangoFlexCompiler.Parser.Nodes.Intrinsics
{
    public class NodeExprSystemcall : IExpr
    {
        public NodeExpr IntNumber { get; set; }
        public NodeExpr[] Args { get; set; }
    }
}