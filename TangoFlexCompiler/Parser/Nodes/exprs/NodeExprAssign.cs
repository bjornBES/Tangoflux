using TangoFlexCompiler.Parser.Nodes;

namespace TangoFlexCompiler.Parser.Nodes.exprs
{
    public class NodeExprAssign : IExpr
    {
        public string VarName { get; set; }
        public NodeExpr Value { get; set; }
    }
}