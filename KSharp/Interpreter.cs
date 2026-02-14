namespace KSharpInterpreter;

using static TokenType;

#pragma warning disable CS8605, CS8602, CS8600, CS8603 // Various NON-issues that the type checker still does not like

class Interpreter : Expr.IVisitor<object?>, Stmt.IVisitor<object>
{
    private class Break : Exception {}
    private class Continue : Exception {}
    public readonly KSharpEnvironment globals;
    public KSharpEnvironment environment;
    private readonly Dictionary<Expr, int> locals = [];

    public Interpreter()
    {
        globals = new();
        globals.Define("clock", new Clock());
        globals.Define("snooze", new Snooze());
        globals.Define("print", new Print());
        globals.Define("input", new Input());
        globals.Define("round", new Round());
        globals.Define("string", new String());
        globals.Define("sqrt", new Sqrt());

        environment = new(globals);
    }

    public void Interpret(List<Stmt> statements)
    {
        try
        {
            foreach (Stmt statement in statements)
            {
                Execute(statement);
            }
        }
        catch (RuntimeError error)
        {
            KSharp.RuntimeError(error);
        }
    }

    public object? VisitBinaryExpr(Expr.Binary expr)
    {
        object? left = Evaluate(expr.Left);
        object? right = Evaluate(expr.Right);

        switch (expr.Operator.Type)
        {
            case MINUS:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left - (double)right;
            case SLASH:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left / (double)right;
            case STAR:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left * (double)right;
            case PLUS:
                if (left is double a && right is double b) return a + b;
                if (left is string c && right is string d) return c + d;
                throw new RuntimeError(expr.Operator, "Operands must be two numbers or two strings.");
            case GREATER:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left > (double)right;
            case GREATER_EQUAL:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left >= (double)right;
            case LESSER:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left < (double)right;
            case LESSER_EQUAL:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left <= (double)right;
            case EQUAL_EQUAL: return AreEqual(left, right);
            case BANG_EQUAL: return !AreEqual(left, right);
            case MOD:
                CheckNumberOperands(expr.Operator, left, right);
                return (double)left % (double)right;
            case DIV:
                CheckNumberOperands(expr.Operator, left, right);
                return Math.Floor((double)left / (double)right);
            case CARET:
                CheckNumberOperands(expr.Operator, left, right);
                return Math.Pow((double)left, (double)right);
        }

        // Unreachable.
        return null;
    }

    public object? VisitLiteralExpr(Expr.Literal expr)
    {
        if (expr.Value is List<Expr> list)
        {
            List<object?> values = [];
            foreach (Expr expression in list) values.Add(Evaluate(expression));
            return values;
        }
        return expr.Value;
    }

    public object? VisitGroupingExpr(Expr.Grouping expr)
    {
        return Evaluate(expr.Expression);
    }

    public object? VisitUnaryExpr(Expr.Unary expr)
    {
        object? right = Evaluate(expr.Right);

        switch (expr.Operator.Type)
        {
            case MINUS:
                CheckNumberOperand(expr.Operator, right);
                return -(double)right;
            case BANG:
                return !IsTruthy(right);
        }

        // Unreachable.
        return null;
    }

    public object? VisitConditionalExpr(Expr.Conditional expr)
    {
        object? condition = Evaluate(expr.Condition);

        if (IsTruthy(condition)) return Evaluate(expr.Then);
        return Evaluate(expr.Else);
    }

    public object? VisitVariableExpr(Expr.Variable expr)
    {
        return LookUpVariable(expr.Name, expr);
    }

    private object? LookUpVariable(Token name, Expr expr)
    {
        if (locals.TryGetValue(expr, out int distance)) return environment.GetAt(distance, name.Lexeme);
        else return environment.Get(name);
    }

    public object VisitAssignExpr(Expr.Assign expr)
    {
        object value = Evaluate(expr.Value);
        
        int? distance;
        if (locals.TryGetValue(expr, out int dist)) distance = dist;
        else distance = null;

        if (distance != null) environment.AssignAt((int)distance, expr.Name, value);
        else globals.Assign(expr.Name, value);

