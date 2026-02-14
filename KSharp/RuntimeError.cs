namespace KSharpInterpreter;

class RuntimeError(Token token, string message) : Exception(message)
{
    public readonly Token Token = token;
}