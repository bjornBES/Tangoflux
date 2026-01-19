public class ScratchRegisterAllocator
{
    private readonly ICallingConvention conv;
    private readonly RegisterProfile profile;

    // pool of available registers (RegOperand)
    private readonly Stack<RegOperand> freeRegs = new Stack<RegOperand>();
    private readonly HashSet<RegisterInfo> inUse = new HashSet<RegisterInfo>();

    public ScratchRegisterAllocator(ICallingConvention conv, RegisterProfile profile)
    {
        this.conv = conv;
        this.profile = profile;

        // initialize pool
        foreach (var r in GetScratchRegisters(profile, conv))
            freeRegs.Push(new RegOperand(r));
    }

    private static IEnumerable<RegisterInfo> GetScratchRegisters(RegisterProfile profile, ICallingConvention conv)
    {
        switch (profile)
        {
            case RegisterProfile.X86_64_SysV:
                return new[]
                {
                    conv.GetRegister(RegisterFunction.Scratch) ?? new RegisterInfo("rax",3),
                    conv.GetRegister(RegisterFunction.Arg0) ?? new RegisterInfo("rdi",4),
                    // conv.GetRegister(RegisterFunction.Arg1) ?? new RegisterInfo("rsi",5),
                    conv.GetRegister(RegisterFunction.Arg2) ?? new RegisterInfo("rdx",6),
                    conv.GetRegister(RegisterFunction.Arg3) ?? new RegisterInfo("r10",7),
                    conv.GetRegister(RegisterFunction.Arg4) ?? new RegisterInfo("r8",8),
                    conv.GetRegister(RegisterFunction.Arg5) ?? new RegisterInfo("r9",9),
                    new RegisterInfo("rcx",10),
                    new RegisterInfo("r11",11),
                };
            default:
                throw new NotImplementedException();
        }
    }

    public RegOperand? Allocate()
    {
        if (freeRegs.Count == 0)
        {
            Console.WriteLine("No scratch registers available!");
            return null;
        }

        var r = freeRegs.Pop();
        Console.WriteLine($"Allocating register: {r}");
        inUse.Add(r.Register);
        return r;
    }
    public RegOperand? AllocateTemp()
    {
        if (freeRegs.Count == 0)
        {
            Console.WriteLine("No scratch registers available!");
            return null;
        }
        // 2 left to avoid exhausting all scratch regs
        if (freeRegs.Count <= 2)
        {
            Console.WriteLine("Exceeded max scratch registers!");
            return null;
        }

        RegOperand r = freeRegs.Pop();
        Console.WriteLine($"Allocating temp register: {r}");
        inUse.Add(r.Register);
        return r;
    }

    public void Release(RegOperand r)
    {
        if (!inUse.Remove(r.Register))
            throw new InvalidOperationException($"Register {r} not in use.");
        Console.WriteLine($"Releasing register: {r}");
        freeRegs.Push(r);
    }

    // peek (non-allocating) just for info/debug
    public IEnumerable<RegOperand> Available() => freeRegs.ToArray();
}