        return value;
    }

    public object VisitLogicalExpr(Expr.Logical expr)
    {
        object left = Evaluate(expr.Left);

        if (expr.Operator.Type == OR)
        {
            if (IsTruthy(left)) return left;
        }
        else
        {
            if (!IsTruthy(left)) return left;
        }

        return Evaluate(expr.Right);
    }

    public object VisitIncExpr(Expr.Inc expr)
    {
        object currentValue = LookUpVariable(expr.VariableName, expr);
        if (currentValue is not double) throw new RuntimeError(expr.VariableName, "Variable passed to increment expression had non-number value at runtime.");
        environment.Assign(expr.VariableName, (double)currentValue + 1);

        return currentValue;
    }

    public object VisitDecExpr(Expr.Dec expr)
    {
        object currentValue = LookUpVariable(expr.VariableName, expr);
        if (currentValue is not double) throw new RuntimeError(expr.VariableName, "Variable passed to decrement expression had non-number value at runtime.");
        environment.Assign(expr.VariableName, (double)currentValue - 1);
        
        return currentValue;
    }

    public object VisitCallExpr(Expr.Call expr)
    {
        object callee = Evaluate(expr.Callee);

        List<object?> args = [];
        foreach (Expr arg in expr.Args) args.Add(Evaluate(arg));

        if (callee is not IKSharpCallable) throw new RuntimeError(expr.Paren, "Can only call subroutine and classes.");

        IKSharpCallable subroutine = (IKSharpCallable)callee;
        // -1 arity implies arbitrary num of args.
        if (args.Count != subroutine.Arity() && subroutine.Arity() != -1)
        {
            throw new RuntimeError(expr.Paren, $"Expected {subroutine.Arity()} arguments but got {args.Count}.");
        }
        return subroutine.Call(this, args);
    }

    public object VisitGetExpr(Expr.Get expr)
    {
        object @object = Evaluate(expr.Object);
        if (@object is KSharpInstance instance)
        {
            KSharpSubroutine? getMethod = instance.GetGetMethod(expr.Name);
            if (getMethod != null && !expr.InMethod) return getMethod.Bind(instance).Call(this, []);
            else
            {
                if (getMethod == null && instance.GetSetMethod(expr.Name) != null)
                {
                    throw new RuntimeError(expr.Name, $"Instance has a set method but no matching get method for '{expr.Name.Lexeme}'.");
                }

                return instance.GetField(expr.Name);
            }
        }
        if (@object is KSharpClass @class) return @class.GetStaticMethod(expr.Name);
        
        throw new RuntimeError(expr.Name, "Only instances have properties.");
    }

    public object VisitSetExpr(Expr.Set expr)
    {
        object? @object = Evaluate(expr.Object);

        if (@object is KSharpInstance instance)
        {
            object? value = Evaluate(expr.Value);

            KSharpSubroutine? setMethod = instance.GetSetMethod(expr.Name);
            if (setMethod != null && !expr.InMethod) setMethod.Bind(instance).Call(this, [value]);
            else
            {
                if (setMethod == null && instance.GetGetMethod(expr.Name) != null)
                {
                    throw new RuntimeError(expr.Name, $"Instance has a get method but no matching set method for '{expr.Name.Lexeme}'.");
                }

                instance.SetField(expr.Name, value, expr.InMethod);
            }

            return value;
        }
        else throw new RuntimeError(expr.Name, "Only instances have fields.");
    }

    public object VisitThisExpr(Expr.This expr)
    {
        return LookUpVariable(expr.Keyword, expr);
    }

    public object VisitSuperExpr(Expr.Super expr)
    {
        int distance = locals[expr];

        KSharpClass superclass = (KSharpClass)environment.GetAt(distance, "super");
        KSharpInstance @object = (KSharpInstance)environment.GetAt(distance - 1, "this");
        KSharpSubroutine method = superclass.FindMethod(expr.Method.Lexeme);

