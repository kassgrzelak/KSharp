namespace KSharpInterpreter;

enum TokenType
{
    // Single character tokens.
    LEFT_PAREN, RIGHT_PAREN, LEFT_BRACE, RIGHT_BRACE,
    COMMA, DOT, MINUS, PLUS, SEMICOLON, SLASH, STAR,

    // One or two character tokens.
    BANG, BANG_EQUAL,
    EQUAL, EQUAL_EQUAL,
    GREATER, GREATER_EQUAL,
    LESSER, LESSER_EQUAL,

    // My additions.
    QUESTION, COLON, CARET, PLUS_EQUAL, MINUS_EQUAL,
    STAR_EQUAL, SLASH_EQUAL, CARET_EQUAL, LESSER_MINUS,
    LEFT_SQUARE, RIGHT_SQUARE,

    // Literals.
    IDENTIFIER, STRING, NUMBER,

    // Keywords.
    AND, CLASS, ELSE, TRUE, FALSE, SUB, FOR, IF, ZILCH,
    OR, RETURN, SUPER, THIS, VAR, WHILE,

    // My additions.
    EXIT, INC, DEC, BREAK, CONTINUE, MOD, DIV, INF,
    STATIC, GET, SET,

    // Extra special!
    EOF, UNK
}