
/// <summary>
/// Describes the function frame requirements the generator needs.
/// </summary>
public class FunctionFrame
{
    public string FunctionName { get; set; } = "func";
    /// <summary>Bytes required for locals (stack slots). The generator will add alignment padding.</summary>
    public int LocalSize { get; set; } = 0;
    /// <summary>Whether code wants/needs a frame pointer (rbp/x29). If false, frame pointer omitted when possible.</summary>
    public bool UseFramePointer { get; set; } = true;
    /// <summary>Registers (RegisterInfo) that the function actually uses which must be preserved (callee-saved).</summary>
    public List<RegisterInfo> CalleeSavedUsed { get; } = new List<RegisterInfo>();
    /// <summary>Stack alignment in bytes required by the ABI (typically 16 for x86-64 SysV, 4/16 for others).</summary>
    public int StackAlignment { get; set; } = 16;
    /// <summary>If true, function is leaf (no calls) so caller-saved could sometimes be reused; generator doesn't currently optimize for leaf-ness except frame-pointer omission.</summary>
    public bool IsLeaf { get; set; } = false;

    // Registers touched by this function (any usage)
    public HashSet<RegisterInfo> RegistersUsed = new HashSet<RegisterInfo>();
}
