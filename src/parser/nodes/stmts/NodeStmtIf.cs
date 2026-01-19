
using System.Text.Json.Serialization;

public class NodeStmtIf : IStmt
{
    public NodeExpr Condition { get; set; }
    public NodeStmtScope Scope { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NodeIfPred? Pred { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(NodeIfPredElse), "NodeIfPredElse")]
[JsonDerivedType(typeof(NodeIfPredElseIf), "NodeIfPredElseIf")]
public class NodeIfPred
{
    public bool GetTrueType<T>(out T out_pred) where T : NodeIfPred
    {
        if (GetType() == typeof(T) && this is T inst)
        {
            out_pred = inst;
            return true;
        }
        out_pred = default;
        return false;
    }
}

public class NodeIfPredElse : NodeIfPred
{
    public NodeStmtScope Scope { get; set; }
}

public class NodeIfPredElseIf : NodeIfPred
{
    public NodeExpr Condition { get; set; }
    public NodeStmtScope Scope { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NodeIfPred? Pred { get; set; }
}