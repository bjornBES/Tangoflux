.rodata
S0: db 0x48,0x00,0x00,0x00,0x65,0x00,0x00,0x00,0x6C,0x00,0x00,0x00,0x6C,0x00,0x00,0x00,0x6F,0x00,0x00,0x00,0x20,0x00,0x00,0x00,0x54,0x00,0x00,0x00,0x61,0x00,0x00,0x00,0x6E,0x00,0x00,0x00,0x67,0x00,0x00,0x00,0x6F,0x00,0x00,0x00,0x46,0x00,0x00,0x00,0x6C,0x00,0x00,0x00,0x65,0x00,0x00,0x00,0x78,0x00,0x00,0x00,0x21,0x00,0x00,0x00,0x5C,0x00,0x00,0x00,0x74,0x00,0x00,0x00,0x20,0x00,0x00,0x00,0x5C,0x00,0x00,0x00,0x6E,0x00,0x00,0x00,0x20,0x00,0x00,0x00,0x5C,0x00,0x00,0x00,0x72,0x00,0x00,0x00,0x20,0x00,0x00,0x00,0x5C,0x00,0x00,0x00,0x6E,0x00,0x00,0x00,0x00
S1: db 0x55,0x6E,0x69,0x78,0x20,0x78,0x36,0x34,0x20,0x64,0x65,0x74,0x65,0x63,0x74,0x65,0x64,0x5C,0x6E,0x00
.text
extern strlen
; func @print(str : ptr(ptr(uint8))) : int32
global print:
print:
    push rbp
    mov rbp, rsp
    sub rsp, 48
    ; %t0 = move constI64 1

    ; %t1 = move @str
    ; [DEBUG] Allocated temp %t1 to r9
    mov r9, qword [rbp-8]

    ; %t2 = call @strlen, %t1
    mov rdi, r9
    call strlen
    ; [DEBUG] Allocated temp %t2 to r8
    mov r8, rax

    ; %t3 = sysCall constI64 1, %t0, %t1, %t2
    mov rdi, r8
    mov rsi, r9
    mov rdx, 0x1
    mov rax, 0x1
    syscall
    ; [DEBUG] Allocated temp %t3 to rcx
    mov ecx, eax

    ; ret %t3
    mov rax, rcx

    ; [DEBUG] Temp %t1 freed from r9
    ; [DEBUG] Temp %t2 freed from r8
    ; [DEBUG] Temp %t3 freed from rcx
print_end:
    add rsp, 48
    pop rbp
    ret
; func @exit(exitCode : uint32) : void
global exit:
exit:
    push rbp
    mov rbp, rsp
    sub rsp, 32
    ; %t0 = move constI64 60

    ; %t1 = move @exitCode
    ; [DEBUG] Allocated temp %t1 to rcx
    mov ecx, dword [rbp-8]

    ; %t2 = sysCall %t0, %t1
    mov rdi,    
    mov rax, 0x3c
    syscall
    ; [DEBUG] Allocated temp %t2 to r8
    mov r8d, eax

    ; ret
    xor rax, rax

    ; [DEBUG] Temp %t1 freed from rcx
    ; [DEBUG] Temp %t2 freed from r8
exit_end:
    add rsp, 32
    pop rbp
    ret
; func @main(argv : ptr(ptr(uint8)), argc : int32) : int32
global main:
main:
    push rbp
    mov rbp, rsp
    sub rsp, 416
    ; move @framebuffer, constI64 753664
    mov qword [rbp-24], 0xb8000

    ; %t0 = move constI64 -10

    ; move @x, %t0
    mov qword [rbp-32], 0xfffffffffffffff6

    ; move @y, constI32 25
    mov dword [rbp-40], 0x19

    ; %t1 = move constI64 -5

    ; move @z, %t1
    mov word [rbp-48], 0xfffffffffffffffb

    ; move @sum, constI64 0
    mov qword [rbp-56], 0x0

    ; move @arr, constI64 4096
    mov qword [rbp-64], 0x1000

    ; %t2 = move constI64 -5

    ; move @i, %t2
    mov dword [rbp-72], 0xfffffffffffffffb

    ; label for_loop_cond_0
