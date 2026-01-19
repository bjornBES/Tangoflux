
#namespace main
// 

#define winCall external "kernel32.dll" stdcall
#define winCall as external "kernel32.dll" stdcall

external func "[lib, path]" [ABI] [name]([arguments]) : [type]

external func "kernel32.dll" stdcall MessageBoxA(hwnd : void ptr, text : cstring, caption : cstring, flags : uint32) : int32
// saying external function MessageBoxA(hwnd : void ptr, text : cstring, caption : cstring, flags : uint32) returning int32 from kernel32 using the stdcall ABI
// the that we say for a tango flex function

func stdcall TangoFlexFunction(number : int) : byte
// saying function TangoFlexFunction(number : int) returning byte from FILE using the stdcall ABI

external func cdecl printf(cstring fmt, params) : int

// this explicit saying
#define MessageBoxA as MsgBox
// or like c's define
#define MessageBoxA MsgBox

// External data
external var [Name] : [Type]