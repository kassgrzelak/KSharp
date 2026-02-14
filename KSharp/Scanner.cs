using System.Globalization;
using System.Text.RegularExpressions;
using static KSharpInterpreter.TokenType;

namespace KSharpInterpreter;

partial class Scanner(string source)
{
    private readonly string source = source;
    private readonly List<Token> tokens = [];
    // Start index of part of source of interest.
    private int start = 0;
    // Current index of part of source of interest, once the end is reached (e.g., the end of an identifier name), this is the end index of that part.
    private int current = 0;
    private int lineNum = 1;
    // Dictionary containing names of all keeywords in K#.
    private static readonly Dictionary<string, TokenType> keywords = new()
    {
        { "and", AND },
        { "class", CLASS },
        { "else", ELSE },
        { "false", FALSE },
        { "true", TRUE },
        { "for", FOR },
        { "sub", SUB },
        { "if", IF },
        { "zilch", ZILCH },
        { "or", OR },
        { "return", RETURN },
        { "super", SUPER },
        { "this", THIS },
        { "var", VAR },
        { "while", WHILE },
        { "exit", EXIT },
        { "inc", INC },
        { "dec", DEC },
        { "break", BREAK },
        { "continue", CONTINUE },
        { "mod", MOD },
        { "div", DIV },
        { "inf", INF},
        { "static", STATIC },
        { "get", GET },
        { "set", SET }
    };

    /// <summary>
    /// Scan all tokens in source and return a list of them all.
    /// </summary>
    public List<Token> ScanTokens()
    {
        while (!AtEnd())
        {
            // Set start of current token.
            start = current;
            ScanToken();
        }
        
        // Add in the extra special EOF token.
        tokens.Add(new Token(EOF, "", null, lineNum));
        return tokens;
    }

    /// <summary>
    /// Scan the next token in the source code and add it to the list of tokens.
    /// </summary>
    private void ScanToken()
    {
        char c = Advance();
        switch (c)
        {
            case '(': AddToken(LEFT_PAREN); break;
            case ')': AddToken(RIGHT_PAREN); break;
            case '{': AddToken(LEFT_BRACE); break;
            case '}': AddToken(RIGHT_BRACE); break;
            case '[': AddToken(LEFT_SQUARE); break;
            case ']': AddToken(RIGHT_SQUARE); break;
            case ',': AddToken(COMMA); break;
            case '.': AddToken(DOT); break;
            case ';': AddToken(SEMICOLON); break;
            case '?': AddToken(QUESTION); break;
            case ':': AddToken(COLON); break;

            case '+':
                AddToken(MatchNextChar('=') ? PLUS_EQUAL : PLUS);
                break;
            case '-':
                AddToken(MatchNextChar('=') ? MINUS_EQUAL : MINUS);
                break;
            case '*':
                AddToken(MatchNextChar('=') ? STAR_EQUAL : STAR);
                break;
            case '^':
                AddToken(MatchNextChar('=') ? CARET_EQUAL : CARET);
                break;
            case '!':
                AddToken(MatchNextChar('=') ? BANG_EQUAL : BANG);
                break;
            case '=':
                AddToken(MatchNextChar('=') ? EQUAL_EQUAL : EQUAL);
                break;
            case '<':
                AddToken(MatchNextChar('=') ? LESSER_EQUAL : (MatchNextChar('-') ? LESSER_MINUS : LESSER));
                break;
            case '>':
                AddToken(MatchNextChar('=') ? GREATER_EQUAL : GREATER);
                break;
            case '/':
                if (MatchNextChar('/'))
                {
                    // a comment goes until the end of the line
                    while (Peek() != '\n' && !AtEnd()) Advance();
                }
                else if (MatchNextChar('=')) AddToken(SLASH_EQUAL); 
                else AddToken(SLASH);
                break;
            // case '$':
            //     if (MatchNextChar('"')) CreateFormattedString('"');
            //     else if (MatchNextChar('\'')) CreateFormattedString('\'');
            //     break;
            
            case ' ':
            case '\r':
            case '\t':
                // ignore whitespace
                break;

            case '\n':
                lineNum++;
                break;

            case '"': CreateString('"'); break;
            case '\'': CreateString('\''); break;

            default:
                if (IsDigit(c))
                {
                    CreateNumber();
                }
                else if (IsAlpha(c))
                {
                    CreateIdentifier();
                }
                else
                {
                    KSharp.DisplayError(lineNum, "Unexpected character");
                }
                break;
        }
    }

    /// <summary>
    /// Create an identifier starting from the current index and add a token for it. If the name of this identifier matches a keyword,
    /// that keyword's token will be added instead.
    /// </summary>
    private void CreateIdentifier()
    {
        // Keep adding characters until the characters are not alphanumeric.
        while (IsAlphaNumeric(Peek())) Advance();

        string text = source[start..current];
        // Try to write keywords[identifier] to variable "type". If this fails, this method will return false.
        if (!keywords.TryGetValue(text, out TokenType type))
        {
            type = IDENTIFIER;
        }
        AddToken(type);
    }

