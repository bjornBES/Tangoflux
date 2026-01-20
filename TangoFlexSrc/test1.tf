#define Test as 1
#undef Test

#ifdef Test
func main(argv : uint8 ptr ptr, argc : uint8) : int
{
    var test : int = 0
    for (var i : int = 0 .. 10 step 1)
    {
    }
}
#elif !Test
func case2() : void
{

}
#else
func new_main() : void
{

}
#endif