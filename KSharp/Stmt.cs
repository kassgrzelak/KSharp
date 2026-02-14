using System.IO.Compression;
using System.Reflection.Metadata.Ecma335;

namespace KSharpInterpreter;

abstract class Stmt
{
	public interface IVisitor<R>
	{
		public R VisitExpressionStmt(Expression stmt);
		public R VisitPrintStmt(Print stmt);
		public R VisitVarStmt(Var stmt);
		public R VisitBlockStmt(Block stmt);
		public R VisitExitStmt(Exit stmt);
		public R VisitIfStmt(If stmt);
		public R VisitWhileStmt(While stmt);
		public R VisitIncStmt(Inc stmt);
		public R VisitDecStmt(Dec stmt);
		public R VisitBreakStmt(Break stmt);
		public R VisitContinueStmt(Continue stmt);
		public R VisitForStmt(For stmt);
		public R VisitSubroutineStmt(Subroutine stmt);
		public R VisitReturnStmt(Return stmt);
		public R VisitClassStmt(Class stmt);
	}

	public abstract R Accept<R>(IVisitor<R> visitor);
	public class Expression(Expr expression) : Stmt
	{
		public readonly Expr Expr = expression;
		public override R Accept<R>(IVisitor<R> visitor)
		{
			return visitor.VisitExpressionStmt(this);
		}
	}

	public class Print(Expr expression) : Stmt
	{
		public readonly Expr Expr = expression;
		public override R Accept<R>(IVisitor<R> visitor)
		{
			return visitor.VisitPrintStmt(this);
		}
	}

	public class Var(Token name, Expr? initializer) : Stmt
	{
		public readonly Token Name = name;
		public readonly Expr? Initializer = initializer;
		public override R Accept<R>(IVisitor<R> visitor)
		{
			return visitor.VisitVarStmt(this);
		}
	}

	public class Block(List<Stmt> statements) : Stmt
	{
		public readonly List<Stmt> Statements = statements;
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitBlockStmt(this);
        }
    }

	public class Exit(Token token, Expr exitCode) : Stmt
	{
		public readonly Token Token = token;
		public readonly Expr ExitCode = exitCode;
        public override R Accept<R>(IVisitor<R> visitor)
        {
			return visitor.VisitExitStmt(this);
        }
    }

	public class If(Expr condition, Stmt thenBranch, Stmt? elseBranch) : Stmt
	{
		public readonly Expr Condition = condition;
		public readonly Stmt ThenBranch = thenBranch;
		public readonly Stmt? ElseBranch = elseBranch;
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitIfStmt(this);
        }
    }

	public class While(Expr condition, Stmt body) : Stmt
	{
		public readonly Expr Condition = condition;
		public readonly Stmt Body = body;
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitWhileStmt(this);
        }
    }

	public class Inc(Token variableName) : Stmt
	{
		public readonly Token VariableName = variableName;
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitIncStmt(this);
        }
    }

	public class Dec(Token variableName) : Stmt
	{
		public readonly Token VariableName = variableName;
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitDecStmt(this);
        }
    }

	public class Break : Stmt
	{
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitBreakStmt(this);
        }
    }

	public class Continue : Stmt
	{
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitContinueStmt(this);
        }
    }

	public class For(Stmt? initializer, Expr condition, Expression? increment, Stmt body) : Stmt
	{
		public readonly Stmt? Initializer = initializer;
		public readonly Expr Condition = condition;
		public readonly Expression? Increment = increment;
		public readonly Stmt Body = body;
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitForStmt(this);
        }
    }

	public class Subroutine(Token name, List<Token> @params, List<Stmt> body) : Stmt
	{
		public readonly Token Name = name;
		public readonly List<Token> Params = @params;
		public readonly List<Stmt> Body = body;
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitSubroutineStmt(this);
        }
    }

	public class Return(Token keyword, Expr? value) : Stmt
	{
		public readonly Token Keyword = keyword;
		public readonly Expr? Value = value;
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitReturnStmt(this);
        }
    }

	public class Class(Token name, Expr.Variable? superclass, List<Subroutine> methods, List<Subroutine> staticMethods,
					   List<Subroutine> getMethods, List<Subroutine> setMethods) : Stmt
	{
		public readonly Token Name = name;
		public readonly Expr.Variable? Superclass = superclass;
		public readonly List<Subroutine> Methods = methods;
		public readonly List<Subroutine> StaticMethods = staticMethods;
		public readonly List<Subroutine> GetMethods = getMethods;
		public readonly List<Subroutine> SetMethods = setMethods;
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitClassStmt(this);
        }
    }
}