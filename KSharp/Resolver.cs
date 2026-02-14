namespace KSharpInterpreter;

class Resolver(Interpreter interpreter) : Expr.IVisitor<object?>, Stmt.IVisitor<object?>
{
    private readonly Interpreter interpreter = interpreter;
    private readonly Stack<Dictionary<string, bool>> scopes = new();
    private SubroutineType currentSubroutine = SubroutineType.NONE;
    private ClassType currentClass = ClassType.NONE;
    private enum SubroutineType
    {
        NONE, SUBROUTINE, CONSTRUCTOR, METHOD, STATIC_METHOD
    }

    private enum ClassType
    {
        NONE, CLASS, SUBCLASS
    }

    public object? VisitBlockStmt(Stmt.Block stmt)
    {
        BeginScope();
        Resolve(stmt.Statements);
        EndScope();
        return null;
    }

    public object? VisitExpressionStmt(Stmt.Expression stmt)
    {
        Resolve(stmt.Expr);
        return null;
    }

    public object? VisitExitStmt(Stmt.Exit stmt)
    {
        Resolve(stmt.ExitCode);
        return null;
    }

    public object? VisitIncStmt(Stmt.Inc stmt)
    {
        ResolveLocal(new Expr.Variable(stmt.VariableName), stmt.VariableName);
        return null;
    }

    public object? VisitDecStmt(Stmt.Dec stmt)
    {
        ResolveLocal(new Expr.Variable(stmt.VariableName), stmt.VariableName);
        return null;
    }

    public object? VisitBreakStmt(Stmt.Break stmt)
    {
        return null;
    }

    public object? VisitContinueStmt(Stmt.Continue stmt)
    {
        return null;
    }

    public object? VisitForStmt(Stmt.For stmt)
    {
        if (stmt.Initializer != null) Resolve(stmt.Initializer);
        if (stmt.Condition != null) Resolve(stmt.Condition);
        if (stmt.Increment != null) Resolve(stmt.Increment);
        Resolve(stmt.Body);
        return null;
    }

