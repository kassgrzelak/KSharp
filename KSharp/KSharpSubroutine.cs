namespace KSharpInterpreter;

class KSharpSubroutine(Stmt.Subroutine declaration, KSharpEnvironment closure, bool isConstructor) : IKSharpCallable
{
    private readonly Stmt.Subroutine declaration = declaration;
    private readonly KSharpEnvironment closure = closure;
    public readonly bool isConstructor = isConstructor;

    public KSharpSubroutine Bind(KSharpInstance instance)
    {
        KSharpEnvironment environment = new(closure);
        environment.Define("this", instance);
        return new KSharpSubroutine(declaration, environment, isConstructor);
    }

    public int Arity()
    {
        return declaration.Params.Count;
    }

    public object? Call(Interpreter interpreter, List<object?> args)
    {
        KSharpEnvironment environment = new(closure);

        for (int i = 0; i < declaration.Params.Count; i++)
        {
            environment.Define(declaration.Params[i].Lexeme, args[i]);
        }

        try
        {
            interpreter.ExecuteBlock(declaration.Body, environment);
        }
        catch (Return returnValue)
        {
            if (isConstructor) return closure.GetAt(0, "this");
            return returnValue.Value;
        }

        if (isConstructor) return closure.GetAt(0, "this");

        return null;
    }

    public override string ToString()
    {
        return $"<sub {declaration.Name.Lexeme}>";
    }
}