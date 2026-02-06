using TangoFlexCompiler.Parser.Nodes;

namespace TangoFlexCompiler.Parser.Nodes.exprs
{
    public class NodeExprCall : IExpr
    {
        public string FuncName { get; set; }
        public NodeExpr Callee { get; set; }
        public NodeExpr[] Args { get; set; }
    }
}