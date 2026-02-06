#namespace io
{

#included once

#define NULL as @cast(void ptr, 0)

#if __windows__

#define STD_OUTPUT_HANDLE -11

external "kernel32.dll" func __attribute__(WINAPI) WriteFile(hand : void ptr, buffer : void ptr, bytes : uint32, written : uint32 ptr, overlapped : void ptr) : int32

external "kernel32.dll" func __attribute__(WINAPI) GetStdHandle(nStdHandle : uint32) : void ptr

#endif

external func strlen(str : string ptr) : uint64

func public print(str : string ptr) : int32
{
    #if __unix__
    #ifdef __fat_strings__
    return @systemcall(1, 1, str, str->length)
    #else
    return @systemcall(1, 1, str, call strlen(str))
    #endif
    #elif __windows__
    var written : uint32 = 0
    call WriteFile(call GetStdHandle(STD_OUTPUT_HANDLE), str, call strlen(str), &written, NULL)
    return written
    #endif
}

func public read(buffer : uint8 ptr, size : uint32) : uint32
{
    return 0
}

func public exit(exitCode : uint32) : void
{
    #if __unix__
    @systemcall(@cast(uint32, 60), exitCode)
    #endif
}
}