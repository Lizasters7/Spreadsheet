using System;
using System.Globalization;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LabCalculator.Tests
{
    public class CalculatorTests
    {
        private const double Eps = 1e-9;

        private static double EvalNumber(string expression)
        {
            object result = Calculator.Evaluate(expression);
            Assert.IsInstanceOfType(result, typeof(double), $"'{expression}' мав повернути число");
            return (double)result;
        }

        private static bool EvalBool(string expression)
        {
            object result = Calculator.Evaluate(expression);
            Assert.IsInstanceOfType(result, typeof(bool), $"'{expression}' мав повернути bool");
            return (bool)result;
        }

        public void EvaluateSingleIntegerReturnsThatNumber()
        {
            string expression = "42";
            object result = Calculator.Evaluate(expression);
            Assert.AreEqual(42.0, (double)result, Eps);
        }

        public void Evaluate_DecimalNumber_UkrainianCulture_ParsesWithDot()
        {
            CultureInfo original = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("uk-UA");
                Assert.AreEqual(2.5, EvalNumber("2.5"), Eps);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original; // повертаємо як було
            }
        }

        public void EvaluateBinaryOperationReturnsCorrectResult(string expression, double expected)
        {
            Assert.AreEqual(expected, EvalNumber(expression), Eps);
        }

        public void EvaluateOperatorPrecedenceIsRespected(string expression, double expected)
        {
            Assert.AreEqual(expected, EvalNumber(expression), Eps);
        }

        public void Evaluate_SamePrecedence_IsLeftAssociative(string expression, double expected)
        {
            Assert.AreEqual(expected, EvalNumber(expression), Eps);
        }

        public void Evaluate_UnaryOperators_ReturnCorrectResult(string expression, double expected)
        {
            Assert.AreEqual(expected, EvalNumber(expression), Eps);
        }

        public void Evaluate_IncDec_ReturnCorrectResult(string expression, double expected)
        {
            Assert.AreEqual(expected, EvalNumber(expression), Eps);
        }

        public void Evaluate_NumberComparison_ReturnsBool(string expression, bool expected)
        {
            Assert.AreEqual(expected, EvalBool(expression));
        }

        public void Evaluate_ComparisonHasLowestPrecedence(string expression, bool expected)
        {
            Assert.AreEqual(expected, EvalBool(expression));
        }

        public void Evaluate_BooleanLiteralsAndEquality(string expression, bool expected)
        {
            Assert.AreEqual(expected, EvalBool(expression));
        }

        public void Evaluate_UnknownIdentifier_IsTreatedAsZero(string expression, double expected)
        {
            Assert.AreEqual(expected, EvalNumber(expression), Eps);
        }


        public void Evaluate_DivisionByZero_ThrowsDivideByZeroException(string expression)
        {
            Assert.ThrowsException<DivideByZeroException>(() => Calculator.Evaluate(expression));
        }

        public void Evaluate_EmptyInput_ThrowsArgumentException(string expression)
        {
            Assert.ThrowsException<ArgumentException>(() => Calculator.Evaluate(expression));
        }

        public void Evaluate_InvalidSyntax_ThrowsArgumentException(string expression)
        {
            Assert.ThrowsException<ArgumentException>(() => Calculator.Evaluate(expression));
        }

        public void Evaluate_TypeMismatch_ThrowsArgumentException(string expression)
        {
            Assert.ThrowsException<ArgumentException>(() => Calculator.Evaluate(expression));
        }

        public void Evaluate_UnaryOrIncDecOnBool_ThrowsArgumentException(string expression)
        {
            Assert.ThrowsException<ArgumentException>(() => Calculator.Evaluate(expression));
        }
    }
}