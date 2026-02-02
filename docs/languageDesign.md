# TangoFlex Language & Compiler Design (v1)

> **Purpose:** Defines the syntax, semantics, and constraints of the TangoFlex language and compiler. This serves as the single source of truth for v1 implementation.

---

## 1. Vision & Non‑Goals

### 1.1 Vision

* Systems programming language with explicit control
* Predictable performance and memory behavior
* Simple, inspectable compilation model
* First‑class support for multiple backends (x86, ARM, RISC-V)

### 1.2 Non‑Goals / Never in v1

* Borrow checking
* Safety annotations
* Atomics
* Automatic garbage collection
* Hidden allocations
* JIT compilation

---

## 2. Design Principles

* **Explicit over implicit:** costs must be visible
* **Stable IR, flexible backend**
* **ABI correctness over cleverness**
* Compilation should be understandable by reading the code

---

## 3. Language Syntax & Semantics

### 3.1 Function Declarations

Functions are declared using the `func` keyword.

```tangoflex
func <visibility> <calling_convention> <NAME>(<parameters>) : <TYPE> {
    <scope>
}
```

* Default return type is `void`.
* Non-void functions **must** use `return <expr>`.
* Parameters:

```tangoflex
<NAME> : <TYPE>
```

---

### 3.2 Variables

#### Local

```tangoflex
var <visibility> <NAME> : <TYPE> = <EXPR>
var <visibility> <NAME> : <TYPE>
```

* Default type: `int32`.
* Uninitialized locals default to 0.

#### Global

```tangoflex
var <visibility> <NAME> : <TYPE> = <EXPR>
var <visibility> <NAME> : <TYPE>
```

* Globals default to 0.
* Globals are emitted into the `.bss` section during code generation.

---

### 3.3 Types

* **Built-in:** `void`, `int8`, `uint8`, `int16`, `uint16`, `int32`, `uint32`, `int64`, `uint64`
* **Pointers:** append `ptr` (e.g., `uint8 ptr`)
* **Booleans:** syntactic sugar for `uint8`, `true == 1`, `false == 0`
  * In future versions, `bool` may use multiple bits per value (not limited to 1 bit).
