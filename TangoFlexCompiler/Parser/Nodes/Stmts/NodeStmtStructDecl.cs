using TangoFlexCompiler.Parser.Nodes;
using TangoFlexCompiler.Parser.Nodes.NodeData;

namespace TangoFlexCompiler.Parser.Nodes.Stmts
{
    public class NodeStructField
    {
        public string FieldName { get; set; }
        public NodeVisibility NodeVisibility { get; set; }
        public NodeType FieldType { get; set; }
        // syntax var <visibility> <Name> : <Type>

    }

    public class NodeStmtStructDecl : IStmt
    {
        public string StructName { get; set; }
        public NodeVisibility NodeVisibility { get; set; }
        public List<NodeStructField> Fields { get; set; } = new List<NodeStructField>();
    }
}