
public class ExternalFunc : IExternal
{
    public string funcName {get; set;}
    public NodeVisibility nodeVisibility{get; set;}
    public string callingConvention {get; set;}
    public NodeType returnType {get; set;}
    public List<FuncArguments> parameters {get; set;} = new List<FuncArguments>();
}