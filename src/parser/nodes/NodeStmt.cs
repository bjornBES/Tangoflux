
using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(NodeStmtReturn), "NodeStmtReturn")]
[JsonDerivedType(typeof(NodeStmtVarDecl), "NodeStmtVarDecl")]
[JsonDerivedType(typeof(NodeStmtFuncDecl), "NodeFuncDecl")]
[JsonDerivedType(typeof(NodeStmtIf), "NodeStmtIf")]
[JsonDerivedType(typeof(NodeStmtScope), "NodeScope")]
[JsonDerivedType(typeof(NodeStmtForLoop), "NodeStmtForLoop")]
[JsonDerivedType(typeof(NodeStmtVarReassign), "NodeStmtVarReassign")]
[JsonDerivedType(typeof(NodeStmtWhileLoop), "NodeStmtWhileLoop")]
[JsonDerivedType(typeof(NodeStmtCallFunction), "NodeStmtCallFunction")]
[JsonDerivedType(typeof(NodeStmtExternal), "NodeStmtExternal")]
[JsonDerivedType(typeof(NodeStmtExpr), "NodeStmtExpr")]
[JsonDerivedType(typeof(NodeStmtNamespace), "NodeStmtNamespace")]
public abstract class IStmt
{
    public SourceSpan SourceSpan{ get; set; }
}

public class NodeStmt
{
    public IStmt stmt { get; set; }
    public bool GetTrueType<T>(out T out_stmt) where T : IStmt
    {
        if (stmt is T inst)
        {
            out_stmt = inst;
            return true;
        }
        out_stmt = default;
        return false;
    }
}
