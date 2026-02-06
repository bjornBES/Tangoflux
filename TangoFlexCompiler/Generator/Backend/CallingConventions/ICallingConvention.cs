using TangoFlexCompiler.Generator.Backend.CallingConventions.Registers;
using TangoFlexCompiler.Generator.Backend.CallingConventions.Stack;
using TangoFlexCompiler.Generator.Backend.Codegen;
using TangoFlexCompiler.Generator.Backend.Operand;
using TangoFlexCompiler.Generator.Backend.Registers;

namespace TangoFlexCompiler.Generator.Backend.CallingConventions
{
    public interface ICallingConvention
    {
        string Name { get; }

        StackPolicy Stack { get; }

        bool SupportsRipRelative { get; }

        IReadOnlyList<ArgumentRule> ArgumentRegisters { get; }
        IReadOnlyList<PhysicalRegister> ScratchRegisters { get; }
        IReadOnlyList<PhysicalRegister> CalleeSavedRegisters { get; }
        IReadOnlyList<PhysicalRegister> AddressScratchRegisters { get; }
        IReadOnlyList<PhysicalRegister> AddressCalleeSavedRegisters { get; }


        bool TryGetRole(RegisterRole role, out PhysicalRegister register);
        bool TryGetRole(RegisterRole role, out RegisterInfo register);
        RegisterInfo GetRegister(RegisterRole role, string msg);
        string EmitCall(TFAsmGen emitter, AsmOperand symbol, IReadOnlyList<AsmOperand> args);
        string EmitSyscall(TFAsmGen emitter, AsmOperand symbol, IReadOnlyList<AsmOperand> args);
    }
}