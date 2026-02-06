using TangoFlexCompiler.Parser.Nodes.exprs;

namespace TangoFlexCompiler.Parser.Nodes.Terms
{
    public class NodeTermStringlit : ITerm
    {
        public string Value { get; set; }
    }
}