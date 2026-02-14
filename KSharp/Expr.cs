using System.Dynamic;
using System.Security.Cryptography;

namespace KSharpInterpreter;

abstract class Expr
{
	public interface IVisitor<R>
	{
		public R VisitBinaryExpr(Binary expr);
		public R VisitGroupingExpr(Grouping expr);
		public R VisitLiteralExpr(Literal expr);
		public R VisitUnaryExpr(Unary expr);
		public R VisitConditionalExpr(Conditional expr);
		public R VisitVariableExpr(Variable expr);
		public R VisitAssignExpr(Assign expr);
		public R VisitLogicalExpr(Logical expr);
		public R VisitIncExpr(Inc expr);
		public R VisitDecExpr(Dec expr);
		public R VisitCallExpr(Call expr);
		public R VisitGetExpr(Get expr);
		public R VisitSetExpr(Set expr);
		public R VisitThisExpr(This expr);
		public R VisitSuperExpr(Super expr);
	}

	public abstract R Accept<R>(IVisitor<R> visitor);

	public class Binary(Expr Left, Token Operator, Expr Right) : Expr
	{
		public readonly Expr Left = Left;
		public readonly Token Operator = Operator;
		public readonly Expr Right = Right;
		public override R Accept<R>(IVisitor<R> visitor)
		{
			return visitor.VisitBinaryExpr(this);
		}
	}

	public class Grouping(Expr expression) : Expr
	{
		public readonly Expr Expression = expression;
		public override R Accept<R>(IVisitor<R> visitor)
		{
			return visitor.VisitGroupingExpr(this);
		}
	}

	public class Literal(object? value) : Expr
	{
		public readonly object? Value = value;
		public override R Accept<R>(IVisitor<R> visitor)
		{
			return visitor.VisitLiteralExpr(this);
		}
	}

	public class Unary(Token @operator, Expr right) : Expr
	{
		public readonly Token Operator = @operator;
		public readonly Expr Right = right;
		public override R Accept<R>(IVisitor<R> visitor)
		{
			return visitor.VisitUnaryExpr(this);
		}
	}

	public class Conditional(Expr condition, Expr then, Expr @else) : Expr
	{
		public readonly Expr Condition = condition;
		public readonly Expr Then = then;
		public readonly Expr Else = @else;

		public override R Accept<R>(IVisitor<R> visitor)
		{
			return visitor.VisitConditionalExpr(this);
		}
	}

	public class Variable(Token name) : Expr
	{
		public readonly Token Name = name;
		public override R Accept<R>(IVisitor<R> visitor)
		{
			return visitor.VisitVariableExpr(this);
		}
	}

	public class Assign(Token name, Expr value) : Expr
	{
		public readonly Token Name = name;
		public readonly Expr Value = value;
		public override R Accept<R>(IVisitor<R> visitor)
		{
			return visitor.VisitAssignExpr(this);
		}
	}

	public class Logical(Expr left, Token @operator, Expr right) : Expr
	{
		public readonly Expr Left = left;
		public readonly Token Operator = @operator;
		public readonly Expr Right = right;
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitLogicalExpr(this);
        }
    }

	public class Inc(Token variableName) : Expr
	{
		public readonly Token VariableName = variableName;
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitIncExpr(this);
        }
    }

	public class Dec(Token variableName) : Expr
	{
		public readonly Token VariableName = variableName;
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitDecExpr(this);
        }
    }

	public class Call(Expr callee, Token paren, List<Expr> args) : Expr
	{
		public readonly Expr Callee = callee;
		public readonly Token Paren = paren;
		public readonly List<Expr> Args = args;
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitCallExpr(this);
        }
    }

	public class Get(Expr @object, Token name, bool inMethod) : Expr
	{
		public readonly Expr Object = @object;
		public readonly Token Name = name;
		public bool InMethod = inMethod;
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitGetExpr(this);
        }
    }

	public class Set(Expr @object, Token name, Expr value, bool inMethod) : Expr
	{
		public readonly Expr Object = @object;
		public readonly Token Name = name;
		public readonly Expr Value = value;
		public readonly bool InMethod = inMethod;
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitSetExpr(this);
        }
    }

	public class This(Token keyword) : Expr
	{
		public readonly Token Keyword = keyword;
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitThisExpr(this);
        }
    }

	public class Super(Token keyword, Token method) : Expr
	{
		public readonly Token Keyword = keyword;
		public readonly Token Method = method;
        public override R Accept<R>(IVisitor<R> visitor)
        {
            return visitor.VisitSuperExpr(this);
        }
    }
}