using TangoFlexCompiler.Lexer;

namespace TangoFlexCompiler.Parser.Nodes.NodeData
{
    [Serializable]
    public class NodeType
    {
        public NodeType? NestedTypes { get; set; }
        public KeywordVal Type { get; set; }
    }
}