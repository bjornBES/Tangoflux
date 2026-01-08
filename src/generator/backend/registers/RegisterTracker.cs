public sealed class RegisterTracker
{
    private readonly FunctionFrame frame;
    private readonly ICallingConvention conv;
    private readonly RegisterProfile profile;

    private readonly HashSet<string> calleeSavedNames;

    public RegisterTracker(FunctionFrame frame, ICallingConvention conv, RegisterProfile profile)
    {
        this.frame = frame;
        this.conv = conv;
        this.profile = profile;

        calleeSavedNames = PrologueEpilogueCalleeSavedNames(profile);
    }

    public void Use(RegisterInfo reg)
    {
        if (frame.RegistersUsed.Add(reg))
        {
            if (calleeSavedNames.Contains(reg.Name))
                frame.CalleeSavedUsed.Add(reg);
        }
    }

    private static HashSet<string> PrologueEpilogueCalleeSavedNames(RegisterProfile profile)
    {
        switch (profile)
        {
            case RegisterProfile.X86_64_SysV:
                return new HashSet<string> { "rbp", "rbx", "r12", "r13", "r14", "r15" };

            case RegisterProfile.X86_32_Cdecl:
                return new HashSet<string> { "ebp", "ebx", "esi", "edi" };

            case RegisterProfile.ARM64_Linux:
                return new HashSet<string>
                {
                    "x19","x20","x21","x22","x23","x24","x25","x26","x27","x28","x29"
                };

            case RegisterProfile.ARM32:
                return new HashSet<string>
                {
                    "r4","r5","r6","r7","r8","r9","r10","r11"
                };

            default:
                return new HashSet<string>();
        }
    }
}
