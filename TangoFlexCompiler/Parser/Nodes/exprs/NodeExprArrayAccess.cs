using TangoFlexCompiler.Parser.Nodes;

namespace TangoFlexCompiler.Parser.Nodes.exprs
{
    public class NodeExprArrayAccess : IExpr
    {
        public NodeExpr Array { get; set; }
        public NodeExpr Index { get; set; }
    }
}