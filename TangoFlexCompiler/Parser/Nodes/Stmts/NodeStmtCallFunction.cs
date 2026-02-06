using TangoFlexCompiler.Parser.Nodes;

namespace TangoFlexCompiler.Parser.Nodes.Stmts
{
    public class NodeStmtCallFunction : IStmt
    {
        public string FunctionName { get; set; }
        public NodeExpr[] FuncArguments { get; set; }
    }
}