.data
S0: db "Hello world"
S0_Length: dw $-S0
.text
; func @main(argv : ptr(ptr(uint8)), argc : int32) : int32
global main:
main:
    push rbp
    mov rbp, rsp
    sub rsp, 208
    mov dword [rbp-24], 753664

    mov dword [rbp-32], 0

    ; [DEBUG] Allocated temp %t0 to r11
    mov r11d, dword [rbp-32]

    ; [DEBUG] Allocated temp %t1 to rcx
    xor rcx, rcx
    cmp r11b, byte 0
    sete cl

    test ecx, ecx
    jz if_else_0

if_then_2:

    ; [DEBUG] Temp %t0 freed from r11
    ; [DEBUG] Temp %t1 freed from rcx
    ; [DEBUG] Allocated temp %t2 to rcx
    mov ecx, dword [rbp-32]

    ; [DEBUG] Allocated temp %t3 to r11
    xor r11, r11
    cmp cl, byte 1
    sete r11b

    test r11d, r11d
    jz if_else_3

if_then_5:

    ; [DEBUG] Temp %t3 freed from r11
    ; [DEBUG] Temp %t2 freed from rcx
    mov rax, 26
    jmp main_end

if_else_3:

    jmp if_end_4

if_end_4:

    mov dword [rbp-40], 0

for_loop_cond_6:

    ; [DEBUG] Allocated temp %t4 to rcx
    xor rcx, rcx
    cmp dword [rbp-40], 10
    setle cl

    test ecx, ecx
    jz for_loop_end_8

for_loop_body_7:

while_loop_cond_9:

    ; [DEBUG] Allocated temp %t5 to r11
    mov r11d, dword [rbp-32]

    ; [DEBUG] Allocated temp %t6 to r9
    xor r9, r9
    cmp r11b, byte 10
    setne r9b

    test r9d, r9d
    jz while_loop_end_11

    ; [DEBUG] Allocated temp %t7 to r8
    mov r8d, dword [rbp-32]

    add r8d, byte 1
    ; [DEBUG] Allocated temp %t8 to rcx
    mov ecx, r8d

    mov dword [rbp-32], ecx

    jmp while_loop_cond_9

while_loop_end_11:

    mov dword [rbp-32], 0

    ; [DEBUG] Allocated temp %t9 to rdx
    mov edx, dword [rbp-32]

    ; [DEBUG] Allocated temp %t10 to dword [rbp-144] on stack
    ; [DEBUG] Allocated scratch register rdi
    mov rdi, dword [rbp-144]
    xor rdi, rdi
    cmp dl, byte 1
    sete dil
    ; [DEBUG] Releasing compare scratch register rdi

    test dword [rbp-144], dword [rbp-144]
    jz if_else_12

if_then_14:

    ; [DEBUG] Temp %t5 freed from r11
    ; [DEBUG] Temp %t4 freed from rcx
    ; [DEBUG] Temp %t6 freed from r9
    ; [DEBUG] Temp %t7 freed from r8
    ; [DEBUG] Temp %t8 freed from rcx
    ; [DEBUG] Temp %t9 freed from rdx
    ; [DEBUG] Allocated scratch register rdx
    mov rdx, 1
    add rdx, byte 2
    ; [DEBUG] Allocated temp %t11 to rcx
    mov ecx, edx
    ; [DEBUG] Releasing arithmetic scratch register rdx

    add ecx, byte 5
    ; [DEBUG] Allocated temp %t12 to rdx
    mov edx, ecx

    mov rax, rdx
    jmp main_end

if_else_12:

    ; [DEBUG] Temp %t12 freed from rdx
    ; [DEBUG] Temp %t11 freed from rcx
    jmp if_end_13

if_end_13:


    add dword [rbp-40], dword 1
    ; [DEBUG] Allocated temp %t14 to rcx
    mov ecx, dword [rbp-40]

    mov dword [rbp-40], ecx

    jmp for_loop_cond_6

for_loop_end_8:

    jmp if_end_1

if_else_0:

    ; [DEBUG] Temp %t14 freed from rcx
    mov rax, 15
    jmp main_end

if_end_1:

    lea r10, [rel S0]
    ; [DEBUG] Allocated temp %t15 to rcx
    mov rcx, r10

    mov qword [rbp-48], rcx

    mov byte [rbp-56], 0

    ; [DEBUG] Allocated temp %t16 to rdx
    mov rdx, qword [rbp-48]

    xor rsi, rsi
    ; [DEBUG] Allocated temp %t17 to r8
    mov r8b, byte [r10+rsi]

    mov byte [rbp-56], r8b

    mov rax, 23

    ; [DEBUG] Temp %t17 freed from r8
    ; [DEBUG] Temp %t16 freed from rdx
    ; [DEBUG] Temp %t15 freed from rcx
main_end:
    add rsp, 208
    pop rbp
    mov rdi, rax
    mov rax, 60
    syscall