# Grammar

$$
\begin{align}
    [\text{Prog}] &\to [\text{Stmt}]^* \\
    [\text{Stmt}] &\to
    \begin{cases}
        \text{ident} = \text{[Expr]} \\
        \text{if} ([\text{Expr}])[\text{Scope}]\text{[IfPred]}\\
        \text{while} ([\text{Expr}])[\text{Scope}]\\
        \text{for} (\text{var}\space\text{ident} : [\text{Type}] = [\text{Expr}]..[\text{Expr}])[\text{Scope}]\\
        [\text{Scope}] \\
        [\text{VarDecl}]
    \end{cases} \\
    \text{[Scope]} &\to \{[\text{Stmt}]^*\} \\
    \text{[VarDecl]} &\to
    \begin{cases}
        \text{var}\space\text{ident} : [\text{Type}] = [\text{Expr}] \\
        \text{var}\space\text{ident} : [\text{Type}]
    \end{cases} \\
    \text{[IfPred]} &\to
    \begin{cases}
        \text{else if}(\text{[Expr]})\text{[Scope]}\text{[IfPred]} \\
        \text{else}\text{[Scope]} \\
        \epsilon
    \end{cases} \\
    [\text{Expr}] &\to
    \begin{cases}
        [\text{Term}] \\
        [\text{BinExpr}]
    \end{cases} \\
    [\text{BinExpr}] &\to
    \begin{cases}
        [\text{Expr}] * [\text{Expr}] & \text{prec} = 1 \\
        [\text{Expr}] / [\text{Expr}] & \text{prec} = 1 \\
        [\text{Expr}] + [\text{Expr}] & \text{prec} = 0 \\
        [\text{Expr}] - [\text{Expr}] & \text{prec} = 0 \\
    \end{cases} \\
    [\text{Term}] &\to
    \begin{cases}
        \text{int\_lit} \\
        \text{string\_lit} \\
        \text{ident} \\
        ([\text{Expr}])
    \end{cases} \\
    [\text{Type}] &\to
    \begin{cases}
        \text{uint8} \\
        \text{int8} \\
        \text{uint16} \\
        \text{int16} \\
        \text{uint32} \\
        \text{int32} \\
        \text{uint64} \\
        \text{int64} \\
        \text{string} \\
    \end{cases}
\end{align}
$$
