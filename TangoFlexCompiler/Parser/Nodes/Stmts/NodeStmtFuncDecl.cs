using TangoFlexCompiler.Lexer;
using TangoFlexCompiler.Parser.Nodes;
using TangoFlexCompiler.Parser.Nodes.NodeData;

namespace TangoFlexCompiler.Parser.Nodes.Stmts
{
    public class FuncArguments
    {
        public string Name { get; set; }
        public NodeType Type { get; set; }

        public FuncArguments(string name, NodeType type)
        {
            Name = name;
            Type = type;
        }
    }

    public class NodeStmtFuncDecl : IStmt
    {
        public string FuncName { get; set; }
        public NodeVisibility NodeVisibility { get; set; }
        public string CallingConvention { get; set; }
        public NodeType ReturnType { get; set; }
        public List<FuncArguments> Parameters { get; set; } = new List<FuncArguments>();
        public NodeStmtScope Scope { get; set; }
    }
}