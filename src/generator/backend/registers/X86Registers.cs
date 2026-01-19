public static class X86Registers
{

    public static Dictionary<string[], RegisterInfo[]> RegisterMap = new Dictionary<string[], RegisterInfo[]>
    {
        {
            ["rax", "eax", "ax", "al", "ah"],
            new RegisterInfo[]
            {
                new RegisterInfo("rax", 0),
                new RegisterInfo("eax", 0),
                new RegisterInfo("ax", 0),
                new RegisterInfo("al", 0),
                new RegisterInfo("ah", 0),
            }
        },
        {
            ["rbx", "ebx", "bx", "bl", "bh"],
            new RegisterInfo[]
            {
                new RegisterInfo("rbx", 1),
                new RegisterInfo("ebx", 1),
                new RegisterInfo("bx", 1),
                new RegisterInfo("bl", 1),
                new RegisterInfo("bh", 1),
            }
        },
        {
            ["rcx", "ecx", "cx", "cl", "ch"],
            new RegisterInfo[]
            {
                new RegisterInfo("rcx", 2),
                new RegisterInfo("ecx", 2),
                new RegisterInfo("cx", 2),
                new RegisterInfo("cl", 2),
                new RegisterInfo("ch", 2),
            }
        },
        {
            ["rdx", "edx", "dx", "dl", "dh"],
            new RegisterInfo[]
            {
                new RegisterInfo("rdx", 3),
                new RegisterInfo("edx", 3),
                new RegisterInfo("dx", 3),
                new RegisterInfo("dl", 3),
                new RegisterInfo("dh", 3),
            }
        },
        {
            ["rsi", "esi", "si", "sil"],
            new RegisterInfo[]
            {
                new RegisterInfo("rsi", 6),
                new RegisterInfo("esi", 6),
                new RegisterInfo("si", 6),
                new RegisterInfo("sil", 6),
            }
        },
        {
            ["rdi", "edi", "di", "dil"],
            new RegisterInfo[]
            {
                new RegisterInfo("rdi", 7),
                new RegisterInfo("edi", 7),
                new RegisterInfo("di", 7),
                new RegisterInfo("dil", 7),
            }
        },
        {
            ["rbp", "ebp", "bp", "bpl"],
            new RegisterInfo[]
            {
                new RegisterInfo("rbp", 5),
                new RegisterInfo("ebp", 5),
                new RegisterInfo("bp", 5),
                new RegisterInfo("bpl", 5),
            }
        },
{
            ["rsp", "esp", "sp", "spl"],
            new RegisterInfo[]
            {
                new RegisterInfo("rsp", 4),
                new RegisterInfo("esp", 4),
                new RegisterInfo("sp", 4),
                new RegisterInfo("spl", 4),
            }
        },
        {
            ["r8", "r8d", "r8w", "r8b"],
            new RegisterInfo[]
            {
                new RegisterInfo("r8", 8),
                new RegisterInfo("r8d", 8),
                new RegisterInfo("r8w", 8),
                new RegisterInfo("r8b", 8),
            }
        },
        {
            ["r9", "r9d", "r9w", "r9b"],
            new RegisterInfo[]
            {
                new RegisterInfo("r9", 9),
                new RegisterInfo("r9d", 9),
                new RegisterInfo("r9w", 9),
                new RegisterInfo("r9b", 9),
            }
        },
        {
            ["r10", "r10d", "r10w", "r10b"],
            new RegisterInfo[]
            {
                new RegisterInfo("r10", 10),
                new RegisterInfo("r10d", 10),
                new RegisterInfo("r10w", 10),
                new RegisterInfo("r10b", 10),
            }
        },
        {
            ["r11", "r11d", "r11w", "r11b"],
            new RegisterInfo[]
            {
                new RegisterInfo("r11", 11),
                new RegisterInfo("r11d", 11),
                new RegisterInfo("r11w", 11),
                new RegisterInfo("r11b", 11),
            }
        },
        {
            ["r12", "r12d", "r12w", "r12b"],
            new RegisterInfo[]
            {
                new RegisterInfo("r12", 12),
                new RegisterInfo("r12d", 12),
                new RegisterInfo("r12w", 12),
                new RegisterInfo("r12b", 12),
            }
        },
        {
            ["r13", "r13d", "r13w", "r13b"],
            new RegisterInfo[]
            {
                new RegisterInfo("r13", 13),
                new RegisterInfo("r13d", 13),
                new RegisterInfo("r13w", 13),
                new RegisterInfo("r13b", 13),
            }
        },
        {
            ["r14", "r14d", "r14w", "r14b"],
            new RegisterInfo[]
            {
                new RegisterInfo("r14", 14),
                new RegisterInfo("r14d", 14),
                new RegisterInfo("r14w", 14),
                new RegisterInfo("r14b", 14),
            }
        },
        {
            ["r15", "r15d", "r15w", "r15b"],
            new RegisterInfo[]
            {
                new RegisterInfo("r15", 15),
                new RegisterInfo("r15d", 15),
                new RegisterInfo("r15w", 15),
                new RegisterInfo("r15b", 15),
            }
        },
    };

    public static RegisterInfo GetRegisterByte(RegisterInfo register, bool high = false)
    {
        string[] key = RegisterMap.Keys.Select(keys => keys.Contains(register.Name) ? keys : null).First(k => k != null);

        RegisterInfo result;
        result = RegisterMap[key][3];
        if (high == true)
        {
            result = RegisterMap[key][4];
        }
        result.Index = register.Index;
        return result;
    }
    public static RegisterInfo GetRegisterWord(RegisterInfo register)
    {
        string[] key = RegisterMap.Keys.Select(keys => keys.Contains(register.Name) ? keys : null).First(k => k != null);

        RegisterInfo result;
        result = RegisterMap[key][2];
        result.Index = register.Index;
        return result;
    }
    public static RegisterInfo GetRegisterDword(RegisterInfo register)
    {
        string[] key = RegisterMap.Keys.Select(keys => keys.Contains(register.Name) ? keys : null).First(k => k != null);

        RegisterInfo result;
        result = RegisterMap[key][1];
        result.Index = register.Index;
        return result;
    }
    public static RegisterInfo GetRegisterLong(RegisterInfo register)
    {
        string[] key = RegisterMap.Keys.Select(keys => keys.Contains(register.Name) ? keys : null).First(k => k != null);

        RegisterInfo result;
        result = RegisterMap[key][0];
        result.Index = register.Index;
        return result;
    }

    public static bool IsByteRegister(RegisterInfo register)
    {
        string[] key = RegisterMap.Keys.Select(keys => keys.Contains(register.Name) ? keys : null).First(k => k != null);
        return RegisterMap[key].Any(r => r.Name == register.Name && (r == RegisterMap[key][3] || (RegisterMap[key].Length > 4 && r == RegisterMap[key][4])));
    }
    public static bool IsWordRegister(RegisterInfo register)
    {
        string[] key = RegisterMap.Keys.Select(keys => keys.Contains(register.Name) ? keys : null).First(k => k != null);
        return RegisterMap[key].Any(r => r.Name == register.Name && r == RegisterMap[key][2]);
    }
    public static bool IsDwordRegister(RegisterInfo register)
    {
        string[] key = RegisterMap.Keys.Select(keys => keys.Contains(register.Name) ? keys : null).First(k => k != null);
        return RegisterMap[key].Any(r => r.Name == register.Name && r == RegisterMap[key][1]);
    }
    public static bool IsLongRegister(RegisterInfo register)
    {
        string[] key = RegisterMap.Keys.Select(keys => keys.Contains(register.Name) ? keys : null).First(k => k != null);
        return RegisterMap[key].Any(r => r.Name == register.Name && r == RegisterMap[key][0]);
    }
}