sealed class BlockLiveness
{
    public HashSet<IrTemp> Use = new();
    public HashSet<IrTemp> Def = new();
    public HashSet<IrTemp> LiveIn = new();
    public HashSet<IrTemp> LiveOut = new();
}
