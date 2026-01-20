#namespace io

#included once

#define __windows__ as 1

#define NULL as @cast(void ptr, 0)

#if __windows__

#define STD_OUTPUT_HANDLE -11

external "kernel32.dll" func WINAPI WriteFile(hand : void ptr, buffer : void ptr, bytes : uint32, written : uint32 ptr, overlapped : void ptr) : int32

external "kernel32.dll" func WINAPI GetStdHandle(nStdHandle : uint32) : void ptr

#endif

func public print(str : string ptr) : int
{
    #if __unix__
    return @systemcall(1, 1, str, str.length)
    #elif __windows__
    var written : uint32 = 0
    call WriteFile(call GetStdHandle(STD_OUTPUT_HANDLE), str, str.length, &written, NULL)
    return written
    #endif
}