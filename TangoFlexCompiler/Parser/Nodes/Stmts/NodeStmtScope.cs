using TangoFlexCompiler.Parser.Nodes;

namespace TangoFlexCompiler.Parser.Nodes.Stmts
{
    public class NodeStmtScope : IStmt
    {
        public List<NodeStmt> Stmts { get; set; }
    }
}