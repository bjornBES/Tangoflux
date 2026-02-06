using TangoFlexCompiler.Parser.Nodes;
using TangoFlexCompiler.Parser.Nodes.NodeData;

namespace TangoFlexCompiler.Parser.Nodes.exprs
{
    public class NodeExprBinary : IExpr
    {
        public NodeExpr Lhs { get; set; }
        public NodeOperator Operator { get; set; }
        public NodeExpr Rhs { get; set; }
    }
}