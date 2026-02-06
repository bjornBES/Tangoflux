
using System.Text.Json.Serialization;
using TangoFlexCompiler.Parser.Nodes;
using TangoFlexCompiler.Parser.Nodes.Terms;

namespace TangoFlexCompiler.Parser.Nodes.exprs
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(NodeTermIntlit), "NodeTermIntlit")]
    // [JsonDerivedType(typeof(NodeTermFloatlit), "NodeTermFloatlit")]
    [JsonDerivedType(typeof(NodeTermStringlit), "NodeTermStringlit")]
    // [JsonDerivedType(typeof(NodeTermBoollit), "NodeTermBoollit")]
    [JsonDerivedType(typeof(NodeTermVar), "NodeTermVar")]
    public abstract class ITerm
    {
        public SourceSpan SourceSpan{ get; set; }
    }

    public class NodeTerm : IExpr
    {
        public ITerm Term { get; set; }

        public bool GetTrueType<T>(out T out_stmt) where T : ITerm
        {
            if (Term.GetType() == typeof(T) && Term is T inst)
            {
                out_stmt = inst;
                return true;
            }
            out_stmt = default;
            return false;
        }
    }
}