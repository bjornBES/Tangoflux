
using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(NodeStmtReturn), "NodeStmtReturn")]
[JsonDerivedType(typeof(NodeStmtVarDecl), "NodeStmtVarDecl")]
[JsonDerivedType(typeof(NodeStmtFuncDecl), "NodeFuncDecl")]
[JsonDerivedType(typeof(NodeStmtScope), "NodeScope")]
public interface IStmt
{
}

public class NodeStmt
{
    public IStmt stmt { get; set; }
    public bool GetTrueType<T>(out T out_stmt) where T : IStmt
    {
        if (stmt.GetType() == typeof(T) && stmt is T inst)
        {
            out_stmt = inst;
            return true;
        }
        out_stmt = default;
        return false;
    }
}
