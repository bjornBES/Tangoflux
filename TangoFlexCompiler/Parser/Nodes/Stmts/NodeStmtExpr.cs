using TangoFlexCompiler.Parser.Nodes;

namespace TangoFlexCompiler.Parser.Nodes.Stmts
{
    public class NodeStmtExpr : IStmt
    {
        public NodeExpr Expr { get; set; }
    }
}