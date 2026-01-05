// need an inculde preprocess thing

extern func FuncOutsideHere() : void

func OtherFuncs(first : uint8, _2nd : uint8) : void {

}

// Tango flex program

var ConstValue : const int = 0x55AA

// structs are costom types to the type system
struct uint24 {
    var value : uint8[3]
}

struct SomeOtherStruct {
    var id : uint8
    var name : string ptr
}

var SomeOtherFunc : void ptr (uint8, uint8)

// here int and int32 is the same type
func main(argc : int32, argv : string ptr) : int {
    var framebuffer : uint8 ptr = ptr 0xb8000
    for (var i : int = 0 .. 1000)
    {
        framebuffer[i] = 0
    }

    SomeOtherFunc = ptr OtherFuncs
    SomeOtherFunc(1, 2)

    var newFunc : int ptr (uint8) = ptr 0x1000_0000
    newFunc(1)

    // made the function and called it
    //  - (void ptr ()) ptr 0x1234
    //      - (void ptr ()) cast into a void ptr with 0 params
    //      - ptr 0x1234 address 0x1234
    //      - in conclusion we case 0x1234 into a function void ptr with 0 params
    //  -  ((void ptr ()) ptr 0x1234) ()
    //      - () call address 0x1234 with 0 params
    ((void ptr ()) ptr 0x1234) ()

    // if it's a struct with 1 field in it you can do this
    var CCantDoThis : uint24 = 0xFFFFFF

    // you can't do
    var ThisWouldGiveAnError : SomeOtherStruct = 1  // error
}
