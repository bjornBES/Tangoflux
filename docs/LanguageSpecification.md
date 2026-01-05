# **TangoFlex Language Specification**

VERSION: Draft v0.1

## 1. Design Goals

TangoFlex is designed to be:

* **Minimal**
* **Explicit**
* **Easy to parse**
* **Easy to lower into IR**
* **Systems-level**
* **Non-C-centric but familiar**

There is **no hidden behavior**. All semantics must be visible in the source.

---

## 2. Program Structure

A TangoFlex program consists of:

* Function declarations
* Global variable declarations (optional)
* No implicit includes (preprocessor planned)

### Example

```tangoflex
func main(argv : string ptr, argc : int32) : int {
    return 0
}
```

---

## 3. Keywords

### Core keywords

``` tangoflex
func
var
if
else
while
for
return
```

### Type-related

```tangoflex
ptr
```

(others like `struct`, `enum`, `const` may be added later)

---

## 4. Types

### Built-in scalar types

| Type   | Description             |
| ------ | ----------------------- |
| int    | Native signed integer   |
| int8   | 8-bit signed integer    |
| int16  | 16-bit signed integer   |
| int32  | 32-bit signed integer   |
| int64  | 64-bit signed integer   |
| uint8  | 8-bit unsigned integer  |
| uint16 | 16-bit unsigned integer |
| uint32 | 32-bit unsigned integer |
| uint64 | 64-bit unsigned integer |

> `int` and `int32` may be aliases depending on target.

---

### Pointer types

Pointers are expressed using the `ptr` keyword:

```tangoflex
uint8 ptr
string ptr
int ptr
```

This avoids `*` and simplifies parsing.

---

### String type

```tangoflex
string
```

* Represents a pointer to character data
* Implementation-defined encoding (ASCII/UTF-8)
* `string ptr` represents pointer-to-string

---

## 5. Variable Declaration

### Syntax

```tangoflex
var <name> : <type> = <expression>
```

### Examples

```tangoflex
var i : int = 0
var framebuffer : uint8 ptr = (uint8 ptr)0xb8000
```

### Rules

* Variables must be initialized
* Type inference is **not supported**
* Variables are mutable by default

---

## 6. Functions

### Syntax

```tangoflex
func <name>(<params>) : <return_type> {
    <statements>
}
```

### Parameters

```tangoflex
name : type
```

### Example

```tangoflex
func fib(n : int) : int {
    if (n <= 1) {
        return n
    }
    return fib(n - 1) + fib(n - 2)
}
```

---

## 7. Statements

### 7.1 Block

```tangoflex
{
    <statements>
}
```

Creates a new scope.

---

### 7.2 If statement

```tangoflex
if (<expression>) {
    <statements>
} else {
    <statements>
}
```

* Parentheses are **required**
* `else` is optional

---

### 7.3 While loop

```tangoflex
while (<expression>) {
    <statements>
}
```

---

### 7.4 For loop

#### Syntax

```tangoflex
for (var <name> : <type> = <start> .. <end> [step <expr>]) {
    <statements>
}
```

#### Example

```tangoflex
for (var i : int = 0 .. 1000) {
    framebuffer[i] = 0
}
```

#### Semantics

* Range is **inclusive**
* Default step is `1`
* Step may be negative
* Step = 0 is a compile-time error
* Loop variable is scoped to the loop body

#### Lowering rule

The `for` loop is **syntactic sugar** and must be lowered to:

```tangoflex
var i : int = start
while (i <= end) {
    body
    i = i + step
}
```

before IR generation.

---

### 7.5 Return

```tangoflex
return <expression>
```

Functions returning `void` (future) may omit expression.

---

## 8. Expressions

### Supported operators

#### Arithmetic

``` plaintext
+  -  *  /  %
```

#### Comparison

``` plaintext
==  !=  <  <=  >  >=
```

#### Assignment

``` plaintext
=
```

---

### Indexing

```tangoflex
expr[expr]
```

Semantics:

```text
*(base + index)
```

Scaling is based on the pointed-to type.

---

### Function calls

```tangoflex
fib(10)
print("hello")
```

---

## 9. Literals

### Integer literals

```tangoflex
123
0xff
0b1010
```

### String literals

```tangoflex
"hello world"
```

---

## 10. Casting

Explicit casts only.

### Syntax

```tangoflex
(type) expression
```

### Example

```tangoflex
var framebuffer : uint8 ptr = (uint8 ptr)0xb8000
```

No implicit pointer/integer casts are allowed.

---

## 11. Scope Rules

* Variables are block-scoped
* Function parameters are scoped to function body
* Loop variables are scoped to the loop block

---

## 12. Undefined / Implementation-Defined Behavior (for now)

* Integer overflow
* Pointer arithmetic beyond valid memory
* String encoding
* `int` size if not explicitly specified

---

## 13. Example Program (Complete)

```tangoflex
func main(argv : string ptr, argc : int32) : int {
    var framebuffer : uint8 ptr = (uint8 ptr)0xb8000

    for (var i : int = 0 .. 3999) {
        framebuffer[i] = 0
    }

    return 0
}
