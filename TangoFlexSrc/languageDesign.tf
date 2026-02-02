
// declare function
func <visibility> <calling convention> <NAME>(<parameters>) : <TYPE> {
    <scope>
}
// parameters are structured as so
<NAME> : <TYPE>

// declare variable

// local
// in a function declare it as so
var <visibility> <NAME> : <TYPE> = <EXPR>
var <visibility> <NAME> : <TYPE>

// outside a function declare it as so
// global
var <visibility> <NAME> : <TYPE> = <EXPR>
var <visibility> <NAME> : <TYPE>
// globals are 0 by default and are in the bss at code gen

// return
return <EXPR>
// if a function has a type that is not void return is needed

// types
// the default type for a variable is an int32
// the default type for a function is a void
// there are 11 default types in tango flex
// void, int8, uint8, int16, uint16, int32, uint32, int64 and uint64
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
// strings are a way to define strings
// to get a char from a string they can be indexed like so
string str = "Hello world"
var c : uint8 = str[0]
// general syntax 
str[<EXPR>]
// it will return either as uint8, uint16 or uint32
// depending on the strings char size
// string size and type lowering
// if the --fat-strings flag is present
// strings will be structs defined like this
struct string
{
    var length : uint16
    var value : uint8 ptr
} 
// if the --fat-strings flag is not present
// strings will be syntactic sugar for uint8 ptr and are treated as cstring
//
// other types of strings are for example
// cstring: are the same as c's string type

// string literals
// string literals are defined as so
"Hello world"
// string literals can also have a different size
u8"Hello world"
// is a byte string
u16"Hello world"
// is a 2 byte string where each char is 2 bytes
u32"Hello world"
// is a 4 byte string where each char is 4 bytes

// booleans
// booleans are 1 byte only so a uint8
// true is 1 and false is 0

// Arrays
// arrays are defined as so
var <NAME> : <TYPE> array <EXPR> 
// like this
var arrayVar : uint32 array 10
// saying it's an uint32 array with 10 elements

// variable reassignment rules
// a variable can be reassignment to other values*
// local variable can be assigned a value when if the syntax is
var local : uint32
// a local variable not assigned to a value by default is set to 0
// and as for globals they are set to 0 in the bss
local = 10020
// * note that const variable can not by reassigned

// const
// a const variable is readonly after init
var constVar : const uint64 = 982734
constVar = 123 // this is not allowed
// but const variables can be changed using pointers
var pConstVar : uint64 ptr = &constVar
*pConstVar = 123
// if this is done in any way use the
// #ChangingConst enable preprocessor flag
// the flag can also be disabled by
// #ChangingConst disable
// if the flag is not used the IR will try to lower
// it into it's base value
// by default it's enabled
// const variables can be assigned be expressions 

// integer lowering
// int literals and const expressions are folded/evaluated at compile time

// casting
// Explicit casting is done using an intrinsic like so
@cast(<TYPE>, <EXPR>)
// Implicit casting is done like so
<TYPE> <EXPR>

// function calls
// to call a function use the CALL keyword * like so
call <NAME>(<parameters>)
// in the parsers there are 2 types of function calls
// a NodeStmtFuncCall and NodeExprFuncCall
// all functions return in there return register
// x86: eax/rax
// parameters are passed in as per the abi's rule.
// * using the --disable-call flag will disable this requirement

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

// switch (WIP)
// syntax may be changed
switch (<EXPR>)
{ // not a new scope
    case 1
    { // new scope
        break // "exit" the switch
    }
    case 2
    {
        // if a break or return is not present
        // parser will send in an warning
        // and add in a break by default
    }
    default
    { // new scope
        return <EXPR> // will jump down to the function exit
    }
}

// defer (WIP)
// defer will be a stack of stmt in a new scope
// schedules a block to execute when the function exits
// syntax
defer
{ // new scope
    <STMT>
}

// external

// external func
// syntax
external "<library>" func <CallingConventions> <NAME>(<parameters>) : <TYPE>

// external variables
external var <NAME> : <TYPE>

// if stmt

// syntax
if (<EXPR>)
{
    <SCOPE>
}

else if (<EXPR>)
{
    <SCOPE>
}

else
{
    <SCOPE>
}

// non zero expressions evaluate as true
// as for V1

// for

// syntax
for (var <NAME> : <TYPE> == <EXPR>..<EXPR>)
for (var <NAME> : <TYPE> == <EXPR>..<EXPR> step <EXPR>)
for (<EXPR> == <EXPR>..<EXPR>)
for (<EXPR> == <EXPR>..<EXPR> step <EXPR>)

// while

// syntax
while (<EXPR>)