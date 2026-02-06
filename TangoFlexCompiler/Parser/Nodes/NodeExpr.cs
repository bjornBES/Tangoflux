
using System.Text.Json.Serialization;
using TangoFlexCompiler;
using TangoFlexCompiler.Parser.Nodes.exprs;
using TangoFlexCompiler.Parser.Nodes.Intrinsics;

namespace TangoFlexCompiler.Parser.Nodes
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(NodeTerm), "NodeTerm")]
    [JsonDerivedType(typeof(NodeExprBinary), "NodeExprBinary")]
    [JsonDerivedType(typeof(NodeExprUnary), "NodeExprUnary")]
    [JsonDerivedType(typeof(NodeExprCall), "NodeExprCall")]
    [JsonDerivedType(typeof(NodeExprAssign), "NodeExprAssign")]
    [JsonDerivedType(typeof(NodeExprArrayAccess), "NodeExprArrayAccess")]
    [JsonDerivedType(typeof(NodeExprFieldAccess), "NodeExprFieldAccess")]
    [JsonDerivedType(typeof(NodeExprAddressOf), "NodeExprAddressOf")]
    [JsonDerivedType(typeof(NodeExprCast), "NodeExprCast")]
    [JsonDerivedType(typeof(NodeExprIntrinsic), "NodeExprIntrinsic")]
    [JsonDerivedType(typeof(NodeExprSystemcall), "NodeExprSystemcall")]
    [JsonDerivedType(typeof(NodeExpr), "NodeExpr")]
    public abstract class IExpr
    {
        public SourceSpan SourceSpan { get; set; }
    }
    public class NodeExpr : IExpr
    {
        public IExpr Expr { get; set; }

        public bool GetTrueType<T>(out T out_stmt) where T : IExpr
        {
            if (Expr.GetType() == typeof(T) && Expr is T inst)
            {
                out_stmt = inst;
                return true;
            }
            out_stmt = default;
            return false;
        }
    }
}