    /// <summary>
    /// Create a number starting from the current index and add a token for it.
    /// </summary>
    private void CreateNumber()
    {
        NumBase numBase = NumBase.DECIMAL;

        if (Previous() == '0' && Peek() == 'b') { Advance(); numBase = NumBase.BINARY; }
        else if (Previous() == '0' && Peek() == 'x') { Advance(); numBase = NumBase.HEXADECIMAL; }

        if ((numBase == NumBase.BINARY || numBase == NumBase.HEXADECIMAL) && !IsDigitInBase(Peek(), numBase))
        {
            KSharp.DisplayError(lineNum, "invalid character following base literal.");
            return;
        }
        while (IsDigitInBase(Peek(), numBase)) Advance();

        // look for a fractional part if in decimal
        if (Peek() == '.' && IsDigit(PeekNext()) && numBase == NumBase.DECIMAL)
        {
            // consume the '.'
            Advance();

            while (IsDigit(Peek())) Advance();
        }

        if (numBase == NumBase.DECIMAL) AddToken(NUMBER, double.Parse(source[start..current]));
        else if (numBase == NumBase.BINARY) AddToken(NUMBER, (double)Convert.ToUInt32(source[(start+2)..current], 2));
        else if (numBase == NumBase.HEXADECIMAL) AddToken(NUMBER, (double)Convert.ToUInt32(source[(start+2)..current], 16));
    }

    /// <summary>
    /// Create a number starting from the current index and add a token for it.
    /// </summary>
    private void CreateString(char quoteChar)
    {
        while (Peek() != quoteChar && !AtEnd())
        {
            // Allow multiline strings. Cool.
            if (Peek() == '\n') lineNum++;
            Advance();
        }

        // If we've reached the end of the source code, the string must be unterminated.
        if (AtEnd())
        {
            KSharp.DisplayError(lineNum, "Unterminated string");
            return;
        }

        // Advance past the closing "
        Advance();

        // Exclude the quotes from the string literal.
        string value = source[(start + 1)..(current - 1)];
        AddToken(STRING, value);
    }

    // private void CreateFormattedString(char quoteChar)
    // {
    //     while (!AtEnd())
    //     {
    //         if (Peek() == '\n') lineNum++; 
    //         if (Peek() == '{' && PeekNext() != '}')
    //         {
    //             h
    //         }
    //     }
    // }

    /// <summary>
    /// Return true if the next character in the source code matches a given character. False otherwise.
    /// </summary>
    private bool MatchNextChar(char expected)
    {
        if (AtEnd()) return false;
        if (source[current] != expected) return false;

        current++;
        return true;
    }

    /// <summary>
    /// Return next character in source code. Returns null character if at EOF.
    /// </summary>
    private char Peek()
    {
        if (AtEnd()) return '\0';
        return source[current];
    }

    /// <summary>
    /// Return next next character in source code. Returns null character if at EOF.
    /// </summary>
    private char PeekNext()
    {
        if (current + 1 >= source.Length) return '\0';
        return source[current + 1];
    }

    /// <summary>
    /// Return true if given character is a letter or underscore. Return false otherwise.
    /// </summary>
    private bool IsAlpha(char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
    }

    /// <summary>
    /// Return true if given character is a digit. Return false otherwise.
    /// </summary>
    private bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    private bool IsDigitInBase(char c, NumBase numBase)
    {
        return numBase switch
        {
            NumBase.BINARY => c == '0' || c == '1',
            NumBase.DECIMAL => IsDigit(c),
            NumBase.HEXADECIMAL => IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'),
            _ => throw new ArgumentException("IsDigitInBase takes one of BINARY, DECIMAL, or HEXADECIMAL."),
        };
    }

    /// <summary>
    /// Return true if given character is alphanumeric. Return false otherwise.
    /// </summary>
    private bool IsAlphaNumeric(char c)
    {
        return IsAlpha(c) || IsDigit(c);
    }

    /// <summary>
    /// Return true if next char is out of range. Return false otherwise.
    /// </summary>
    private bool AtEnd()
    {
        return current >= source.Length;
    }

    /// <summary>
    /// Return current character and incrementindex counter.
    /// </summary>
    private char Advance()
    {
        return source[current++];
    }

    private char Previous()
    {
        return source[current - 1];
    }

    /// <summary>
    /// Add token to token list.
    /// </summary>
    private void AddToken(TokenType type)
    {
        AddToken(type, null);
    }

    /// <summary>
    /// Add token to token list + optional literal value.
    /// </summary>
    private void AddToken(TokenType type, object? literal)
    {
        string text = source[start..current];
        tokens.Add(new Token(type, text, literal, lineNum));
    }

    [GeneratedRegex("[1234567890]")]
    private static partial Regex MyRegex();
}