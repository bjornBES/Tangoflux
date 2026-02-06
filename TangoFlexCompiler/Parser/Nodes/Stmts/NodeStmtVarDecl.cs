
using System.Text.Json.Serialization;
using TangoFlexCompiler.Parser.Nodes;
using TangoFlexCompiler.Parser.Nodes.NodeData;

namespace TangoFlexCompiler.Parser.Nodes.Stmts
{
    public class NodeStmtVarDecl : IStmt
    {
        public string Name { get; set; }
        public NodeType Type { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public NodeExpr Expr { get; set; }
        public NodeVisibility NodeVisibility { get; set; }
    }
}