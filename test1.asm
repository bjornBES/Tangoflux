.text
; func @main(argv : ptr(ptr(uint8)), argc : int32) : int32
main:
    push rbp
    mov rbp, rsp
    sub rsp, 24
; local argv : ptr(ptr(uint8))
; local argc : int32
; local test : int32
; enter function main
    mov r11, 0
    mov rcx, 65535
; entry:
;  store with 2 operands [rbp-12], 0
    mov [rbp-12], [rbp-12]
;    store @test, constI64 0
;    %t0 = load @test
;    ret %t0
    add rsp, 24
    pop rbp
    ret