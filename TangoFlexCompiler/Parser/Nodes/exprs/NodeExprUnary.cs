using TangoFlexCompiler.Parser.Nodes;
using TangoFlexCompiler.Parser.Nodes.NodeData;

namespace TangoFlexCompiler.Parser.Nodes.exprs
{
    public class NodeExprUnary : IExpr
    {
        public NodeOperator Operator { get; set; }
        public NodeExpr Operand { get; set; }
    }
}