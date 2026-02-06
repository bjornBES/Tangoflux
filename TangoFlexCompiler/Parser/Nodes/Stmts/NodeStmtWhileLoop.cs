using TangoFlexCompiler.Parser.Nodes;

namespace TangoFlexCompiler.Parser.Nodes.Stmts
{
    public class NodeStmtWhileLoop : IStmt
    {
        public NodeExpr Condition { get; set; }
        public NodeStmtScope Scope { get; set; }
    }
}