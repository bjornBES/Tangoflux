using TangoFlexCompiler.Parser.Nodes.NodeData;
using TangoFlexCompiler.Parser.Nodes.Stmts;

namespace TangoFlexCompiler.Parser.Nodes.Externals
{
    public class ExternalFunc : IExternal
    {
        public string FuncName { get; set; }
        public NodeVisibility NodeVisibility { get; set; }
        public string CallingConvention { get; set; }
        public NodeType ReturnType { get; set; }
        public List<FuncArguments> Parameters { get; set; } = new List<FuncArguments>();
    }
}