using TangoFlexCompiler.Lexer;

namespace TangoFlexCompiler.Parser.Nodes.NodeData
{
    [Serializable]
    public class NodeOperator
    {
        public OperatorVal Val { get; set; }
    }
}