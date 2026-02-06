using TangoFlexCompiler.Parser.Nodes.exprs;

namespace TangoFlexCompiler.Parser.Nodes.Terms
{
    public class NodeTermIntlit : ITerm
    {
        public long Value { get; set; } = 0;
    }
}