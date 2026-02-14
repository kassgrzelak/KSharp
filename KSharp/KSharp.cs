using System.Runtime.InteropServices;
using System.Security.Principal;

namespace KSharpInterpreter;

internal class KSharp
{
    private static readonly Interpreter interpreter = new();
    static bool hadError = false;
    static bool hadRuntimeError = false;

    public static void Main(string[] args)
    {
        // Can only accept 0 or 1 arguments.
        if (args.Length > 1)
        {
            Console.Error.WriteLine("Usage: ksharp [script]");
            Environment.Exit(64);
        }
        // 1 argument == path to file to run.
        else if (args.Length == 1)
        {
            RunFile(args[0]);
        }
        // Start REPL (Read, Evaluate, Print, Loop).
        else
        {
            RunPrompt();
        }
    }

    private static void RunFile(string path)
    {
        string fileText = File.ReadAllText(path);
        Run(fileText);

        if (hadError) Environment.Exit(65);
        if (hadRuntimeError) Environment.Exit(70);
    }

    // REPL
    private static void RunPrompt()
    {
        while (true)
        {
            Console.Write("> ");
            string? line = Console.ReadLine();

            // Quit REPL if input is nothing.
            if (line == null || line == "") break;
            RunREPL(line);
            hadError = false;
        }
    }

    private static void Run(string source)
    {
        Scanner scanner = new(source);
        List<Token> tokens = scanner.ScanTokens();
        Parser parser = new(tokens);
        List<Stmt> statements = parser.Parse();

        // Stop if there was a syntax error.
        if (hadError) return;

        Resolver resolver = new(interpreter);
        resolver.Resolve(statements);

        if (hadError) return;

        interpreter.Interpret(statements);
    }

    private static void RunREPL(string source)
    {
        Scanner scanner = new(source);
        List<Token> tokens = scanner.ScanTokens();
        Parser parser = new(tokens);
        List<Stmt> statements = parser.Parse();

        // Stop if there was a syntax error.
        if (hadError) return;

        Resolver resolver = new(interpreter);
        resolver.Resolve(statements);

        if (hadError) return;

        if (statements.Count == 1 && statements[0] is Stmt.Expression expression) interpreter.TryPrintExpr(expression.Expr);
        else interpreter.Interpret(statements);
    }

    public static void DisplayError(int line, string message)
    {
        Report(line, "", message);
    }

    public static void DisplayError(Token token, string message)
    {
        if (token.Type == TokenType.EOF) Report(token.Line, " at end", message);
        else Report(token.Line, $" at '{token.Lexeme}'", message);
    }

    public static void RuntimeError(RuntimeError error)
    {
        Console.Error.WriteLine($"{error.Message}\n[line {error.Token.Line}]");
        hadRuntimeError = true;
    }

    private static void Report(int line, string where, string message)
    {
        Console.Error.WriteLine($"[line {line}] Error{where}: {message}");
        hadError = true;
    }
}