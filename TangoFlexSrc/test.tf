#extension io

#define Hello_World as 0

func main(argv : uint8 ptr ptr, argc : int32):int
{
    #if Hello_World == 0
    {
        var framebuffer : uint8 ptr = 0xb8000
    }
    #endif
    var test : int = 0
    if (test == 0)
    {
        if (test == 1)
        {
            return 26
        }
        for (var i : int = 0 .. 10 step 1)
        {
            while (test != 10)
            {
                test += 1
            }
            test = 0
            if (test == 1)
            {
                return 1 + 2 + 5
            }
        }
    }
    else
    {
        return 15
    }

    var testStr : string = "Hello world"
    var c : uint8 = 0
    c = testStr[0]

    return 23
}
