namespace KSharpInterpreter;

using static TokenType;

class Parser(List<Token> tokens)
{
    private class ParseError : Exception {}

    private readonly List<Token> tokens = tokens;
    private int current = 0;
    private bool inLoop = false;
    private bool inMethod = false;

    public List<Stmt> Parse()
    {
        List<Stmt> statements = [];

        while (!AtEnd())
        {
            Stmt? statement = Declaration();
            if (statement != null) statements.Add(statement);
        }

        return statements;
    }

    private Stmt? Declaration()
    {
        try
        {
            if (MatchNextToken(CLASS)) return ClassDeclaration();
            if (MatchNextToken(SUB)) return Subroutine("subroutine");
            if (MatchNextToken(VAR)) return VarDeclaration();

            return Statement();
        }
        catch (ParseError)
        {
            Synchronize();
            return null;
        }
    }

    private Stmt.Class ClassDeclaration()
    {
        inMethod = true;

        Token name = Consume(IDENTIFIER, "Expect class name.");

        Expr.Variable? superclass = null;
        if (MatchNextToken(LESSER_MINUS))
        {
            Consume(IDENTIFIER, "Expect superclass name.");
            superclass = new Expr.Variable(Previous());
        }

        Consume(LEFT_BRACE, "Expect '{' before class body.");

        List<Stmt.Subroutine> methods = [];
        List<Stmt.Subroutine> staticMethods = [];
        List<Stmt.Subroutine> getMethods = [];
        List<Stmt.Subroutine> setMethods = [];

        while (!CheckFor(RIGHT_BRACE) && !AtEnd())
        {
            if (MatchNextToken(STATIC)) staticMethods.Add(Subroutine("method"));
            else if (MatchNextToken(GET)) getMethods.Add(Subroutine("get method"));
            else if (MatchNextToken(SET)) setMethods.Add(Subroutine("set method"));
            else methods.Add(Subroutine("method"));
        }

        Consume(RIGHT_BRACE, "Expect '}' after class body.");

        inMethod = false;
        return new Stmt.Class(name, superclass, methods, staticMethods, getMethods, setMethods);
    }

    private Stmt.Var VarDeclaration()
    {
        Token name = Consume(IDENTIFIER, "Expect variable name.");
        
        Expr? initializer = null;
        if (MatchNextToken(EQUAL))
        {
            initializer = Expression();
        }

        Consume(SEMICOLON, "Expect ';' after variable declaration.");
        return new Stmt.Var(name, initializer);
    }

    private Stmt Statement()
    {
        if (MatchNextToken(IF)) return IfStatement();
        if (MatchNextToken(WHILE)) return WhileStatement();
        if (MatchNextToken(FOR)) return ForStatement();
        if (MatchNextToken(EXIT)) return ExitStatement();
        if (MatchNextToken(INC)) return IncStatement();
        if (MatchNextToken(DEC)) return DecStatement();
        if (MatchNextToken(RETURN)) return ReturnStatement();
        if (MatchNextToken(BREAK)) return BreakStatement();
        if (MatchNextToken(CONTINUE)) return ContinueStatement();
        if (MatchNextToken(LEFT_BRACE)) return new Stmt.Block(Block());

        return ExpressionStatement();
    }

    private Stmt.If IfStatement()
    {
        Consume(LEFT_PAREN, "Expect '(' after 'if'.");
        Expr condition = Expression();
        Consume(RIGHT_PAREN, "Expect ')' after condition.");

        Stmt thenBranch = Statement();
        Stmt? elseBranch = null;
        if (MatchNextToken(ELSE)) elseBranch = Statement();

        return new Stmt.If(condition, thenBranch, elseBranch);
    }

    private Stmt.While WhileStatement()
    {
        inLoop = true;

        Consume(LEFT_PAREN, "Expect '(' after 'while'.");
        Expr condition = Expression();
        Consume(RIGHT_PAREN, "Expect ')' after condition.");

        Stmt body = Statement();

        inLoop = false;
        return new Stmt.While(condition, body);
    }

