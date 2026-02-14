namespace KSharpInterpreter;

class KSharpEnvironment
{
    public readonly KSharpEnvironment? Enclosing;
    private readonly Dictionary<string, object?> values = [];

    public KSharpEnvironment()
    {
        Enclosing = null;
    }

    public KSharpEnvironment(KSharpEnvironment enclosing)
    {
        this.Enclosing = enclosing;
    }

    public void Define(string name, object? value)
    {
        if (values.ContainsKey(name)) values[name] = value;
        else values.Add(name, value);
    }

    public void Assign(Token name, object? value)
    {
        if (values.ContainsKey(name.Lexeme))
        {
            values[name.Lexeme] = value;
            return;
        }

        if (Enclosing != null)
        {
            Enclosing.Assign(name, value);
            return;
        }

        throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }

    public void AssignAt(int distance, Token name, object? value)
    {
        Ancestor(distance).values[name.Lexeme] = value;
    }

    public object? Get(Token name)
    {
        if (values.TryGetValue(name.Lexeme, out object? p)) return p;

        if (Enclosing != null) return Enclosing.Get(name);

        throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }

    public object? GetAt(int distance, string name)
    {
        return Ancestor(distance).values[name];
    }

    KSharpEnvironment Ancestor(int distance)
    {
        KSharpEnvironment environment = this;
        for (int i = 0; i < distance; i++)
        {
            if (environment.Enclosing != null) environment = environment.Enclosing;
        }

        return environment;
    }
}