#namespace io

#inclued once

#if __windows__

#define STD_OUTPUT_HANDLE (int32)-11

external func "kernel32.dll" WINAPI WriteFile(var hand : void ptr, var buffer : void ptr, var bytes : uint32, var written : uint32 ptr, var overlapped : void ptr) : int32

external func "kernel32.dll" WINAPI GetStdHandle(var nStdHandle : uint32) : void ptr

#endif

func public print(var str : string ptr) : int
{
    #if __unix__
    return @systemcall(1, 1, str, str.length)
    #else
    var written : uint32
    WriteFile(GetStdHandle(STD_OUTPUT_HANDLE), str, str.length, &written, NULL)
    return written
    #endif
}