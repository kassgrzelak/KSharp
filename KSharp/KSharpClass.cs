namespace KSharpInterpreter;

class KSharpClass(string name, KSharpClass? superclass, Dictionary<string, KSharpSubroutine> methods, Dictionary<string, KSharpSubroutine> staticMethods,
                  Dictionary<string, KSharpSubroutine> getMethods, Dictionary<string, KSharpSubroutine> setMethods) : IKSharpCallable
{
    public readonly string Name = name;
    public readonly KSharpClass? Superclass = superclass; 
    private readonly Dictionary<string, KSharpSubroutine> methods = methods;
    private readonly Dictionary<string, KSharpSubroutine> staticMethods = staticMethods;
    private readonly Dictionary<string, KSharpSubroutine> getMethods = getMethods;
    private readonly Dictionary<string, KSharpSubroutine> setMethods = setMethods;

    public KSharpSubroutine? FindMethod(string name)
    {
        methods.TryGetValue(name, out KSharpSubroutine? method);
        if (method == null && Superclass != null) return Superclass.FindMethod(name);
        return method;
    }

    public KSharpSubroutine? FindGetMethod(string name)
    {
        getMethods.TryGetValue(name, out KSharpSubroutine? method);
        if (method == null && Superclass != null) return Superclass.FindGetMethod(name);
        return method;
    }

    public KSharpSubroutine? FindSetMethod(string name)
    {
        setMethods.TryGetValue(name, out KSharpSubroutine? method);
        if (method == null && Superclass != null) return Superclass.FindSetMethod(name);
        return method;
    }

    public KSharpSubroutine GetStaticMethod(Token name)
    {
        if (staticMethods.TryGetValue(name.Lexeme, out KSharpSubroutine? staticMethod)) return staticMethod;
        if (Superclass != null) return Superclass.GetStaticMethod(name);

        throw new RuntimeError(name, $"Undefined static method '{name.Lexeme}'.");
    }

    public override string ToString()
    {
        return $"<{Name} class>";
    }

    public int Arity()
    {
        KSharpSubroutine? initializer = FindMethod("construct");
        if (initializer == null) return 0;
        return initializer.Arity();
    }

    public object? Call(Interpreter interpreter, List<object?> args)
    {
        KSharpInstance instance = new(this);
        KSharpSubroutine? initializer = FindMethod("construct");
        if (initializer != null) initializer.Bind(instance).Call(interpreter, args);
        
        return instance;
    }
}