* **Strings:** syntactic sugar for `uint8 ptr` (thin string) or struct with length (fat string, `--fat-strings` flag) more rules about strings [in §3.5](#35-strings)

---

### 3.4 Structs

A **struct** is a user-defined aggregate type consisting of one or more named fields laid out contiguously in memory.

Declaring a struct defines a new type.

```tangoflex
struct Vec2 {
    var x : int32
    var y : int32
}
```

---

#### 3.4.1 Storage and Representation

* **(v1)** Structs always have backing storage in memory.
* A struct is never a pure register value.
* Depending on context, struct storage may reside in:

  * static storage (`.bss` / `.data`)
  * stack storage
  * other storage classes in future versions

A struct value is conceptually treated as:

* a pointer to its backing memory

A struct field is conceptually treated as:

* a pointer to the struct’s backing memory plus a constant offset

---

#### 3.4.2 Registers and Loads

* **(v1, invariant)** Struct values are never loaded into registers.
* Struct fields **may** be loaded into registers.
* All operations on structs operate on memory addresses, not register-resident aggregates.

---

#### 3.4.3 Assignment Semantics

Assigning one struct to another performs a memory copy of the entire struct.

```tangoflex
var a : Vec2
var b : Vec2
b = a
```

Is lowered as:

```tangoflex IR
memcpy &b, &a, sizeof(Vec2)
```

* If the source or destination is a pointer, it must be explicitly dereferenced.
* No implicit address-of or dereference operations are performed by the compiler.

---

#### 3.4.4 Function Parameters

* **(v1)** Struct parameters **must** be declared as pointers.

```tangoflex
func foo(v : Vec2 ptr)   // valid
```

* Declaring a function parameter as a struct value is syntactically valid but **semantically undefined behavior in v1**.

```tangoflex
func foo(v : Vec2)       // undefined behavior in v1
```

* Passing a struct value where a pointer is expected is a compile-time type error.
* Passing a pointer where a struct value is expected is a compile-time type error.
* No implicit address-taking or dereferencing occurs during function calls.

---

#### 3.4.5 Aliasing

* Two distinct struct variables never alias.
* Aliasing is only possible by explicitly taking and using addresses.

```tangoflex
var a : Vec2
var p : Vec2 ptr = @addr_of(a)   // explicit aliasing
```

---

#### 3.4.6 Field Initialization and Partial Assignment

Struct fields may be assigned individually after declaration:

```tangoflex
var v : Vec2
v.x = 10
```

**(v1)** A struct may also be initialized at declaration by explicitly assigning one or more fields in its backing memory. Unspecified fields are zero-initialized.

```tangoflex
var vector : Vec2 at {
    x = 0
    y = 10
}
```

**Explanation:**

* `at { … }` opens a field-assignment context for the memory backing the struct.
* Each field inside is assigned directly; no struct value is created or copied.
* This is primarily intended for fat strings or other single/multi-field structs.
* This syntax may change in future versions.

---

#### 3.4.7 Single-Field Struct Decay

* **(v1)** A struct with exactly one field may implicitly decay to that field in assignment and scalar contexts.

```tangoflex
struct uint24 {
    var value : uint8[3]
}

var x : uint24 = 0xFFFFFF
```

Is equivalent to:

```tangoflex
x.value = 0xFFFFFF
```

This rule applies only to:

* structs with exactly one field
* fat strings ([see §3.5](#35-strings))

It does not apply to multi-field structs.

---

### 3.5 Strings

### 3.5.1 String Kinds

TangoFlex defines **two string representations**:

### Thin string

```tangoflex
string == uint8 ptr
```

* Enabled by default
* No length metadata
* Points to a contiguous sequence of code units
* Conventionally **null-terminated**, but not enforced by the type system
* Zero-cost abstraction
* ABI-compatible with C `char*`

This is the **default string type** in v1.

---

### Fat string

Enabled with compiler flag:

```plaintext
--fat-strings
```

When enabled, `string` refers to the following struct:

```tangoflex
struct string {
    var length : uint16
    var value  : uint8 ptr
}
```

* Explicit length
* No implicit null termination
* ABI-visible layout
* Passed by pointer across ABI boundaries

---

## 3.5.2 String Literals

### Literal forms

```tangoflex
"Hello"        // default
u8"Hello"      // 1 byte per code unit
u16"Hello"     // 2 bytes per code unit
u32"Hello"     // 4 bytes per code unit
```

### Lowering rules

| Literal    | Memory type     | Element type |
| ---------- | --------------- | ------------ |
| `"..."`    | static readonly | `uint8`      |
| `u8"..."`  | static readonly | `uint8`      |
| `u16"..."` | static readonly | `uint16`     |
| `u32"..."` | static readonly | `uint32`     |

* Stored in `.rodata`
* Implicitly null-terminated
* Alignment follows element size
* Lifetime: **entire program**

---

### Literal type

| Mode         | Literal type      |
| ------------ | ----------------- |
| Thin strings | `uint8 ptr`       |
| Fat strings  | `string` (struct) |

---

## 3.5.3 Indexing Semantics

```tangoflex
str[index]
```

### Thin string

* Lowered as pointer indexing
* No bounds checking
* Result type depends on code unit width

| String             | Result   |
| ------------------ | -------- |
| `"..."`, `u8"..."` | `uint8`  |
| `u16"..."`         | `uint16` |
| `u32"..."`         | `uint32` |

---

### Fat string

* Lowered to:

```tangoflex
str.value[index]
```

* No implicit bounds check in v1
* `index >= length` is undefined behavior

---

## 3.5.4 Mutability Rules (v1)

* String literals are **read-only**
* Writing through a string literal pointer is undefined behavior

```tangoflex
var s : string = "hi"
s[0] = 'H'    // ❌ undefined behavior
```

Valid mutable string:

```tangoflex
var buf : uint8 array 16
buf[0] = 'H'
```

---

### 3.6 Arrays

```tangoflex
var <NAME> : <TYPE> array <SIZE>
```

```tangoflex
var arr : uint32 array 10
```

* Zero-indexed
* Bounds checking: None in v1
* Can contain any type (fixed-width numeric, pointer, string, etc.)
* Arrays are pointers by default

---

### 3.7 Const Variables

```tangoflex
var constVar : const uint64 = 982734
constVar = 123  // ❌ not allowed
```

* **Read-only after init**
* Can be modified through pointers with `#ChangingConst` flag
* Default behavior: lowered to compile-time constant in IR
* Const variables can be assigned expressions

---

### 3.8 Compile-Time Evaluation

* Literal expressions and `const` expressions are evaluated at **compile-time**:

```tangoflex
var x : const int32 = 2 + 3  // x lowered to 5 in IR
```

---

### 3.9 Casting

```tangoflex
@cast(<TYPE>, <EXPR>)  // explicit
<TYPE> <EXPR>          // implicit
```

---

### 3.10 Function Calls

```tangoflex
call <NAME>(<parameters>)
```

* Distinguishes statement vs expression calls (`NodeStmtFuncCall` vs `NodeExprFuncCall`)
* Return values go into ABI-defined register (e.g., `eax/rax` for cdecl and sysV)
* Optional `--disable-call` flag for omitting `call` keyword

---

### 3.11 Binary Operators & Precedence

| Precedence | Operators   |   |   |
| ---------- | ----------- | - | - |
| 7          | `* / %`     |   |   |
| 6          | `+ -`       |   |   |
| 5          | `< <= > >=` |   |   |
| 4          | `== !=`     |   |   |
| 3          | `&`         |   |   |
| 2          | `^`         |   |   |
| 1          | `\|`        | ` |   |
| 0          | `&&`        |   |   |
| -1         | `\|\|`      |   | ` |

* Compound operators (`+= -= *= /= %= &= |= ^= <<= >>=`) are lowered to simple assignments

---

### 3.12 Field Access

* Direct: `a.Field`
* Indirect: `a->Field` (dereference)

---

### 3.13 Switch (v1, WIP)

```tangoflex
switch (expr) {
    case 1 {
        break
    }
    case 2, 3 {
        return 5
    }
    default {
        // must have break/return, otherwise auto-inserted
    }
}
```

* Break exits switch
* Default executed if no other case matches

---

### 3.14 Defer (v1, WIP)

```tangoflex
defer {
    <stmt>
}
```

* Deferred blocks run **LIFO** when the function exits
* Useful for cleanup (closing files, releasing resources)

---

### 3.15 External Declarations

```tangoflex
external "<library>" func <calling_convention> <NAME>(<parameters>) : <TYPE>
external var <NAME> : <TYPE>
```

---

### 3.16 If / Else Statements

```tangoflex
if (<EXPR>) {
    <SCOPE>
}
else if (<EXPR>) {
    <SCOPE>
}
else {
    <SCOPE>
}
```

* Non-zero expressions evaluate as **true**.
* Each branch introduces a **new block scope**.
* `else if` and `else` are optional.

---

### 3.17 For Loops

```tangoflex
for (var <NAME> : <TYPE> == <START>..<END>) {
    <SCOPE>
}

for (var <NAME> : <TYPE> == <START>..<END> step <STEP>) {
    <SCOPE>
}

for (<INIT_EXPR> == <END_EXPR>) {
    <SCOPE>
}

for (<INIT_EXPR> == <END_EXPR> step <STEP_EXPR>) {
    <SCOPE>
}
```

* `START` and `END` must be integer types.
* `step` is optional, defaults to 1.
* Upper bound `<END>` is **exclusive**.
* Loop variable can be declared locally or reused from outer scope.

---

### 3.18 While Loops

```tangoflex
while (<EXPR>) {
    <SCOPE>
}
```

* Continues while `<EXPR>` is **non-zero**.
* Introduces a **new block scope**.

---

### 3.19 Break Statement

```tangoflex
break
```

* Exits the **nearest enclosing loop or switch**.
* Can appear inside `for`, `while`, or `switch` blocks.
* Control continues after the enclosing block.

---

### 3.20 System Call

```tangoflex
@systemcall(<INT NUM>, <PARAMS>)
```

* Direct low-level system call interface.
* `<INT NUM>` is the system call number for the target OS/ABI.
* `<PARAMS>` are the arguments passed according to the ABI.

---

### 3.21 Address-of Operator

```tangoflex
@addr_of(<EXPR>)
```

* Alternative to `&<EXPR>`.
* Returns a **pointer** to the variable/expression.
* Example:

```tangoflex
var x : int32 = 42
var px : int32 ptr = @addr_of(x)
```

* Useful for **explicit pointer manipulation** without relying on syntax sugar.

---

### Notes on Control Flow Integration

* `defer` inside loops or conditionals still executes **after function exit**, in **LIFO order**.
* `break` exits the nearest enclosing loop or switch.
* `continue` skips to the next loop iteration.

---

## 4. Memory Model

* Pointers are addresses in memory
* Aliasing is not implemented
* Alignment follows ABI

---

## 5. IR Overview

* `IrOperand`, `OperandVar`, `OperandValue`, `OperandSymbol`
* Instructions: `<result> = <instruction> extra=<operand>, <operands>`
* Terminators: `ret`, `jump`, `branch`

---

## 6. ABI & Calling Conventions

* Defines register allocation, stack layout, function calls
* Backend implements ABI rules
* Frontend emits IR accordingly

---

## 7. Target Support

* **x86:** v1
* **ARM / RISC-V:** planned v2
* Support definition: A target is supported if ABI and calling convention rules are implemented correctly, including register usage, stack layout, and parameter passing.

---

## 8. Semantics

* Binary evaluation: left-to-right
* Integers: wraparound allowed
* Boolean: 1 byte (`true=1`, `false=0`)
* Comparisons: result `0` or `1`

---

## 9. Lifetime

* Locals live until function exit (stack or registers).
* Temporaries: live until last use (stack or registers).
* Globals live for the entire program duration (BSS or registers).

---

## 10. IR Invariants

* Every `IrTemp` is assigned **exactly once** (SSA-style).
* No instruction may appear after a terminator.
* `ret` must be the final instruction in a basic block.

---

## 11. Basic Blocks

* A basic block is a linear sequence of instructions.
* Labels mark block entry points.
* A basic block is terminated by `ret`, `jump`, or `branch`.

---

## 12. Error Model

* Compiler aborts on first error
* Undefined symbols = codegen error
* Type mismatch = undefined behavior

---

## 13. ABI Boundary Rules

* Stack ownership follows ABI rules
* Structs are passed by pointer
* Variadic functions are supported via the `params` keyword.

---

## 14. Lowering Pipeline

``` plaintext
Source
 → Preprocessor (#include)
 → Lexer
 → Preprocessor Evaluation (#if/#elif)
 → Parser (AST)
 → IR Lowering
 → Code Generation (Assembly / C)
```

---

## 15. Decisions Log

* All future changes must be documented here

> If a feature is not described here, it does not exist.
