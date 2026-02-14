namespace KSharpInterpreter;

class Clock : IKSharpCallable
{
    public int Arity() { return 0; }

    public object? Call(Interpreter interpreter, List<object?> args)
    {
        return Environment.TickCount / 1000.0;
    }

    public override string ToString()
    {
        return "<native sub>";
    }
}

class Snooze : IKSharpCallable
{
    public int Arity() { return 1; }

    public object? Call(Interpreter interpreter, List<object?> args)
    {
        if (args[0] is double secs)
        {
            Thread.Sleep((int)(secs * 1000));
            return null;
        }
        throw new RuntimeError(Token.UnknownToken(), "Expected number input.");
    }

    public override string ToString()
    {
        return "<native sub>";
    }
}

class Print : IKSharpCallable
{
    public int Arity() { return -1; }

    public object? Call(Interpreter interpreter, List<object?> args)
    {
        if (args.Count == 0) Console.WriteLine();
        else
        {
            foreach (object? obj in args)
            {
                Console.Write(interpreter.Stringify(obj));
                Console.Write(" ");
            }
            Console.Write("\n");
        }

        return null;
    }

    public override string ToString()
    {
        return "<native sub>";
    }
}

class Input : IKSharpCallable
{
    public int Arity() { return -1; }

    public object? Call(Interpreter interpreter, List<object?> args)
    {
        if (args.Count == 0) return Console.ReadLine();
        if (args.Count > 1) throw new RuntimeError(Token.UnknownToken(), "Input takes 0 or 1 arguments.");
        if (args[0] is string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine();
        }
        else
        {
            throw new RuntimeError(Token.UnknownToken(), "Prompt must be a string.");
        }
    }

    public override string ToString()
    {
        return "<native sub>";
    }
}

class Round : IKSharpCallable
{
    public int Arity() { return -1; }

    public object? Call(Interpreter interpreter, List<object?> args)
    {
        if (args.Count == 0 || args.Count > 2) throw new RuntimeError(Token.UnknownToken(), "Round takes 1 or 2 arguments.");
        if (args[0] is double num)
        {
            int digits = 0;

            if (args.Count == 2)
            {
                if (args[1] is double digitsDouble)
                {
                    if (double.IsInteger(digitsDouble)) digits = (int)digitsDouble;
                    else throw new RuntimeError(Token.UnknownToken(), "digits must be an integer.");
                }
                else throw new RuntimeError(Token.UnknownToken(), "digits must be an integer.");
            }

            return Math.Round(num, digits);
        }
        throw new RuntimeError(Token.UnknownToken(), "Expected number input.");
    }

    public override string ToString()
    {
        return "<native sub>";
    }
}

class String : IKSharpCallable
{
    public int Arity() { return 1; }

    public object? Call(Interpreter interpreter, List<object?> args)
    {
        return interpreter.Stringify(args[0]);
    }

    public override string ToString()
    {
        return "<native sub>";
    }
}

class Sqrt : IKSharpCallable
{
    public int Arity() { return 1; }

    public object? Call(Interpreter interpreter, List<object?> args)
    {
        if (args[0] is double num)
        {
            return Math.Sqrt(num);
        }

        throw new RuntimeError(Token.UnknownToken(), "Expected number input.");
    }

    public override string ToString()
    {
        return "<native sub>";
    }
}

// class Template : IKSharpCallable
// {
//     public int Arity() { return n; }

//     public object? Call(Interpreter interpreter, List<object?> args)
//     {
//         if (args[0] is type thing)
//         {
//             // Do stuff.
//         }
//         else
//         {
//             throw new RuntimeError(Token.UnknownToken(), "Argument must be a <type>.");
//         }
//     }

//     public override string ToString()
//     {
//         return "<native sub>";
//     }
// }