using TangoFlexCompiler.Parser.Nodes.exprs;

namespace TangoFlexCompiler.Parser.Nodes.Terms
{
    public class NodeTermVar : ITerm
    {
        public string Name { get; set; }
    }
}