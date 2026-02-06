using TangoFlexCompiler.Parser.Nodes;

namespace TangoFlexCompiler.Parser.Nodes.Stmts
{
    public class NodeStmtNamespace : IStmt
    {
        public string Name { get; set; }
        public NodeStmtScope Scope { get; set; }
    }
}