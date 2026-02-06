using TangoFlexCompiler.Parser.Nodes.NodeData;
using TangoFlexCompiler.Parser.Nodes.Stmts;

namespace TangoFlexCompiler.Parser.Nodes.Externals
{
    public class ExternalVar : IExternal
    {
        public string VarName { get; set; }
        public NodeVisibility NodeVisibility { get; set; }
        public NodeType Type { get; set; }
    }
}