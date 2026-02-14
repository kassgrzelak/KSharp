namespace KSharpInterpreter;

interface IKSharpCallable
{
    public int Arity();
    public object? Call(Interpreter interpreter, List<object?> args);
}