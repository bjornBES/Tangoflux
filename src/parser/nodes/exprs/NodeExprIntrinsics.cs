
using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(NodeExprCast), "NodeExprCast")]
public abstract class IExprIntrinsic : IExpr
{
    
}

public class NodeExprIntrinsic : IExpr
{
    public string instruction {get; set;}
    public NodeExpr intrinsic {get; set;}
}