for_loop_cond_0:

    ; %t3 = leq @i, constI32 5
    ; [DEBUG] Allocated temp %t3 to r8
    xor r8, r8
    cmp dword [rbp-72], 0x5
    setle r8b

    ; cjump constI8 1, %t3, for_loop_end_2
    test r8d, r8d
    jz for_loop_end_2

    ; label for_loop_body_1
for_loop_body_1:

    ; %t4 = move @sum
    ; [DEBUG] Allocated temp %t4 to rcx
    mov rcx, qword [rbp-56]

    ; %t5 = move @i
    ; [DEBUG] Allocated temp %t5 to r9
    mov r9d, dword [rbp-72]

    ; %t6 = add %t4, %t5
    add rcx, r9d
    ; [DEBUG] Allocated temp %t6 to rdx
    mov edx, ecx

    ; move @sum, %t6
    mov [rbp-56], edx

    ; label while_loop_cond_3
while_loop_cond_3:

    ; %t7 = move @x
    ; [DEBUG] Allocated temp %t7 to rsi
    mov rsi, qword [rbp-32]

    ; %t8 = lt %t7, constI64 0
    ; [DEBUG] Allocated temp %t8 to dword [rbp-184] on stack
    ; [DEBUG] Allocated scratch register rdi
    ;     mov rdi, dword [rbp-184]
    xor rdi, rdi
    cmp sil, byte 0x0
    setl dil
    ; [DEBUG] Releasing compare scratch register rdi

    ; cjump constI8 1, %t8, while_loop_end_5
    test dword [rbp-184], dword [rbp-184]
    jz while_loop_end_5

    ; %t9 = move @x
    ; [DEBUG] Allocated temp %t9 to qword [rbp-192] on stack
    mov rdi, qword [rbp-32]
    mov qword [rbp-192], rdi

    ; %t10 = add %t9, constI64 3
    add qword [rbp-192], qword 0x3
    ; [DEBUG] Allocated temp %t10 to dword [rbp-200] on stack
    mov edi, qword [rbp-192]
    mov dword [rbp-200], edi

    ; move @x, %t10
    mov rdi, dword [rbp-200]
    mov qword [rbp-32], rdi

    ; jump while_loop_cond_3
    jmp while_loop_cond_3

    ; label while_loop_end_5
while_loop_end_5:

    ; %t11 = move @y
    ; [DEBUG] Allocated temp %t11 to dword [rbp-208] on stack
    mov edi, dword [rbp-40]
    mov dword [rbp-208], edi

    ; %t12 = move @i
    ; [DEBUG] Allocated temp %t12 to dword [rbp-216] on stack
    mov edi, dword [rbp-72]
    mov dword [rbp-216], edi

    ; %t13 = sub %t11, %t12
   ; Invalid operand for sub
    ; [DEBUG] Allocated temp %t13 to dword [rbp-224] on stack
    mov edi, dword [rbp-208]
    mov dword [rbp-224], edi

    ; %t14 = gt %t13, constI32 20
    ; [DEBUG] Allocated temp %t14 to dword [rbp-232] on stack
    ; [DEBUG] Allocated scratch register rdi
    ;     mov rdi, dword [rbp-232]
    xor rdi, rdi
    cmp dword [rbp-224], 0x14
    setg dil
    ; [DEBUG] Releasing compare scratch register rdi

    ; cjump constI8 1, %t14, if_else_6
    test dword [rbp-232], dword [rbp-232]
    jz if_else_6

    ; label if_then_8
if_then_8:

    ; [DEBUG] Temp %t3 freed from r8
    ; [DEBUG] Temp %t4 freed from rcx
    ; [DEBUG] Temp %t5 freed from r9
    ; [DEBUG] Temp %t6 freed from rdx
    ; [DEBUG] Temp %t7 freed from rsi
    ; %t15 = move @z
    ; [DEBUG] Allocated temp %t15 to rsi
    mov si, word [rbp-48]

    ; %t16 = sub %t15, constI16 1
    sub si, word 0x1
    ; [DEBUG] Allocated temp %t16 to rdx
    mov edx, esi

    ; move @z, %t16
    mov [rbp-48], edx

    ; jump if_end_7
    jmp if_end_7

    ; label if_else_6
