# TangoFlex Language & Compiler Design

> **Purpose**: This document defines the goals, semantics, and implementation constraints of the TangoFlex language and compiler. It is intended to keep design decisions coherent as the project scales across multiple architectures (x86, ARM, RISC-V, etc.).

---

## 1. Vision & Non‑Goals

### 1.1 Vision

* Systems programming language with explicit control
* Predictable performance and memory behavior
* Simple, inspectable compilation model
* First‑class support for multiple backends (x86, ARM, RISC-V)

### 1.2 Non‑Goals

* Automatic garbage collection
* Hidden allocations
* JIT compilation (at least initially)
* ABI‑breaking magic or implicit runtime behavior

---

## 2. Design Principles

* **Explicit over implicit**: costs must be visible
* **Stable IR, flexible backend**
* **ABI correctness over cleverness**
* **Compilation should be understandable by reading the code**

---

## 3. Language Syntax & Semantics (Draft)

### 3.1 Function Declarations

Functions are declared using the `func` keyword.

``` tangoflex
func <visibility> <calling_convention> <NAME>(<parameters>) : <TYPE> {
    <scope>
}
```

* Default return type is `void` if omitted.
* If the return type is not `void`, a `return <expr>` statement is mandatory.
* Calling conventions are explicit and part of the function signature.

### Parameters

Parameters are declared as:

``` tangoflex
<NAME> : <TYPE>
```

### 3.2 Variable Declarations

#### **Local Variables**

``` tangoflex
var <visibility> <NAME> : <TYPE> = <EXPR>
var <visibility> <NAME> : <TYPE>
```

* Default type is `int32` if omitted.

#### **Global Variables**

``` tangoflex
var <visibility> <NAME> : <TYPE>
```

* Globals are zero-initialized by default.
* Globals are emitted into the `.bss` section during code generation.

### 3.3 Types

**Built-in Types**: void, bool, int8, uint8, int16, uint16, int32, uint32, int64, uint64

* All numeric types are fixed-width.
* Pointer types are formed by appending `ptr` (e.g., `uint8 ptr`, `void ptr`).

**Strings**: syntactic sugar for `uint8 ptr` with a length field.

### 3.4 Casting

``` tangoflex
@cast(<TYPE>, <EXPR>)
```

* Explicit cast only.

### 3.5 Function Calls

``` tangoflex
call <NAME>(<parameters>)
```

* Parser distinguishes `NodeStmtFuncCall` and `NodeExprFuncCall`.
* Return values are placed in the ABI-defined return register (e.g., `eax/rax` for x86).
* Arguments follow ABI rules.

### 3.6 Binary Operators & Precedence

| Operator Group | Operators   | Precedence |   |    |
| -------------- | ----------- | ---------- | - | -- |
| Multiplicative | `* / %`     | 7          |   |    |
| Additive       | `+ -`       | 6          |   |    |
| Relational     | `< <= > >=` | 5          |   |    |
| Equality       | `== !=`     | 4          |   |    |
| Bitwise AND    | `&`         | 3          |   |    |
| Bitwise XOR    | `^`         | 2          |   |    |
| Bitwise OR     | `           | `          | 1 |    |
| Logical AND    | `&&`        | 0          |   |    |
| Logical OR     | `           |            | ` | -1 |

### 3.7 Compound Operators

``` tangoflex
+= -= *= /= %= &= |= ^= <<= >>=
```

* Lowered into simple assignments during parsing.

### 3.8 Field Access

**Direct:** `a.Field`

**Indirect:** `a->Field` (dereferences pointer automatically)

---

## 4. Memory Model

* Pointers are just addresses in memory.
* Aliasing is not implemented yet.
* Alignment is defined by the ABI (e.g., 16 bytes for x86_64 SysV).

---

## 5. IR Overview

### 5.1 Operand Categories

* `IrOperand`: anything used as an operand (`IrTemp`, `IrLocal`, `IrLabel`, `IrSymbol`, `IrConstStr`, `IrConstFloat`, `IrConstInt`)
* `OperandVar`: `IrTemp` or `IrLocal`
* `OperandValue`: `IrTemp`, `IrLocal`, `IrConstFloat`, `IrConstInt`
* `OperandSymbol`: `IrLabel`, `IrSymbol`, `IrConstStr`

### 5.2 Instruction Structure

