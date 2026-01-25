
// declare function
func <visibility> <calling convation> <NAME>(<parameters>) : <TYPE> {
    <scope>
}
// parameters are structered as so
<NAME> : <TYPE>

// declare variabel

// local
// in a function declare it as so
var <visibility> <NAME> : <TYPE> = <EXPR>
var <visibility> <NAME> : <TYPE>

// outside a function declare it as so
// global
var <visibility> <NAME> : <TYPE>
// globals are 0 by defualt and are in the bss at code gen

// return
return <EXPR>
// if a function has a type that is not void return is needed

// types
// the defualt type for a variabel is an int32
// the defualt type for a function is a void
// there are 11 defualt types in tango flex
// void, bool, int8, uint8, int16, uint16, int32, uint32, int64 and uint64
// the ptr keyword can be added at the end of a type
void ptr
uint8 ptr
// making it a pointer
// all number types e.g. int8, uint8, int16, uint16, int32, uint32, int64 and uint64
// are all a fixed width like 1 byte to 8 bytes (1 byte is 8 bits)
// int8 and uint8 1 byte
// int16 and uint16 2 byte
// int32 and uint32 4 byte
// int64 and uint64 8 byte
// in later versions of tango flex there could be a 128 bit type

// strings
// strings are just syntactic sugar for uint8 ptr with a length field

// casting
// casting is done using an intrinsic like so
@cast(<TYPE>, <EXPR>)

// function calls
// to call a function use the CALL keyword like so
call <NAME>(<parameters>)
// in the parsers there are 2 types of function calls
// a NodeStmtFuncCall and NodeExprFuncCall
// all functions return in there return register
// x86: eax/rax
// parameters are passed in as per the abi's rule.

// binary operators
// all binary operators has a precedence
// MULT, DIV and MOD have 7
// ADD and SUB have 6
// LT, LEQ, GT or GEQ have 5
// EQ and NEQ have 4
// BITAND have 3
// XOR have 2
// BITOR have 1
// AND have 0
// OR have -1

// compound operators
// All compound operators like +=, -=, *=, /=, %=, &=, |=, ^=, <<= and >>=
// are all parsed as the first thing

// field access
// field access can be Direct .
// or Indirect ->
// like so
var a : string ptr = @cast(string ptr, 0x1000)
a->Length // if it is an array it will take from the first element.
// or
var a : string = @cast(string, 0x1000)
a.Length

// 