if_else_6:

    ; [DEBUG] Temp %t16 freed from rdx
    ; [DEBUG] Temp %t15 freed from rsi
    ; %t17 = move @z
    ; [DEBUG] Allocated temp %t17 to rsi
    mov si, word [rbp-48]

    ; %t18 = add %t17, constI16 2
    add si, byte 0x2
    ; [DEBUG] Allocated temp %t18 to rdx
    mov edx, esi

    ; move @z, %t18
    mov [rbp-48], edx

    ; label if_end_7
if_end_7:

    ; [DEBUG] Temp %t18 freed from rdx
    ; [DEBUG] Temp %t17 freed from rsi
    ; %t19 = move constI8 1

    ; %t20 = add @i, %t19
    add dword [rbp-72], dword 0x1
    ; [DEBUG] Allocated temp %t20 to rsi
    mov esi, dword [rbp-72]

    ; move @i, %t20
    mov [rbp-72], esi

    ; jump for_loop_cond_0
    jmp for_loop_cond_0

    ; label for_loop_end_2
for_loop_end_2:

    ; %t21 = addr_of @S0
    ; [DEBUG] Allocated temp %t21 to rdx
    lea rdx, [rel S0]

    ; move @testStr, %t21
    mov [rbp-80], rdx

    ; move @c, constI8 0
    mov byte [rbp-88], 0x0

    ; %t22 = move @testStr
    ; [DEBUG] Allocated temp %t22 to r9
    mov r9, qword [rbp-80]

    ; %t23 = load %t22, constI64 0
    mov r10, r9
    xor r11, r11
    ; [DEBUG] Allocated temp %t23 to rcx
    mov cl, byte [r10+r11]

    ; move @c, %t23
    mov [rbp-88], cl

    ; %t24 = addr_of @S1
    ; [DEBUG] Allocated temp %t24 to r8
    lea r8, [rel S1]

    ; %t25 = call @print, %t24
    mov rdi, r8
    call print
    ; [DEBUG] Allocated temp %t25 to dword [rbp-320] on stack
    mov [rbp-320], rax

    ; %t26 = move constI64 -9223372036854775807

    ; move @negTest, %t26
    mov qword [rbp-96], 0x8000000000000001

    ; move @overflow, constI32 2147483647
    mov dword [rbp-104], 0x7fffffff

    ; %t27 = move @overflow
    ; [DEBUG] Allocated temp %t27 to dword [rbp-336] on stack
    mov edi, dword [rbp-104]
    mov dword [rbp-336], edi

    ; %t28 = add %t27, constI32 10
    add dword [rbp-336], dword 0xa
    ; [DEBUG] Allocated temp %t28 to dword [rbp-344] on stack
    mov edi, dword [rbp-336]
    mov dword [rbp-344], edi

    ; move @overflow, %t28
    mov edi, dword [rbp-344]
    mov dword [rbp-104], edi

    ; move @intTest, constI64 0
    mov qword [rbp-112], 0x0

    ; move @intTest, constI64 7
    mov qword [rbp-112], 0x7

    ; move @intTest, constI64 42
    mov qword [rbp-112], 0x2a

    ; move @intTest, constI64 123456
    mov qword [rbp-112], 0x1e240

    ; move @intTest, constI64 1000000
    mov qword [rbp-112], 0xf4240

    ; move @intTest, constI64 42
    mov qword [rbp-112], 0x2a

    ; move @intTest, constI64 42
    mov qword [rbp-112], 0x2a

    ; move @intTest, constI64 123
    mov qword [rbp-112], 0x7b

    ; move @intTest, constI64 123
    mov qword [rbp-112], 0x7b

    ; move @intTest, constI64 99
    mov qword [rbp-112], 0x63

    ; move @intTest, constI64 99
    mov qword [rbp-112], 0x63

    ; move @intTest, constI64 1000000
    mov qword [rbp-112], 0xf4240

    ; move @intTest, constI64 0
    mov qword [rbp-112], 0x0

    ; move @intTest, constI64 26
    mov qword [rbp-112], 0x1a

    ; move @intTest, constI64 255
    mov qword [rbp-112], 0xff

    ; move @intTest, constI64 57005
    mov qword [rbp-112], 0xdead

    ; move @intTest, constI64 3735928559
    mov qword [rbp-112], 0xdeadbeef

    ; move @intTest, constI64 255
    mov qword [rbp-112], 0xff

    ; move @intTest, constI64 255
    mov qword [rbp-112], 0xff

    ; move @intTest, constI64 57005
    mov qword [rbp-112], 0xdead

    ; move @intTest, constI64 48879
    mov qword [rbp-112], 0xbeef

    ; move @intTest, constI64 3735928559
    mov qword [rbp-112], 0xdeadbeef

    ; move @intTest, constI64 0
    mov qword [rbp-112], 0x0

    ; move @intTest, constI64 7
    mov qword [rbp-112], 0x7

    ; move @intTest, constI64 10
    mov qword [rbp-112], 0xa

    ; move @intTest, constI64 63
    mov qword [rbp-112], 0x3f

    ; move @intTest, constI64 493
    mov qword [rbp-112], 0x1ed

    ; move @intTest, constI64 493
    mov qword [rbp-112], 0x1ed

    ; move @intTest, constI64 63
    mov qword [rbp-112], 0x3f

    ; move @intTest, constI64 10
    mov qword [rbp-112], 0xa

    ; move @intTest, constI64 63
    mov qword [rbp-112], 0x3f

    ; move @intTest, constI64 0
    mov qword [rbp-112], 0x0

    ; move @intTest, constI64 0
    mov qword [rbp-112], 0x0

    ; move @intTest, constI64 0
    mov qword [rbp-112], 0x0

    ; move @intTest, constI64 0
    mov qword [rbp-112], 0x0

    ; move @intTest, constI64 0
    mov qword [rbp-112], 0x0

    ; move @intTest, constI64 0
    mov qword [rbp-112], 0x0

    ; move @intTest, constI64 4095
    mov qword [rbp-112], 0xfff

    ; move @intTest, constI64 10
    mov qword [rbp-112], 0xa

    ; %t29 = move @sum
    ; [DEBUG] Allocated temp %t29 to qword [rbp-352] on stack
    mov rdi, qword [rbp-56]
    mov qword [rbp-352], rdi

    ; %t30 = move @negTest
    ; [DEBUG] Allocated temp %t30 to qword [rbp-360] on stack
    mov rdi, qword [rbp-96]
    mov qword [rbp-360], rdi

    ; %t31 = move @overflow
    ; [DEBUG] Allocated temp %t31 to dword [rbp-368] on stack
    mov edi, dword [rbp-104]
    mov dword [rbp-368], edi

    ; %t32 = add %t30, %t31
   ; Invalid operand for add
    ; [DEBUG] Allocated temp %t32 to dword [rbp-376] on stack
    mov edi, qword [rbp-360]
    mov dword [rbp-376], edi

    ; %t33 = add %t29, %t32
   ; Invalid operand for add
    ; [DEBUG] Allocated temp %t33 to dword [rbp-384] on stack
    mov edi, qword [rbp-352]
    mov dword [rbp-384], edi

    ; move @sum, %t33
    mov rdi, dword [rbp-384]
    mov qword [rbp-56], rdi

    ; %t34 = move @sum
    ; [DEBUG] Allocated temp %t34 to qword [rbp-392] on stack
    mov rdi, qword [rbp-56]
    mov qword [rbp-392], rdi

    ; %t35 = move %t34
    ; [DEBUG] Allocated temp %t35 to dword [rbp-400] on stack
    mov edi, qword [rbp-392]
    mov dword [rbp-400], edi

    ; %t36 = call @exit, %t35
    mov rdi, dword [rbp-400]
    call exit
    ; [DEBUG] Allocated temp %t36 to dword [rbp-408] on stack
    mov [rbp-408], rax

    ; ret
    xor rax, rax

    ; [DEBUG] Temp %t24 freed from r8
    ; [DEBUG] Temp %t23 freed from rcx
    ; [DEBUG] Temp %t22 freed from r9
    ; [DEBUG] Temp %t21 freed from rdx
    ; [DEBUG] Temp %t20 freed from rsi
main_end:
    add rsp, 416
    pop rbp
    mov rdi, rax
    mov rax, 60
    syscall