        if (method == null) throw new RuntimeError(expr.Method, $"Undefined property '{expr.Method.Lexeme}'.");

#pragma warning disable CS8604 // Possible null reference argument.
        return method.Bind(@object);
#pragma warning restore CS8604 // Possible null reference argument.
    }

    public object VisitExpressionStmt(Stmt.Expression stmt)
    {
        Evaluate(stmt.Expr);
        return null;
    }

    public object VisitPrintStmt(Stmt.Print stmt)
    {
        object? value = Evaluate(stmt.Expr);
        Console.WriteLine(Stringify(value));
        return null;
    }

    public object VisitVarStmt(Stmt.Var stmt)
    {
        object value = null;
        if (stmt.Initializer != null) value = Evaluate(stmt.Initializer);

        environment.Define(stmt.Name.Lexeme, value);
        return null;
    }

    public object VisitBlockStmt(Stmt.Block stmt)
    {
        ExecuteBlock(stmt.Statements, new KSharpEnvironment(environment));
        return null;
    }

    public object VisitExitStmt(Stmt.Exit stmt)
    {
        object? exitValue = Evaluate(stmt.ExitCode);
        if (exitValue is double exitNum)
        {
            if (double.IsInteger(exitNum))
            {
                int exitCode = (int)exitNum;
                Environment.Exit(exitCode);
            }

            throw new RuntimeError(stmt.Token, "Exit code must be an integer.");
        }
        
        throw new RuntimeError(stmt.Token, "Exit code must be a number.");
    }

    public object VisitIfStmt(Stmt.If stmt)
    {
        if (IsTruthy(Evaluate(stmt.Condition))) Execute(stmt.ThenBranch);
        else if (stmt.ElseBranch != null) Execute(stmt.ElseBranch);
        return null;
    }

    public object VisitWhileStmt(Stmt.While stmt)
    {
        try
        {
            while (IsTruthy(Evaluate(stmt.Condition)))
            {
                try
                {
                    Execute(stmt.Body);
                }
                catch (Continue) {}
            }
        }
        catch (Break) {}

        return null;
    }

    public object VisitIncStmt(Stmt.Inc stmt)
    {
        object currentValue = environment.Get(stmt.VariableName);
        if (currentValue is double numValue)
        {
            environment.Assign(stmt.VariableName, numValue + 1);
            return null;
        }

        throw new RuntimeError(stmt.VariableName, "Variable passed to increment statement had non-number value at runtime.");
    }

    public object VisitDecStmt(Stmt.Dec stmt)
    {
        object currentValue = environment.Get(stmt.VariableName);
        if (currentValue is double numValue)
        {
            environment.Assign(stmt.VariableName, numValue - 1);
            return null;
        }

        throw new RuntimeError(stmt.VariableName, "Variable passed to decrement statement had non-number value at runtime.");
    }

    public object VisitBreakStmt(Stmt.Break stmt)
    {
        throw new Break();
    }

    public object VisitContinueStmt(Stmt.Continue stmt)
    {
        throw new Continue();
    }

    public object VisitForStmt(Stmt.For stmt)
    {
        if (stmt.Initializer != null) Execute(stmt.Initializer);

        try
        {
            while (IsTruthy(Evaluate(stmt.Condition)))
            {
                try
                {
                    Execute(stmt.Body);
                }
                catch (Continue) {}

                if (stmt.Increment != null) Execute(stmt.Increment);
            }
        }
        catch (Break) {}

        return null;
    }

    public object VisitSubroutineStmt(Stmt.Subroutine stmt)
    {
        KSharpSubroutine subroutine = new(stmt, environment, false);
        environment.Define(stmt.Name.Lexeme, subroutine);
        return null;
    }

    public object VisitReturnStmt(Stmt.Return stmt)
    {
        object? value = null;
        if (stmt.Value != null) value = Evaluate(stmt.Value);
        throw new Return(value);
    }

    public object VisitClassStmt(Stmt.Class stmt)
    {
        KSharpClass? superclass = null;
        if (stmt.Superclass != null)
        {
            object? superclassObject = Evaluate(stmt.Superclass);
            if (superclassObject is not KSharpClass) throw new RuntimeError(stmt.Superclass.Name, "Superclass must be a class.");
            superclass = (KSharpClass)superclassObject;
        }

