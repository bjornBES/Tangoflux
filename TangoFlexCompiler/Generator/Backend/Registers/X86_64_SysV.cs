using System.Collections.Generic;
using TangoFlexCompiler.Generator.Backend.CallingConventions;
using TangoFlexCompiler.Generator.Backend.CallingConventions.Registers;
using TangoFlexCompiler.Generator.Backend.CallingConventions.Stack;
using TangoFlexCompiler.Generator.Backend.Codegen;
using TangoFlexCompiler.Generator.Backend.Operand;

namespace TangoFlexCompiler.Generator.Backend.Registers
{
    public class X86_64_SysV : ICallingConvention
    {
        public string Name => "sysv";

        // Registers
        public static readonly PhysicalRegister RAX = new("rax", RegisterClass.Gp, true, false);
        public static readonly PhysicalRegister RBX = new("rbx", RegisterClass.Gp, true, false);
        public static readonly PhysicalRegister RDI = new("rdi", RegisterClass.Gp, true, false);
        public static readonly PhysicalRegister RSI = new("rsi", RegisterClass.Gp, true, false);
        public static readonly PhysicalRegister RDX = new("rdx", RegisterClass.Gp, true, false);
        public static readonly PhysicalRegister RCX = new("rcx", RegisterClass.Gp, true, false);
        public static readonly PhysicalRegister R8 = new("r8", RegisterClass.Gp, true, false);
        public static readonly PhysicalRegister R9 = new("r9", RegisterClass.Gp, true, false);
        public static readonly PhysicalRegister R10 = new("r10", RegisterClass.Gp, true, false);
        public static readonly PhysicalRegister R11 = new("r11", RegisterClass.Gp, true, false);
        public static readonly PhysicalRegister R12 = new("r12", RegisterClass.Gp, true, false);
        public static readonly PhysicalRegister R13 = new("r13", RegisterClass.Gp, true, false);
        public static readonly PhysicalRegister R14 = new("r14", RegisterClass.Gp, true, false);
        public static readonly PhysicalRegister R15 = new("r15", RegisterClass.Gp, true, false);

        public static readonly PhysicalRegister RSP = new("rsp", RegisterClass.Special, false, false);
        public static readonly PhysicalRegister RBP = new("rbp", RegisterClass.Special, false, true);
        public static readonly PhysicalRegister RIP = new("rip", RegisterClass.Special, false, false);

        public static readonly PhysicalRegister XMM0 = new("xmm0", RegisterClass.Float, true, false);
        public static readonly PhysicalRegister XMM1 = new("xmm1", RegisterClass.Float, true, false);


        public StackPolicy Stack => new()
        {
            Alignment = 16,
            Cleanup = StackCleanup.Caller,
            RedZone = true
        };

        public bool SupportsRipRelative => true;
        public IReadOnlyList<ArgumentRule> ArgumentRegisters { get; } =
            new List<ArgumentRule>
            {
            new() { AcceptedClass = RegisterClass.Gp, Register = RDI },
            new() { AcceptedClass = RegisterClass.Gp, Register = RSI },
            new() { AcceptedClass = RegisterClass.Gp, Register = RDX },
            new() { AcceptedClass = RegisterClass.Gp, Register = RCX },
            new() { AcceptedClass = RegisterClass.Gp, Register = R8 },
            new() { AcceptedClass = RegisterClass.Gp, Register = R9 },
            };

        public IReadOnlyList<PhysicalRegister> ScratchRegisters =>
            new[] { RAX, RDI, RSI, RDX, RCX, R8, R9 };

        public IReadOnlyList<PhysicalRegister> CalleeSavedRegisters =>
            new[] { RBP /* + rbx r12-r15 */ };


        public IReadOnlyList<PhysicalRegister> AddressScratchRegisters =>
            new[] { R10, R11 };

        public IReadOnlyList<PhysicalRegister> AddressCalleeSavedRegisters =>
            new[] { RBX, R12, R13, R14, R15 };


