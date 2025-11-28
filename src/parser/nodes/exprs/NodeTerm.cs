
using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(NodeTermIntlit), "NodeTermIntlit")]
// [JsonDerivedType(typeof(NodeTermFloatlit), "NodeTermFloatlit")]
// [JsonDerivedType(typeof(NodeTermStringlit), "NodeTermStringlit")]
// [JsonDerivedType(typeof(NodeTermBoollit), "NodeTermBoollit")]
[JsonDerivedType(typeof(NodeTermVar), "NodeTermVar")]
public interface ITerm
{

}

public class NodeTerm : IExpr
{
    public ITerm term { get; set; }

    public bool GetTrueType<T>(out T out_stmt) where T : ITerm
    {
        if (term.GetType() == typeof(T) && term is T inst)
        {
            out_stmt = inst;
            return true;
        }
        out_stmt = default;
        return false;
    }
}
