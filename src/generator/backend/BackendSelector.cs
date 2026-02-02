public static class BackendSelector
{
    public static ICallingConvention Create(RegisterProfile profile)
    {
        switch (profile)
        {
            case RegisterProfile.X86_64_SysV:
                return new X86_64_SysV();

            // case RegisterProfile.X86_32_SysV:
            //     return new X86_32_SysV();

            // case RegisterProfile.X86_32_Cdecl:
            //     return new X86_32_Cdecl();

            // Future ARM support (not implemented):
            case RegisterProfile.ARM64_Linux:
                throw new NotImplementedException("ARM64 backend not implemented.");

            case RegisterProfile.ARM32:
                throw new NotImplementedException("ARM32 backend not implemented.");

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