        private static readonly Dictionary<RegisterRole, PhysicalRegister> Roles = new()
    {
        // Special
        { RegisterRole.StackPointer, RSP },
        { RegisterRole.BasePointer,  RBP },
        { RegisterRole.ProgramCounter, RIP },

        // ABI
        { RegisterRole.SyscallNumber, RAX },
        { RegisterRole.ReturnLow, RAX },
        { RegisterRole.ReturnHigh, RDX },
        { RegisterRole.ReturnFloat, XMM0 },
        { RegisterRole.ReturnDouble, XMM0 },


        // Addressing
        { RegisterRole.AddressBase, R10 },
        { RegisterRole.AddressIndex, R11 },

        // Multiply (implicit)
        { RegisterRole.MultiplySource1, RAX },
        { RegisterRole.MultiplyResultLow, RAX },
        { RegisterRole.MultiplyResultHigh, RDX },

        // Divide (implicit)
        { RegisterRole.DivideDividendLow, RAX },
        { RegisterRole.DivideDividendHigh, RDX },
        { RegisterRole.DivideQuotient, RAX },
        { RegisterRole.DivideRemainder, RDX },
    };

        public bool TryGetRole(RegisterRole role, out PhysicalRegister register)
            => Roles.TryGetValue(role, out register);
        public bool TryGetRole(RegisterRole role, out RegisterInfo register)
        {
            if (Roles.TryGetValue(role, out PhysicalRegister physical))
            {
                register = physical.GetInfo();
                return true;
            }
            register = null;
            return false;
        }

        public RegisterInfo GetRegister(RegisterRole role, string msg)
        {
            if (!TryGetRole(role, out RegisterInfo src))
                throw new InvalidOperationException("Multiply not supported on this target");
            return src;
        }

        public string EmitCall(TFAsmGen emitter, AsmOperand symbol, IReadOnlyList<AsmOperand> args)
        {
            List<string> lines = new List<string>();
            int regIndex = 0;

            // push stack args right-to-left
            for (int i = args.Count - 1; i >= 0; i--)
            {
                if (regIndex < ArgumentRegisters.Count)
                {
                    var rule = ArgumentRegisters[regIndex++];
                    lines.Add(emitter.Move(new RegOperand(rule.Register.GetInfo()), args[i]));
                }
                else
                {
                    lines.Add(emitter.Push(args[i]));
                }
            }

            lines.Add(emitter.Call(symbol));

            if (Stack.Cleanup == StackCleanup.Caller)
            {
                int stackArgs = Math.Max(0, args.Count - ArgumentRegisters.Count);
                if (stackArgs > 0)
                    lines.Add(emitter.Add(new RegOperand(RSP.GetInfo()), new ImmOperand(stackArgs * 8)));
            }
            return string.Join(Environment.NewLine, lines);
        }
        public string EmitSyscall(TFAsmGen emitter, AsmOperand symbol, IReadOnlyList<AsmOperand> args)
        {
            List<string> lines = new List<string>();
            int regIndex = 0;

            if (args.Count > ArgumentRegisters.Count)
            {
                Console.WriteLine("Invalid system call for this target");
                return emitter.Comment("Invalid system call for this target");
            }

            PhysicalRegister[] registers = emitter.RegisterAllocator.GetUnavailable().ToArray();

            // push stack args right-to-left
            for (int i = args.Count - 1; i >= 0; i--)
            {
                if (regIndex < ArgumentRegisters.Count)
                {
                    ArgumentRule rule = ArgumentRegisters[regIndex++];
                    if (registers.Contains(rule.Register))
                    {
                        lines.Add(emitter.Push(new RegOperand(rule.Register.GetInfo())));
                    }
                    lines.Add(emitter.Move(new RegOperand(rule.Register.GetInfo()), args[i]));
                }
                else
                {
                    throw new ArgumentException("Something when wrong");
                }
            }

            lines.Add(emitter.SystemCall(symbol));

            if (Stack.Cleanup == StackCleanup.Caller)
            {
                int stackArgs = Math.Max(0, args.Count - ArgumentRegisters.Count);
                if (stackArgs > 0)
                    lines.Add(emitter.Add(new RegOperand(RSP.GetInfo()), new ImmOperand(stackArgs * 8)));
            }
            return string.Join(Environment.NewLine, lines);
        }
    }
}