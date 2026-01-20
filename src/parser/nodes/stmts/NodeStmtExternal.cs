
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
}