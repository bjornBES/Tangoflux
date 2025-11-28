
using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(NodeTerm), "NodeTerm")]
// [JsonDerivedType(typeof(NodeExprBinary), "NodeExprBinary")]
// [JsonDerivedType(typeof(NodeExprUnary), "NodeExprUnary")]
// [JsonDerivedType(typeof(NodeExprCall), "NodeExprCall")]
// [JsonDerivedType(typeof(NodeExprAssign), "NodeExprAssign")]
[JsonDerivedType(typeof(NodeExpr), "NodeExpr")]
public interface IExpr
{

}
public class NodeExpr : IExpr
{
    public IExpr expr { get; set; }

    public bool GetTrueType<T>(out T out_stmt) where T : IExpr
    {
        if (expr.GetType() == typeof(T) && expr is T inst)
        {
            out_stmt = inst;
            return true;
        }
        out_stmt = default;
        return false;
    }
}