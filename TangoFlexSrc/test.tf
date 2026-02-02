#extension io
#define Hello_World as 1

var globalVar : uint16

func main(argv : uint8 ptr ptr, argc : int32) : int32
{
    
    #if Hello_World == 1
    {
        var framebuffer : int64 = 0xb_8000
    }
    #endif

    var x : int64 = -10
    var y : int32 = 25
    var z : int16 = -5
    var sum : int64 = 0
    var arr : int32 ptr = 0x1000

    for (var i : int32 = -5 .. 5 step 1)
    {
        sum += i
        while (x < 0)
        {
            x += 3
        }

        if (y - i > 20)
        {
            z -= 1
        }
        else
        {
            z += 2
        }
    }

    var testStr : string = u32"Hello TangoFlex!\t \n \r \n"
    var c : int8 = 0
    c = testStr[0]
    // var name : int32 = call 

    #if __windows__
    call print("Windows x64 detected\r\n")
    #elif __unix__
    call print("Unix x64 detected\n")
    #else
    call print("Unknown OS\n")
    #endif

    var negTest : int64 = -9223372036854775807
    var overflow : int32 = 2147483647
    overflow += 10

    var intTest : uint64 = 0
    intTest = 7
    intTest = 42
    intTest = 123456
    intTest = 1_000_000
    intTest = 42u
    intTest = 42U
    intTest = 123l
    intTest = 123L
    intTest = 99ul
    intTest = 99UL
    intTest = 1_000_000u
    intTest = 0x0
    intTest = 0x1A
    intTest = 0xFF
    intTest = 0xdead
    intTest = 0xDEAD_BEEF
    intTest = 0xFFu
    intTest = 0xFFU
    intTest = 0xdeadl
    intTest = 0xBEEFul
    intTest = 0xDEAD_BEEFUL
    intTest = 00
    intTest = 07
    intTest = 012
    intTest = 077
    intTest = 0755
    intTest = 0755u
    intTest = 077U
    intTest = 012l
    intTest = 077ul
    intTest = 0u
    intTest = 0l
    intTest = 0ul
    intTest = 0x0ul
    intTest = 0b0u
    intTest = 0_0_0
    intTest = 0xF_F_F
    intTest = 0b1_0_1_0

    sum += negTest + overflow

    

    call exit(@cast(int32, sum))
    return 0
}