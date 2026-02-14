namespace KSharpInterpreter;

class KSharpInstance(KSharpClass @class)
{
    private readonly KSharpClass @class = @class;
    private readonly Dictionary<string, object?> fields = [];

    public object? GetField(Token name)
    {
        if (fields.TryGetValue(name.Lexeme, out object? value)) return value;

        KSharpSubroutine? method = @class.FindMethod(name.Lexeme);
        if (method != null) return method.Bind(this);

        throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.");
    }

    public void SetField(Token name, object? value, bool inMethod)
    {
        if (fields.ContainsKey(name.Lexeme)) fields[name.Lexeme] = value;
        else if (inMethod) fields.Add(name.Lexeme, value);
        else throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.");
    }

    public KSharpSubroutine? GetGetMethod(Token name)
    {
        KSharpSubroutine? getMethod = @class.FindGetMethod(name.Lexeme);
        if (getMethod != null) return getMethod.Bind(this);
        else return null;
    }

    public KSharpSubroutine? GetSetMethod(Token name)
    {
        KSharpSubroutine? setMethod = @class.FindSetMethod(name.Lexeme);
        if (setMethod != null) return setMethod.Bind(this);
        else return null;
    }

    public override string ToString()
    {
        return $"<{@class.Name} instance>";
    }
}