        environment.Define(stmt.Name.Lexeme, null);

        if (stmt.Superclass != null)
        {
            environment = new KSharpEnvironment(environment);
            environment.Define("super", superclass);
        }

        Dictionary<string, KSharpSubroutine> methods = [];
        Dictionary<string, KSharpSubroutine> staticMethods = [];
        Dictionary<string, KSharpSubroutine> getMethods = [];
        Dictionary<string, KSharpSubroutine> setMethods = [];

        foreach (Stmt.Subroutine method in stmt.Methods)
        {
            KSharpSubroutine subroutine = new(method, environment, method.Name.Lexeme == "construct");
            methods.Add(method.Name.Lexeme, subroutine);
        }

        foreach(Stmt.Subroutine staticMethod in stmt.StaticMethods)
        {
            KSharpSubroutine subroutine = new(staticMethod, environment, false);
            staticMethods.Add(staticMethod.Name.Lexeme, subroutine);
        }

        foreach(Stmt.Subroutine getMethod in stmt.GetMethods)
        {
            KSharpSubroutine subroutine = new(getMethod, environment, false);
            getMethods.Add(getMethod.Name.Lexeme, subroutine);
        }

        foreach(Stmt.Subroutine setMethod in stmt.SetMethods)
        {
            KSharpSubroutine subroutine = new(setMethod, environment, false);
            setMethods.Add(setMethod.Name.Lexeme, subroutine);
        }

        KSharpClass @class = new(stmt.Name.Lexeme, superclass, methods, staticMethods, getMethods, setMethods);
#pragma warning disable CS8601 // Possible null reference assignment.
        if (superclass != null) environment = environment.Enclosing;
#pragma warning restore CS8601 // Possible null reference assignment.
        environment.Assign(stmt.Name, @class);
        return null;
    }

    private void CheckNumberOperand(Token @operator, object? operand)
    {
        if (operand is double) return;
        throw new RuntimeError(@operator, "Operand must be a number.");
    }

    private void CheckNumberOperands(Token @operator, object? left, object? right)
    {
        if (left is double && right is double) return;
        throw new RuntimeError(@operator, "Operands must be numbers.");
    }

    private bool IsTruthy(object? @object)
    {
        if (@object == null) return false;
        if (@object is bool) return (bool)@object;
        return true;
    }

    private bool AreEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null) return false;

        return a.Equals(b);
    }

    public string Stringify(object? @object)
    {
        if (@object == null) return "zilch";
        
        if (@object is double number)
        {
            if (double.IsPositiveInfinity(number)) return "+inf";
            if (double.IsNegativeInfinity(number)) return "-inf";

            string text = number.ToString();
            if (text.EndsWith(".0"))
            {
                text = text[0..(text.Length - 2)];
            }
            return text;
        }

        if (@object is List<object?> list)
        {
            string text = "[";

            for (int i = 0; i < list.Count; i++)
            {
                text += Stringify(list[i]);
                if (i != list.Count - 1) text += ", ";
            }

            text += "]";

            return text;
        }

        return @object.ToString();
    }

    private object? Evaluate(Expr expr)
    {
        return expr.Accept(this);
    }

    public object? TryPrintExpr(Expr expr)
    {
        try
        {
            Stmt.Print printStmt = new(expr);
            Execute(printStmt);
        }
        catch (RuntimeError error)
        {
            KSharp.RuntimeError(error);
        }

        return null;
    }

    public void Execute(Stmt stmt)
    {
        stmt.Accept(this);
    }

    public void Resolve(Expr expr, int depth)
    {
        locals.Add(expr, depth);
    }

    public void ExecuteBlock(List<Stmt> statements, KSharpEnvironment environment)
    {
        KSharpEnvironment previous = this.environment;

        try
        {
            this.environment = environment;

            foreach (Stmt statement in statements)
            {
                Execute(statement);
            }
        }
        finally
        {
            this.environment = previous;
        }
    }
}