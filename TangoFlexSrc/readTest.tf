#extension io

func public cdecl main() : int32 {

    var buffer : uint8 array 64
    var i : uint32 = 0

    while (i < 3) {

        call print("Enter text: ")

        // read returns number of bytes read
        var bytesRead : int32 = call read(buffer, 63)

        // EOF or error
        if (bytesRead <= 0) {
            break
        }

        // null-terminate
        buffer[bytesRead] = 0

        call print("You typed: ")
        call print(buffer)
        call print("\n")

        i = i + 1
    }

    call exit(0)
    return 0
}
