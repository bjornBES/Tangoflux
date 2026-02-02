func main(argc : int32, argv : cstring ptr) : int
{
    // strings are structs that look something like this
    // struct string {
    // var length : uint32
    // [default]
    // var str : byte ptr
    // }

    // struct cstring {
    // [default]
    // var str : byte ptr
    // }

    // so if you make a string
    var str : string = "Hello world"
    // then you can write str.length to get the length
    var strLength : int = str.length

    var str2 : string = str
    // this can also be done but
    var str2 : string = str.value
    // this will lead to an error
    // string.value is a byte ptr

    // if this is needed the following expr
    // can be used
    var str2 : string = string::value = str.value
    // this means
    // var str2 : string making a new string in memory
    // string::value from this get value
    // = str.value set from str.value
    // length is 0 in this case
    // you'll need this following expr to set the length
    var str2 : string = string::length = str.length
    // or
    str2.length = str.length

    // if a string is a pointer
    // it can't carry any information
    // length is always external or explicit

    // notes
    // string data lives in the data section
    // string handles/pointer live on the stack
    // a pointer to the string is in the stack

    // by default
    // string.value is 0
    // string.length is 0

    // if the --fat-string flag is pass the string
    // will have the value and length fields
    // if not it will be treated as a cstring
}