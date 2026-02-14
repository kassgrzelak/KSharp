// using System.ComponentModel;
// using System.Text;

// namespace KSharpInterpreter;

// class AstPrinter : Expr.IVisitor<string>
// {
//     public string Print(Expr expr)
//     {
//         return expr.Accept(this);
//     }

//     public string VisitBinaryExpr(Expr.Binary expr)
//     {
//         return Parenthesize(expr.Operator.Lexeme, expr.Left, expr.Right);
//     }

//     public string VisitGroupingExpr(Expr.Grouping expr)
//     {
//         return Parenthesize("Group", expr.Expression);
//     }

//     public string VisitLiteralExpr(Expr.Literal expr)
//     {
//         // if this code looks fucking weird to you it's because it is
//         // my IDE does NOT like me
//         if (expr.Value == null) return "zilch";

//         string? value = expr.Value.ToString();

//         if (value == null) return "zilch";
//         else return value;
//     }

//     public string VisitUnaryExpr(Expr.Unary expr)
//     {
//         return Parenthesize(expr.Operator.Lexeme, expr.Right);
//     }

//     public string VisitConditionalExpr(Expr.Conditional expr)
//     {
//         return Parenthesize("Conditional", expr.Condition, expr.A, expr.B);
//     }

//     private string Parenthesize(string name, params Expr[] exprs)
//     {
//         StringBuilder builder = new();

//         builder.Append('(').Append(name);
//         foreach (Expr expr in exprs)
//         {
//             builder.Append(' ');
//             builder.Append(expr.Accept(this));
//         }
//         builder.Append(')');

//         return builder.ToString();
//     }
// }