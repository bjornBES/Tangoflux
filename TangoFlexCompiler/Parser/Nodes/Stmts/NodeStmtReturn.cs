using TangoFlexCompiler.Parser.Nodes;

namespace TangoFlexCompiler.Parser.Nodes.Stmts
{
    public class NodeStmtReturn : IStmt
    {
        public NodeExpr Expr { get; set; }
    }
}