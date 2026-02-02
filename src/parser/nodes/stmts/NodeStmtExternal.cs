
using System.Text.Json.Serialization;
using CompilerTangoFlex.lexer;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ExternalFunc), "ExternalFunc")]
public interface IExternal
{
}

public class NodeStmtExternal : IStmt
{
    public string from { get; set; }
    public IExternal external{ get; set; }
    public bool GetTrueType<T>(out T out_stmt) where T : IExternal
    {
        if (external.GetType() == typeof(T) && external is T inst)
        {
            out_stmt = inst;
            return true;
        }
        out_stmt = default;
        return false;
    }
}