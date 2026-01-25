#namespace CompilerTangoFlex.lexer

enum public TokenKind : uint8
{
    None,
    Float,
    Int,
    Char,
    String,
    Identifier,
    Keyword,
    Operator,
    Newline,
}

struct public Token {
    var public kind : TokenKind
    var public start : int
    var public length : int
    var public line : int
    var public column : int
}

struct TokenList {
    var data : Token ptr
    var count : uint32
    var capacity : uint32
    var arena : Arena ptr
}

var charIndex : int
var source : string ptr // NULL by defualt
var keywordList : cstring ptr // NULL by defualt
var keywordCount : int

func token_list_init(list : TokenList ptr, arena : Arena ptr, cap : uint32) : void {
    list->arena    = arena
    list->count    = 0
    list->capacity = cap
    list->data = @cast(Token ptr,
        call arena_alloc(arena, cap * sizeof(Token), @alignof(Token))
    )
}


func public TangoFlexLexer(var src : string, void ptr arguments, arena : Arena ptr, out_tokens : TokenList ptr) : void
{
    // in the data section in asm get a label called keywords
    keywordList = @data(keywords)
    keywordCount = @data(keywordCount)

    source = &src
    charIndex = 0

    // init lexer arena
    call arena_init(&arena, 1024 * 1024) // 1 MB for lexer

    // make an estimate
    var estimated : int = src.length / 3 // very rough
    if (estimated < 64)
    {
        estimated = 64
    }

    // init list
    var list : TokenList
    call token_list_init(&list, &arena, estimated)
    // WIP 2000 tokens maybe more, maybe less.

    // call lexer
    call Lex(&list)

    out_tokens = &list
}

func internal Lex(list : TokenList ptr) : void
{
    // line and column dose not start at 0
    var line : int = 1
    var column : int = 1

    while (call peek(0) != 0)
    {
        var c = call peek(0)

        // ---------- whitespace ----------
        if (c == ' ' || c == '\t' || c == '\r') {
            call advance()
            column += 1
            continue
        }

        // ---------- newline ----------
        if (c == '\n') {
            var t : Token
            t.kind = TokenKind.Newline
            t.start = charIndex
            t.length = 1
            t.line = line
            t.column = column

            call token_list_push(list, t)

            call advance()
            line += 1
            column = 1
            continue
        }

        if (call is_alpha(c))
        {
            // Identifier / Keyword
            var start = charIndex;
            var col = column;

            while (call is_alnum(call peek(0))) {
                call advance()
                column += 1;
            }

            var t : Token;
            t.start = start;
            t.length = charIndex - start;
            t.line = line;
            t.column = col;

            if (is_keyword(start, t.length)) {
                t.kind = TokenKind.Keyword;
            } else {
                t.kind = TokenKind.Identifier;
            }

            call token_list_push(list, t);
            continue;
        }

        // ---------- number ----------
        if (call is_digit(c)) {
            var start = charIndex;
            var col = column;
            var is_float : bool = false;

            while (call is_digit(peek(0))) {
                call advance()
                column += 1;
            }

            if (i < src.length && call peek(0) == '.') {
                is_float = true;
                call advance()
                column += 1;

                while (call is_digit(peek(0))) {
                    call advance()
                    column += 1;
                }
            }

            var t : Token;
            t.kind = is_float ? TokenKind.Float : TokenKind.Int;
            t.start = start;
            t.length = charIndex - start;
            t.line = line;
            t.column = col;

            call token_list_push(list, t);
            continue;
        }

        // ---------- string ----------
        if (c == '"') {
            var start = charIndex;
            var col = column;

            charIndex += 1; column += 1;

            while (i < src.length && call peek(0) != '"') {
                if (src[i] == '\n') {
                    line += 1;
                    column = 1;
                } else {
                    column += 1;
                }
                call advance()
            }

            // consume closing quote
            if (i < src.length) {
                i += 1;
                column += 1;
            }

            var t : Token;
            t.kind   = TokenKind.String;
            t.start  = start;
            t.length = i - start;
            t.line   = line;
            t.column = col;

            call token_list_push(list, t);
            continue;
        }

        // ---------- operator / punctuation ----------
        {
            var t : Token;
            t.kind = TokenKind.Operator;
            t.start = i;
            t.length = 1;
            t.line = line;
            t.column = column;

            call token_list_push(list, t);

            call advance()
            column += 1;
            continue;
        }
    }
}

func peek(offset : int) : uint8
{
    if (offset + charIndex >= source.length)
    {
        return 0
    }
    return source[offset + charIndex]
}

func advance() : uint8
{
    charIndex++;
    return source[charIndex - 1]
}

func internal token_list_push(list : TokenList ptr, t : Token) : void {
    if (list->count == list->capacity) {
        // grow by allocating a NEW block
        var new_cap = list->capacity * 2
        var new_data = @cast(
            Token ptr,
            call arena_alloc(
                list->arena,
                new_cap * sizeof(Token),
                @alignof(Token)
            )
        )

        call memcpy(new_data, list->data, list->count * sizeof(Token))

        list->data = new_data
        list->capacity = new_cap
    }

    list->data[list->count++] = t
}

// helper functions
func internal is_keyword(start : int, length : int) : bool
{
    var identifier : string = *@cast(string ptr, source + start)
    identifier.length = length
    
    for (var i : int = 0 .. keywordCount step 1)
    {
        var keyword : cstring = keywordList[i]
        if (call strcmp(keyword, identifier) == true)
        {
            return true
        }
    }
    return false
}

func internal is_alpha(c:uint8):bool
{
    return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_'
}

func internal is_digit(c:uint8):bool {
    return (c >= '0' && c <= '9')
}

func internal is_alnum(c:uint8):bool {
    return call is_alpha(c) || call is_digit(c)
}