    private Stmt.For ForStatement()
    {
        inLoop = true;

        Consume(LEFT_PAREN, "Expect '(' after 'for'.");

        Stmt? initializer;
        if (MatchNextToken(SEMICOLON)) initializer = null;
        else if (MatchNextToken(VAR)) initializer = VarDeclaration();
        else initializer = ExpressionStatement();

        Expr? condition = null;
        if (!CheckFor(SEMICOLON)) condition = Expression();
        Consume(SEMICOLON, "Expect ';' after loop condition.");

        Expr? incrementExpr = null;
        if (!CheckFor(RIGHT_PAREN)) incrementExpr = Expression();
        Consume(RIGHT_PAREN, "Expect ')' after for clauses.");
        Stmt.Expression? increment = null;
        if (incrementExpr != null) increment = new Stmt.Expression(incrementExpr);

        Stmt body = Statement();

        if (condition == null) condition = new Expr.Literal(true);

        inLoop = false;
        return new Stmt.For(initializer, condition, increment, body);
    }

    private Stmt.Exit ExitStatement()
    {
        if (MatchNextToken(SEMICOLON)) return new Stmt.Exit(PreviousPrevious(), new Expr.Literal(0.0));

        Expr exitCode = Expression();
        Consume(SEMICOLON, "Expect ';' after value.");
        return new Stmt.Exit(PreviousPrevious(), exitCode); 
    }

    private Stmt.Inc IncStatement()
    {
        Token variableName = Consume(IDENTIFIER, "Expect variable name after 'inc'.");
        Consume(SEMICOLON, "Expect ';' after variable name.");
        return new Stmt.Inc(variableName);
    }

    private Stmt.Dec DecStatement()
    {
        Token variableName = Consume(IDENTIFIER, "Expect variable name after 'dec'.");
        Consume(SEMICOLON, "Expect ';' after variable name.");
        return new Stmt.Dec(variableName);
    }

    private Stmt.Return ReturnStatement()
    {
        Token keyword = Previous();
        Expr? value = null;
        if (!CheckFor(SEMICOLON)) value = Expression();
        Consume(SEMICOLON, "Expect ';' after return value.");
        return new Stmt.Return(keyword, value);
    }

    private Stmt.Break BreakStatement()
    {
        if (inLoop)
        {
            Consume(SEMICOLON, "Expect ';' after break statement.");
            return new Stmt.Break();
        }
        else throw Error(Previous(), "Break statement must occur inside loop.");
    }

    private Stmt.Continue ContinueStatement()
    {
        if (inLoop)
        {
            Consume(SEMICOLON, "Expect ';' after continue statement.");
            return new Stmt.Continue();
        }
        else throw Error(Previous(), "Continue statement must occur inside loop.");
    }

    private Stmt.Expression ExpressionStatement()
    {
        Expr expr = Expression();
        Consume(SEMICOLON, "Expect ';' after value.");
        return new Stmt.Expression(expr);
    }

    private Stmt.Subroutine Subroutine(string kind)
    {
        Token name = Consume(IDENTIFIER, $"Expect {kind} name.");
        Consume(LEFT_PAREN, $"Expect '(' after {kind} name.");

        List<Token> parameters = [];
        if (!CheckFor(RIGHT_PAREN))
        {
            do
            {
                if (parameters.Count >= 255) Error(Peek(), "More than 255 parameters is not allowed.");
                parameters.Add(Consume(IDENTIFIER, "Expect parameter name."));
            }
            while (MatchNextToken(COMMA));
        }
        Consume(RIGHT_PAREN, "Expect ')' after parameters.");

        if (kind == "get method" && parameters.Count > 0) Error(Peek(), "Get methods cannot take any arguments.");
        if (kind == "set method" && parameters.Count != 1) Error(Peek(), "Set methods must take exactly one argument.");

        Consume(LEFT_BRACE, $"Expect '{{' before {kind} body.");
        List<Stmt> body = Block();
        
        return new Stmt.Subroutine(name, parameters, body);
    }

    private List<Stmt> Block()
    {
        List<Stmt> statements = [];

        while (!CheckFor(RIGHT_BRACE) && !AtEnd())
        {
            Stmt? statement = Declaration();
            if (statement != null) statements.Add(statement);
        }

        Consume(RIGHT_BRACE, "Expect '}' after block.");
        return statements;
    }

    private Expr Expression()
    {
        return Assignment();
    }

