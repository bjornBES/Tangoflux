
using System.Text.Json.Serialization;
using TangoFlexCompiler.Parser.Nodes;
using TangoFlexCompiler.Parser.Nodes.Intrinsics;

namespace TangoFlexCompiler.Parser.Nodes.exprs
{
    public class NodeExprIntrinsic : IExpr
    {
        public string Instruction { get; set; }
        public NodeExpr Intrinsic { get; set; }
    }
}