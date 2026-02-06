struct public Arena {
    var base : uint8 ptr
    var ptr : uint8 ptr
    var end : uint8 ptr
}

func public os_alloc(size : uint64) : uint32
{
    
}

func public arena_init(a : Arena ptr, size : uint64) : void {
    a->base = os_alloc(size)   // mmap / VirtualAlloc
    a->ptr  = a->base
    a->end  = a->base + size
}

func public arena_alloc(a : Arena ptr, size : uint64, align : uint32) : void ptr {
    var p : void ptr = @cast(void ptr, a->ptr)
    p = (p + align - 1) & ~(align - 1)

    if (p + size > @cast(void ptr, a->end))
    {
        panic("arena overflow")
    }

    a->ptr = @cast(uint8 ptr, (p + size))
    return @cast(void ptr, p)
}

func public arena_reset(a : Arena ptr) : void {
    a->ptr = a->base
}