    private Expr Assignment()
    {
        Expr expr = Or();

        if (MatchNextToken(EQUAL))
        {
            Token equals = Previous();
            Expr value = Assignment();

            if (expr is Expr.Variable variable) return new Expr.Assign(variable.Name, value);
            else if (expr is Expr.Get get) return new Expr.Set(get.Object, get.Name, value, inMethod);

            Error(equals, "Invalid assignment target.");
        }
        else if (MatchNextToken(PLUS_EQUAL, MINUS_EQUAL, STAR_EQUAL, SLASH_EQUAL, CARET_EQUAL))
        {
            Token equals = Previous();
            TokenType @operator = PLUS;
            string lexeme = "+";
            if (equals.Type == MINUS_EQUAL) { @operator = MINUS; lexeme = "-"; }
            if (equals.Type == STAR_EQUAL) { @operator = STAR; lexeme = "*"; }
            if (equals.Type == SLASH_EQUAL) { @operator = SLASH; lexeme = "/"; }
            if (equals.Type == CARET_EQUAL) { @operator = CARET; lexeme = "^"; }
            Expr value = Assignment();

            if (expr is Expr.Variable variable)
            {
                Token name = variable.Name;
                return new Expr.Assign(name, new Expr.Binary(variable, new Token(@operator, lexeme, null, equals.Line), value));
            }

            Error(equals, "Invalid assignment target.");
        }

        return expr;
    }

    private Expr Or()
    {
        Expr expr = And();

        while (MatchNextToken(OR))
        {
            Token @operator = Previous();
            Expr right = And();
            expr = new Expr.Logical(expr, @operator, right);
        }

        return expr;
    }

    private Expr And()
    {
        Expr expr = Conditional();

        while (MatchNextToken(AND))
        {
            Token @operator = Previous();
            Expr right = Conditional();
            expr = new Expr.Logical(expr, @operator, right);
        }

        return expr;
    }

    private Expr Conditional()
    {
        Expr expr = Equality();

        if (MatchNextToken(QUESTION))
        {
            Expr condition = expr;
            Expr a = Expression();
            Consume(COLON, "Expect expression.");
            Expr b = Expression();
            expr = new Expr.Conditional(condition, a, b);
        }

        return expr;
    }

    private Expr Equality()
    {
        Expr expr = Comparison();

        while (MatchNextToken(BANG_EQUAL, EQUAL_EQUAL))
        {
            Token @operator = Previous();
            Expr right = Comparison();
            expr = new Expr.Binary(expr, @operator, right);
        }

        return expr;
    }

    private Expr Comparison()
    {
        Expr expr = Term();

        while (MatchNextToken(GREATER, GREATER_EQUAL, LESSER, LESSER_EQUAL))
        {
            Token @operator = Previous();
            Expr right = Term();
            expr = new Expr.Binary(expr, @operator, right);
        }

        return expr;
    }

    private Expr Term()
    {
        Expr expr = Factor();

        while (MatchNextToken(PLUS, MINUS))
        {
            Token @operator = Previous();
            Expr right = Factor();
            expr = new Expr.Binary(expr, @operator, right);
        }

        return expr;
    }

    private Expr Factor()
    {
        Expr expr = Exponent();

        while (MatchNextToken(SLASH, STAR, MOD, DIV))
        {
            Token @operator = Previous();
            Expr right = Exponent();
            expr = new Expr.Binary(expr, @operator, right);
        }

        return expr;
    }

    private Expr Exponent()
    {
        Expr expr = Unary();

        while (MatchNextToken(CARET))
        {
            Token @operator = Previous();
            Expr right = Unary();
            expr = new Expr.Binary(expr, @operator, right);
        }

        return expr;
    }

    private Expr Unary()
    {
        if (MatchNextToken(BANG, MINUS))
        {
            Token @operator = Previous();
            Expr right = Unary();
            return new Expr.Unary(@operator, right);
        }

        return Call();
    }

    private Expr Call()
    {
        Expr expr = IncDecExpr();

        while (true)
        {
            if (MatchNextToken(LEFT_PAREN)) expr = FinishCall(expr);
            else if (MatchNextToken(DOT))
            {
                Token name = Consume(IDENTIFIER, "Expect property name after '.'.");
                expr = new Expr.Get(expr, name, inMethod);
            }
            else break;
        }

        return expr;
    }