    public object? VisitIfStmt(Stmt.If stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.ThenBranch);
        if (stmt.ElseBranch != null) Resolve(stmt.ElseBranch);
        return null;
    }

    public object? VisitPrintStmt(Stmt.Print stmt)
    {
        Resolve(stmt.Expr);
        return null;
    }

    public object? VisitReturnStmt(Stmt.Return stmt)
    {
        if (currentSubroutine == SubroutineType.NONE)
        {
            KSharp.DisplayError(stmt.Keyword, "Can't return from top-level code.");
        }
        if (stmt.Value != null)
        {
            if (currentSubroutine == SubroutineType.CONSTRUCTOR) KSharp.DisplayError(stmt.Keyword, "Can't return a value from a class constructor.");
            Resolve(stmt.Value);
        }
        return null;
    }

    public object? VisitWhileStmt(Stmt.While stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.Body);
        return null;
    }

    

    public object? VisitSubroutineStmt(Stmt.Subroutine stmt)
    {
        Declare(stmt.Name);
        Define(stmt.Name);

        ResolveSubroutine(stmt, SubroutineType.SUBROUTINE);
        return null;
    }

    public object? VisitVarStmt(Stmt.Var stmt)
    {
        Declare(stmt.Name);
        if (stmt.Initializer != null) Resolve(stmt.Initializer);
        Define(stmt.Name);
        return null;
    }

    public object? VisitClassStmt(Stmt.Class stmt)
    {
        ClassType enclosingClass = currentClass;
        currentClass = ClassType.CLASS;

        Declare(stmt.Name);
        Define(stmt.Name);

        if (stmt.Superclass != null && stmt.Name.Lexeme == stmt.Superclass.Name.Lexeme)
        {
            KSharp.DisplayError(stmt.Superclass.Name, "A class cannot inherit from itself.");
        }

        if (stmt.Superclass != null)
        {
            currentClass = ClassType.SUBCLASS;
            Resolve(stmt.Superclass);
            BeginScope();
            scopes.Peek().Add("super", true);
        }

        BeginScope();
        scopes.Peek().Add("this", true);

        foreach (Stmt.Subroutine method in stmt.Methods)
        {
            SubroutineType declaration = SubroutineType.METHOD;
            if (method.Name.Lexeme == "construct") declaration = SubroutineType.CONSTRUCTOR;
            ResolveSubroutine(method, declaration);
        }
        foreach (Stmt.Subroutine staticMethod in stmt.StaticMethods)
        {
            ResolveSubroutine(staticMethod, SubroutineType.STATIC_METHOD);
        }
        foreach (Stmt.Subroutine getMethod in stmt.GetMethods)
        {
            ResolveSubroutine(getMethod, SubroutineType.METHOD);
        }
        foreach (Stmt.Subroutine setMethod in stmt.SetMethods)
        {
            ResolveSubroutine(setMethod, SubroutineType.METHOD);
        }

        EndScope();
        if (stmt.Superclass != null) EndScope();

        currentClass = enclosingClass;
        return null;
    }

    public object? VisitAssignExpr(Expr.Assign expr)
    {
        Resolve(expr.Value);
        ResolveLocal(expr, expr.Name);
        return null;
    }

    public object? VisitVariableExpr(Expr.Variable expr)
    {
        if (scopes.Count != 0)
        {
            Dictionary<string, bool> scope = scopes.Peek();
            if (scope.TryGetValue(expr.Name.Lexeme, out bool value) && value == false)
            {
                KSharp.DisplayError(expr.Name, "Can't read local variable in its own initializer.");
            }
        }

        ResolveLocal(expr, expr.Name);
        return null;
    }

    public object? VisitBinaryExpr(Expr.Binary expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return null;
    }

    public object? VisitCallExpr(Expr.Call expr)
    {
        Resolve(expr.Callee);
        foreach (Expr arg in expr.Args) Resolve(arg);
        return null;
    }

    public object? VisitGroupingExpr(Expr.Grouping expr)
    {
        Resolve(expr.Expression);
        return null;
    }

    public object? VisitLiteralExpr(Expr.Literal expr)
    {
        return null;
    }

    public object? VisitLogicalExpr(Expr.Logical expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
        return null;
    }

    public object? VisitUnaryExpr(Expr.Unary expr)
    {
        Resolve(expr.Right);
        return null;
    }

    public object? VisitIncExpr(Expr.Inc expr)
    {
        ResolveLocal(new Expr.Variable(expr.VariableName), expr.VariableName);
        return null;
    }

    public object? VisitDecExpr(Expr.Dec expr)
    {
        ResolveLocal(new Expr.Variable(expr.VariableName), expr.VariableName);
        return null;
    }

    public object? VisitConditionalExpr(Expr.Conditional expr)
    {
        Resolve(expr.Condition);
        Resolve(expr.Then);
        Resolve(expr.Else);
        return null;
    }

    public object? VisitGetExpr(Expr.Get expr)
    {
        Resolve(expr.Object);
        return null;
    }

    public object? VisitSetExpr(Expr.Set expr)
    {
        Resolve(expr.Value);
        Resolve(expr.Object);
        return null;
    }

    public object? VisitThisExpr(Expr.This expr)
    {
        if (currentClass == ClassType.NONE)
        {
            KSharp.DisplayError(expr.Keyword, "Can't use 'this' outside of a class.");
            return null;
        }
        if (currentSubroutine == SubroutineType.STATIC_METHOD)
        {
            KSharp.DisplayError(expr.Keyword, "Can't use 'this' in a static method.");
            return null;
        }

        ResolveLocal(expr, expr.Keyword);
        return null;
    }

    public object? VisitSuperExpr(Expr.Super expr)
    {
        if (currentClass == ClassType.NONE)
        {
            KSharp.DisplayError(expr.Keyword, "Can't use 'super' outside of a class.");
        }
        else if (currentClass != ClassType.SUBCLASS)
        {
            KSharp.DisplayError(expr.Keyword, "Can't use 'super' in a class with no superclass.");
        }

        ResolveLocal(expr, expr.Keyword);
        return null;
    }

    private void BeginScope()
    {
        scopes.Push([]);
    }

    private void EndScope()
    {
        scopes.Pop();
    }

    private void Declare(Token name)
    {
        if (scopes.Count == 0) return;

        Dictionary<string, bool> scope = scopes.Peek();
        if (scope.ContainsKey(name.Lexeme))
        {
            KSharp.DisplayError(name, "Already a variable with this name in this scope.");
            return;
        }

        scope.Add(name.Lexeme, false);
    }

    private void Define(Token name)
    {
        if (scopes.Count == 0) return;
        scopes.Peek()[name.Lexeme] = true;
    }

    private void ResolveLocal(Expr expr, Token name)
    {
        for (int i = 0; i < scopes.Count; i++)
        {
            if (scopes.ElementAt(i).ContainsKey(name.Lexeme))
            {
                interpreter.Resolve(expr, i);
                return;
            }
        }
    }

    public void Resolve(List<Stmt> statements)
    {
        foreach (Stmt statement in statements) Resolve(statement);
    }

    private void Resolve(Stmt stmt)
    {
        stmt.Accept(this);
    }

    private void Resolve(Expr expr)
    {
        expr.Accept(this);
    }

    private void ResolveSubroutine(Stmt.Subroutine subroutine, SubroutineType type)
    {
        SubroutineType enclosingSubroutine = currentSubroutine;
        currentSubroutine = type;

        BeginScope();
        foreach (Token param in subroutine.Params)
        {
            Declare(param);
            Define(param);
        }
        Resolve(subroutine.Body);
        EndScope();

        currentSubroutine = enclosingSubroutine;
    }
}