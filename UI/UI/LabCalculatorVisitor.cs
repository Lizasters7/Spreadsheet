using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabCalculator
{
    class LabCalculatorVisitor : LabCalculatorBaseVisitor<object>
    {
       Dictionary<string, double> tableIdentifier = new Dictionary<string, double>();

        public override object VisitCompileUnit(LabCalculatorParser.CompileUnitContext context)
        {
            return Visit(context.expression());
        }

        public override object VisitNumberExpr(LabCalculatorParser.NumberExprContext context)
        {
            var result = double.Parse(context.GetText());
            Debug.WriteLine(result);
            return result;
        }

        public override object VisitIdentifierExpr(LabCalculatorParser.IdentifierExprContext context)
        {
            var result = context.GetText();
            double value;
            if (tableIdentifier.TryGetValue(result.ToString(), out value))
            {
                return value;
            }
            else
            {
                return 0.0;
            }
        }

        public override object VisitParenthesizedExpr(LabCalculatorParser.ParenthesizedExprContext context)
        {
            return Visit(context.expression());
        }

        public override object VisitAdditiveExpr(LabCalculatorParser.AdditiveExprContext context)
        {
            var left = WalkLeft(context);
            var right = WalkRight(context);
            if (context.operatorToken.Type == LabCalculatorLexer.ADD)
            {
                Debug.WriteLine("{0} + {1}", left, right);
                return left + right;
            }
            else 
            {
                Debug.WriteLine("{0} - {1}", left, right);
                return left - right;
            }
        }

        public override object VisitMultiplicativeExpr(LabCalculatorParser.MultiplicativeExprContext context)
        {
            var left = WalkLeft(context);
            var right = WalkRight(context);
            if (context.operatorToken.Type == LabCalculatorLexer.MULTIPLY)
            {
                Debug.WriteLine("{0} * {1}", left, right);
                return left * right;
            }
            else 
            {
                if (context.operatorToken.Type == LabCalculatorLexer.DIVIDE && right == 0)
                {
                    throw new DivideByZeroException("Division by zero");
                }
                Debug.WriteLine("{0} / {1}", left, right);
                return left / right;
            }
        }

        public override object VisitUnaryExpr(LabCalculatorParser.UnaryExprContext context)
        {
            var value = (double)Visit(context.expression());
            if (context.operatorToken.Type == LabCalculatorLexer.SUBTRACT)
            {
                Debug.WriteLine("unary - {0}", value);
                return -value;
            }
            else 
            {
                return value;
            }
        }

        public override object VisitIncDecExpr(LabCalculatorParser.IncDecExprContext context)
        {
            var value = (double)Visit(context.expression());
            if (context.operatorToken.Type == LabCalculatorLexer.INC)
            {
                Debug.WriteLine("inc({0})", value);
                return value + 1;
            }
            else 
            {
                Debug.WriteLine("dec({0})", value);
                return value - 1;
            }
        }

        public override object VisitComparisonExpr(LabCalculatorParser.ComparisonExprContext context)
        {
            var left = WalkLeftRaw(context);
            var right = WalkRightRaw(context);
            bool result;
            if (left is bool leftBool && right is bool rightBool)
            {
                if (context.operatorToken.Type == LabCalculatorLexer.EQUAL)
                {
                    result = leftBool == rightBool;
                }
                else if (context.operatorToken.Type == LabCalculatorLexer.NOT_EQUAL)
                {
                    result = leftBool != rightBool;
                }
                else
                {
                    throw new ArgumentException("This comparison operator is not supported for boolean values");
                }
            }
            else if (left is double leftNum && right is double rightNum)
            {
                if (context.operatorToken.Type == LabCalculatorLexer.EQUAL)
                {
                    result = leftNum == rightNum;
                }
                else if (context.operatorToken.Type == LabCalculatorLexer.NOT_EQUAL)
                {
                    result = leftNum != rightNum;
                }
                else if (context.operatorToken.Type == LabCalculatorLexer.LESS)
                {
                    result = leftNum < rightNum;
                }
                else if (context.operatorToken.Type == LabCalculatorLexer.GREATER)
                {
                    result = leftNum > rightNum;
                }
                else if (context.operatorToken.Type == LabCalculatorLexer.LESS_EQUAL)
                {
                    result = leftNum <= rightNum;
                }
                else 
                {
                    result = leftNum >= rightNum;
                }
            }
            else
            {
                throw new ArgumentException("Cannot compare a number with a boolean value");
            }

            Debug.WriteLine("{0} {1} -> {2}", left, right, result);
            return result;
        }

        private double WalkLeft(LabCalculatorParser.ExpressionContext context)
        {
            var value = Visit(context.GetRuleContext<LabCalculatorParser.ExpressionContext>(0));
            if (!(value is double))
            {
                throw new ArgumentException("Expected a number, got a boolean value");
            }
            return (double)Visit(context.GetRuleContext<LabCalculatorParser.ExpressionContext>(0));
        }

        private object WalkLeftRaw(LabCalculatorParser.ExpressionContext context)
        {
            return Visit(context.GetRuleContext<LabCalculatorParser.ExpressionContext>(0));
        }

        private object WalkRightRaw(LabCalculatorParser.ExpressionContext context)
        {
            return Visit(context.GetRuleContext<LabCalculatorParser.ExpressionContext>(1));
        }

        private double WalkRight(LabCalculatorParser.ExpressionContext context)
        {
            var value = Visit(context.GetRuleContext<LabCalculatorParser.ExpressionContext>(1));
            if (!(value is double))
            {
                throw new ArgumentException("Expected a number, got a boolean value");
            }
            return (double)Visit(context.GetRuleContext<LabCalculatorParser.ExpressionContext>(1));
        }

        public override object VisitTrueExpr(LabCalculatorParser.TrueExprContext context)
        {
            return true;
        }

        public override object VisitFalseExpr(LabCalculatorParser.FalseExprContext context)
        {
            return false;
        }
    }
}
