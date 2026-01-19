using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Prologue/Epilogue generator. Returns assembly as a string with one instruction per line.
/// </summary>
public static class PrologueEpilogueGenerator
{
    /// <summary>
    /// Generate the prologue assembly lines for the given frame and convention.
    /// </summary>
    public static string GeneratePrologue(FunctionFrame frame, ICallingConvention conv, RegisterProfile profile)
    {
        var lines = new List<string>();
        // nice label
        lines.Add($"global {frame.FunctionName}:");
        lines.Add($"{frame.FunctionName}:");

        // choose per-profile callee-saved default order (push order). We will push only those the function uses.
        var defaultCalleeSaved = GetDefaultCalleeSaved(profile, conv);

        // intersection in the default order
        var toSave = defaultCalleeSaved.Where(r => frame.CalleeSavedUsed.Any(u => u.Name == r.Name)).ToList();

        // If using frame pointer for this ABI: push old fp and set new fp
        if (frame.UseFramePointer && FramePointerRegister(conv, profile) is RegisterInfo fp)
        {
            // push FP (common pattern: push rbp; mov rbp,rsp)
            lines.Add($"    push {conv.GetRegisterName(fp)}");
            lines.Add($"    mov {conv.GetRegisterName(fp)}, rsp"); // x86: mov rbp, rsp ; ARM would be different
        }

        // push callee-saved registers (order: as listed). Some ABIs push after setting fp like GCC does.
        foreach (var r in toSave)
            lines.Add($"    push {conv.GetRegisterName(r)}");

        // compute total stack space needed for locals + red zone / saved registers padding
        int savedCount = toSave.Count;
        // each push is pointer size; decide pointer size from profile
        int pointerSize = PointerSize(profile);
        int savedBytes = savedCount * pointerSize;

        // If we didn't create FP, savedBytes might be different (fp pushed too if used)
        // Note: if FP is pushed above, that was another push accounted for implicitly by being pushed prior; we accounted separately with FramePointerRegister push.
        // But for stack adjustment compute only local space (we already pushed saved regs).
        int locals = frame.LocalSize;

        // Align total stack change so that (rsp after prologue) % alignment == 0
        // Current rsp after pushes = rsp_original - (push_count * pointerSize).
        // We must ensure that when making a call, rsp is aligned. Simpler approach:
        // compute padding = (alignment - (locals % alignment)) % alignment
        int alignment = Math.Max(frame.StackAlignment, PointerSize(profile));
        int padding = ((alignment - (locals % alignment)) % alignment);
        int totalSub = locals + padding;

        if (totalSub > 0)
        {
            // use sub rsp instruction (x86 syntax)
            if (profile == RegisterProfile.X86_64_SysV || profile == RegisterProfile.X86_32_Cdecl)
            {
                lines.Add($"    sub rsp, {totalSub}");
            }
            else if (profile == RegisterProfile.ARM64_Linux || profile == RegisterProfile.ARM32)
            {
                // placeholder style for ARM
                lines.Add($"    // reserve {totalSub} bytes for locals (ARM skeleton)");
                lines.Add($"    sub sp, sp, #{totalSub}");
            }
            else
            {
                lines.Add($"    sub sp, sp, #{totalSub}   // unknown profile");
            }
        }

        // done
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Generate the epilogue for the given frame & conv.
    /// </summary>
    public static string GenerateEpilogue(FunctionFrame frame, ICallingConvention conv, RegisterProfile profile)
    {
        var lines = new List<string>();

        // Build list of callee-saved registers preserved earlier, in reverse order for popping
        var defaultCalleeSaved = GetDefaultCalleeSaved(profile, conv);
        var toSave = defaultCalleeSaved.Where(r => frame.CalleeSavedUsed.Any(u => u.Name == r.Name)).ToList();

        int pointerSize = PointerSize(profile);
        int locals = frame.LocalSize;
        int alignment = Math.Max(frame.StackAlignment, pointerSize);
        int padding = ((alignment - (locals % alignment)) % alignment);
        int totalSub = locals + padding;

        // Restore stack space first (if we subbed)
        if (totalSub > 0)
        {
            if (profile == RegisterProfile.X86_64_SysV || profile == RegisterProfile.X86_32_Cdecl)
            {
                lines.Add($"    add rsp, {totalSub}");
            }
            else if (profile == RegisterProfile.ARM64_Linux || profile == RegisterProfile.ARM32)
            {
                lines.Add($"    add sp, sp, #{totalSub}    // ARM skeleton");
            }
            else
            {
                lines.Add($"    add sp, sp, #{totalSub}");
            }
        }

        // Pop callee-saved in reverse of pushes
        foreach (var r in Enumerable.Reverse(toSave))
            lines.Add($"    pop {conv.GetRegisterName(r)}");

        // Restore frame pointer if we set it
        if (frame.UseFramePointer && FramePointerRegister(conv, profile) is RegisterInfo fp)
        {
            // pop fp; ret
            lines.Add($"    pop {conv.GetRegisterName(fp)}");
        }

        // exit instruction (ABI dependent)
        if (profile == RegisterProfile.X86_64_SysV || profile == RegisterProfile.X86_32_Cdecl)
        {
            if (frame.FunctionName == "main")
            {
                lines.Add($"    mov rdi, rax");
                lines.Add($"    mov rax, 60");
                lines.Add($"    syscall");
            }
            else
            {
                lines.Add($"    ret");
            }
        }
        else if (profile == RegisterProfile.ARM64_Linux)
        {
            lines.Add($"    ret    // ARM64 return (uses lr)");
        }
        else
        {
            lines.Add($"    ret");
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Default callee-saved set and preferred order for pushing (matching common ABIs).
    /// Order matters: push sequence -> pop reversed in epilogue.
    /// </summary>
    private static List<RegisterInfo> GetDefaultCalleeSaved(RegisterProfile profile, ICallingConvention conv)
    {
        switch (profile)
        {
            case RegisterProfile.X86_64_SysV:
                // order: rbx, r12, r13, r14, r15  (common GCC/clang saving order after rbp)
                return new List<RegisterInfo>
                    {
                        new RegisterInfo("rbx", 0),
                        new RegisterInfo("r12", 12),
                        new RegisterInfo("r13", 13),
                        new RegisterInfo("r14", 14),
                        new RegisterInfo("r15", 15),
                    };

            case RegisterProfile.X86_32_Cdecl:
                return new List<RegisterInfo>
                    {
                        new RegisterInfo("ebx", 0),
                        new RegisterInfo("esi", 1),
                        new RegisterInfo("edi", 2),
                    };

            case RegisterProfile.ARM64_Linux:
                // ARM64 callee-saved x19..x29 (x29 is fp) typically saved. We return an empty list for skeleton;
                // generator will respond to frame.CalleeSavedUsed when ARM backend implements full register objects.
                return new List<RegisterInfo>
                    {
                        new RegisterInfo("x19", 19),
                        new RegisterInfo("x20", 20),
                        new RegisterInfo("x21", 21),
                        new RegisterInfo("x22", 22),
                        new RegisterInfo("x23", 23),
                        new RegisterInfo("x24", 24),
                        new RegisterInfo("x25", 25),
                        new RegisterInfo("x26", 26),
                        new RegisterInfo("x27", 27),
                        new RegisterInfo("x28", 28),
                        new RegisterInfo("x29", 29), // fp
                    };

            default:
                return new List<RegisterInfo>();
        }
    }

    private static int PointerSize(RegisterProfile profile)
    {
        switch (profile)
        {
            case RegisterProfile.X86_64_SysV:
            case RegisterProfile.ARM64_Linux:
                return 8;
            case RegisterProfile.X86_32_Cdecl:
            case RegisterProfile.ARM32:
                return 4;
            default:
                return 8;
        }
    }

    private static RegisterInfo FramePointerRegister(ICallingConvention conv, RegisterProfile profile)
    {
        // attempt to ask convention by symbolic name if implemented, otherwise fallback
        switch (profile)
        {
            case RegisterProfile.X86_64_SysV: return new RegisterInfo("rbp", 0);
            case RegisterProfile.X86_32_Cdecl: return new RegisterInfo("ebp", 0);
            case RegisterProfile.ARM64_Linux: return new RegisterInfo("x29", 29);
            case RegisterProfile.ARM32: return new RegisterInfo("r11", 11); // common frame pointer on ARM32
            default: return null;
        }
    }
}