* General form: `<result> = <instruction> extra=<operand>, <operands>`
* Comma separates operands.

### 5.3 Instruction Examples

* `ret <IrOperand>`: return with value
* `ret`: return void
* `<OperandVar> = move <IrOperand>`: move value
* `cjump 1/0, <OperandValue>, <Label>`: conditional jump
* `branch <IrOperand>, <then-Label>, <else-Label>`: branch based on operand
* `<OperandVar> = leq <OperandValue>, <OperandValue>`: comparison (same for other binary operators: `add`, `sub`, `mul`, `div`, etc.)
* `<OperandVar> = call <IrSymbol>`: function call
* `<OperandVar> = Load <IrOperand>, <IrOperand>`: load from memory
* `<OperandVar> = addr_of <IrSymbol>`: address-of operation
* `jump <Label>`: unconditional jump

### 5.4 Control Flow

* IR encodes explicit jumps and branches.
* All control-flow constructs are represented as IR nodes, simplifying backend translation.

---

## 6. ABI & Calling Conventions

* ABIs specify register allocation, stack layout, and function call rules.
* Calling conventions determine which registers are caller/callee saved, argument order, and return registers.
* Backend implements the ABI, frontend generates IR according to these rules.

---

## 7. Target Support Policy

* **x86 first-class:** Supported in compiler version 1.
* **ARM / RISC-V:** Planned for compiler version 2.
* **Support definition:** A target is supported if ABI and calling convention rules are implemented correctly, including register usage, stack layout, and parameter passing.

---

## 8. Semantics & Execution Rules

### 8.1 Evaluation Order

* Binary expressions are evaluated **left-to-right**.

### 8.2 Integer Semantics

* Integer operations follow the rules of the target architecture / C-style semantics.
* Overflow is permitted and follows two’s-complement wraparound behavior.

### 8.3 Boolean Semantics

* `bool` is **1 byte** in version 1.
* `true == 1`, `false == 0`.
* In future versions, `bool` may use multiple bits per value (not limited to 1 bit).

### 8.4 Comparison Results

* All comparison instructions yield `1` (true) or `0` (false).

---

## 9. Lifetime Rules

* **Locals**: live until function exit (stack or registers).
* **Temporaries (IrTemp)**: live until last use (stack or registers).
* **Globals**: live for the entire program duration (BSS or registers).

---

## 10. IR Invariants

* Every `IrTemp` is assigned **exactly once** (SSA-style).
* No instruction may appear after a terminator.
* `ret`, `jump`, and `branch` are terminators.
* `ret` must be the final instruction in a basic block.

---

## 11. Basic Blocks

* A basic block is a linear sequence of instructions.
* Labels mark block entry points.
* A basic block is terminated by `ret`, `jump`, or `branch`.

---

## 12. Error Model

### 12.1 Compile-Time Errors

* Compiler aborts on the **first error**.
* Undefined symbols: code generation / assembler / C compiler error.
* Wrong number of arguments: parser or code generation error.

### 12.2 Undefined / Unspecified Behavior

* Type mismatches: undefined behavior.
* Invalid casts: undefined behavior.

### 12.3 Runtime Behavior

* Runtime behavior for invalid operations is currently undefined.

---

## 13. ABI Boundary Rules

* Stack ownership follows ABI rules (e.g., cdecl: caller-owned stack).
* Structs are passed **by pointer**.
* Variadic functions are supported via the `params` keyword.

### 13.1 Variadic Functions

Variadic functions may be declared using the `params` keyword as the final parameter:

``` tangoflex
external func cdecl printf(cstring fmt, params) : int
```

Rules:

* `params` must be the **last** parameter.
* Variadic arguments follow the target ABI’s calling convention rules.
* No type checking is performed for variadic arguments.
* Access to variadic arguments is ABI-specific and handled in the backend.

---

## 14. Explicitly Not Implemented Yet

* Aliasing rules
* Volatile / atomic memory
* Threading model
* Exception handling
* Tail calls
* Vector types

---

## 15. Lowering Pipeline

``` plaintext
Source
 → Preprocessor (#include)
 → Lexer
 → Preprocessor Evaluation (#if, #elif, etc.)
 → Parser (AST)
 → IR Lowering
 → Code Generation (Assembly / C in version 2)
```

---

## 16. Decisions Log

* Future design decisions and rationale will be recorded here.

> If a feature is not described here, it does not exist.
