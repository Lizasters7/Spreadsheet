using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabCalculator
{
    public static class Calculator
    {
        public static object Evaluate(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new ArgumentException("Expression cannot be empty");
            }
            var lexer = new LabCalculatorLexer(new AntlrInputStream(expression));
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new ThrowExceptionErrorListener());
            var tokens = new CommonTokenStream(lexer);
            var parser = new LabCalculatorParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ThrowExceptionErrorListener());
            var tree = parser.compileUnit();
            var visitor = new LabCalculatorVisitor();
            return visitor.Visit(tree);
        }
    }
}
