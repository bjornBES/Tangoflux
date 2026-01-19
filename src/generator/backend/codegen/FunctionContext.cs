sealed class TempLocation
{
    public RegOperand Reg;
    public int? StackOffset;
}


internal class FunctionContext
{
    public Dictionary<IrLocal, Variable> locals { get; } = new Dictionary<IrLocal, Variable>();
    public Dictionary<IrTemp, Variable> temps { get; } = new Dictionary<IrTemp, Variable>();
    public Dictionary<IrTemp, TempLocation> tempLocations = new Dictionary<IrTemp, TempLocation>();


    public void AddTemp(IrTemp temp, Variable variable)
    {
        temps.Add(temp, variable);
    }
    public void AddLocal(IrLocal local, Variable variable)
    {
        locals.Add(local, variable);
    }

    public bool GetTempVariable(IrTemp temp, out Variable variableInfo)
    {
        if (!temps.TryGetValue(temp, out Variable variable))
        {
            variableInfo = null;
            return false;
        }

        variableInfo = variable;
        return true;
    }
    public bool GetLocalVariable(IrLocal local, out Variable variableInfo)
    {
        if (!locals.TryGetValue(local, out Variable variable))
        {
            variableInfo = null;
            return false;
        }
        variableInfo = variable;
        return true;
    }

}
