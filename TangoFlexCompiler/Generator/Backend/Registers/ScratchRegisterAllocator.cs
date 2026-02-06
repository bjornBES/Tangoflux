using System.Collections.Immutable;
using TangoFlexCompiler.Generator.Backend.CallingConventions;
using TangoFlexCompiler.Generator.Backend.CallingConventions.Registers;
using TangoFlexCompiler.Generator.Backend.Operand;

namespace TangoFlexCompiler.Generator.Backend.Registers
{
    public class ScratchRegisterAllocator
    {
        private readonly ICallingConvention convention;
        private readonly RegisterProfile profile;

        // pool of available registers (RegOperand)
        private readonly Stack<PhysicalRegister> freeRegisters = new Stack<PhysicalRegister>();
        private readonly HashSet<RegOperand> inUse = new HashSet<RegOperand>();

        public ScratchRegisterAllocator(ICallingConvention convention, RegisterProfile profile)
        {
            this.convention = convention;
            this.profile = profile;

            // initialize pool
            foreach (PhysicalRegister r in GetScratchRegisters(profile, convention))
                freeRegisters.Push(r);
        }

        private static IEnumerable<PhysicalRegister> GetScratchRegisters(RegisterProfile profile, ICallingConvention convention)
        {
            return convention.ScratchRegisters;
        }

        public RegOperand? Allocate()
        {
            if (freeRegisters.Count == 0)
            {
                Console.WriteLine("No scratch registers available!");
                return null;
            }

            RegOperand r = new RegOperand(freeRegisters.Pop().GetInfo());
            // Console.WriteLine($"Allocating register: {r}");
            inUse.Add(r);
            return r;
        }
        public RegOperand? AllocateTemp()
        {
            if (freeRegisters.Count == 0)
            {
                Console.WriteLine("No scratch registers available!");
                return null;
            }
            // 2 left to avoid exhausting all scratch registers
            if (freeRegisters.Count <= 2)
            {
                // Console.WriteLine("Exceeded max scratch registers!");
                return null;
            }

            RegOperand r = new RegOperand(freeRegisters.Pop().GetInfo());
            // Console.WriteLine($"Allocating temp register: {r}");
            inUse.Add(r);
            return r;
        }

        public void Release(RegOperand r)
        {
            if (!inUse.Remove(r))
                throw new InvalidOperationException($"Register {r} not in use.");
            // Console.WriteLine($"Releasing register: {r}");

            freeRegisters.Push(getMissingRegister(r));
        }

        private PhysicalRegister getMissingRegister(RegOperand operand)
        {
            PhysicalRegister[] registers = GetScratchRegisters(profile, convention).ToArray();
            foreach (PhysicalRegister reg in registers)
            {
                if (reg.Name == operand.Register.Name)
                {
                    return reg;
                }
            }
            return null;
        }

        // peek (non-allocating) just for info/debug
        public IEnumerable<RegOperand> Available()
        {
            List<RegOperand> result = new List<RegOperand>();
            foreach (PhysicalRegister physical in freeRegisters.ToArray())
            {
                result.Add(new RegOperand(physical.GetInfo()));
            }
            return result;
        }
        public IEnumerable<PhysicalRegister> GetUnavailable()
        {
            List<PhysicalRegister> unAvailableRegisters = new List<PhysicalRegister>();
            foreach (RegOperand info in inUse)
            {
                unAvailableRegisters.Add(getMissingRegister(info));
            }
            return unAvailableRegisters;
        }
    }
}