namespace KSharpInterpreter;

class Return(object? value) : Exception
{
    public readonly object? Value = value;
}