    private Expr.Call FinishCall(Expr callee)
    {
        List<Expr> args = [];

        if (!CheckFor(RIGHT_PAREN))
        {
            do
            {
                if (args.Count >= 255) Error(Peek(), "More than 255 arguments is not allowed.");
                args.Add(Expression());
            }
            while (MatchNextToken(COMMA));
        }

        Token paren = Consume(RIGHT_PAREN, "Expect ')' after arguments.");

        return new Expr.Call(callee, paren, args);
    }

    private Expr IncDecExpr()
    {
        if (MatchNextToken(INC, DEC))
        {
            Token @operator = Previous();
            Expr variable = Primary();
            if (variable is Expr.Variable variableName)
            {
                if (@operator.Type == INC) return new Expr.Inc(variableName.Name);
                return new Expr.Dec(variableName.Name);
            }
            else Error(Previous(), "Expect Variable after increment/decrement instruction.");
        }

        return Primary();
    }

    private Expr Primary()
    {
        if (MatchNextToken(FALSE)) return new Expr.Literal(false);
        if (MatchNextToken(TRUE)) return new Expr.Literal(true);
        if (MatchNextToken(ZILCH)) return new Expr.Literal(null);
        if (MatchNextToken(INF)) return new Expr.Literal(double.PositiveInfinity);

        if (MatchNextToken(NUMBER, STRING)) return new Expr.Literal(Previous().Literal);

        if (MatchNextToken(LEFT_SQUARE)) return MakeArray();

        if (MatchNextToken(SUPER))
        {
            Token keyword = Previous();
            Consume(DOT, "Expect '.' after 'super'.");
            Token method = Consume(IDENTIFIER, "Expect superclass method name.");
            return new Expr.Super(keyword, method);
        }

        if (MatchNextToken(THIS)) return new Expr.This(Previous());

        if (MatchNextToken(IDENTIFIER)) return new Expr.Variable(Previous());

        if (MatchNextToken(LEFT_PAREN))
        {
            Expr expr = Expression();
            Consume(RIGHT_PAREN, "Expect ')' after expression.");
            return new Expr.Grouping(expr);
        }

        throw Error(Peek(), "Expect expression.");
    }

    private bool MatchNextToken(params TokenType[] types)
    {
        foreach (TokenType type in types)
        {
            if (CheckFor(type))
            {
                Advance();
                return true;
            }
        }

        return false;
    }

    private Expr.Literal MakeArray()
    {
        List<Expr> items = [];

        if (!CheckFor(RIGHT_PAREN))
        {
            do
            {
                if (items.Count >= 255) Error(Peek(), "More than 255 items is not allowed.");
                items.Add(Or());
            }
            while (MatchNextToken(COMMA));
        }

        Consume(RIGHT_SQUARE, "Expect ']' after array items.");

        return new Expr.Literal(items);
    }

    /// <summary>
    /// If token at current index is of given type, return that token. Otherwise, throw an error with the given message. 
    /// </summary>
    private Token Consume(TokenType type, string message)
    {
        if (CheckFor(type)) return Advance();

        throw Error(Peek(), message);
    }

    /// <summary>
    /// Checks if token at current index is of given type.
    /// </summary>
    private bool CheckFor(TokenType type)
    {
        if (AtEnd()) return false;
        return Peek().Type == type;
    }

    private Token Advance()
    {
        if (!AtEnd()) current++;
        return Previous();
    }

    private bool AtEnd()
    {
        return Peek().Type == EOF;
    }

    /// <summary>
    /// Return token at current index without advancing.
    /// </summary>
    private Token Peek()
    {
        return tokens[current];
    }

    private Token Previous()
    {
        return tokens[current - 1];
    }

    private Token PreviousPrevious()
    {
        return tokens[current - 2];
    }

    private static ParseError Error(Token token, string message)
    {
        KSharp.DisplayError(token, message);
        return new ParseError();
    }

    private void Synchronize()
    {
        Advance();

        while (!AtEnd())
        {
            if (Previous().Type == SEMICOLON) return;

            switch (Peek().Type)
            {
                case CLASS:
                case SUB:
                case VAR:
                case FOR:
                case IF:
                case WHILE:
                case RETURN:
                    return;
            }

            Advance();
        }
    }
}