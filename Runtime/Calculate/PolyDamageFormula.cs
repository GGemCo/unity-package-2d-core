using System;
using System.Collections.Generic;
using System.Globalization;

namespace GGemCo2DCore
{
    /// <summary>
    /// 문자열 수식을 런타임 계산용 RPN 토큰으로 컴파일한 Poly 데미지 공식입니다.
    /// </summary>
    public sealed class PolyDamageFormula
    {
        private readonly List<Token> _rpn;
        private readonly string _expression;

        /// <summary>원본 수식 문자열입니다.</summary>
        public string Expression => _expression;

        private PolyDamageFormula(string expression, List<Token> rpn)
        {
            _expression = expression ?? string.Empty;
            _rpn = rpn ?? new List<Token>();
        }

        /// <summary>
        /// 수식 문자열을 계산 가능한 Poly 공식으로 컴파일합니다.
        /// </summary>
        /// <param name="expression">컴파일할 수식 문자열입니다.</param>
        /// <param name="formula">컴파일된 공식입니다.</param>
        /// <param name="error">컴파일 실패 원인입니다.</param>
        /// <returns>컴파일에 성공하면 <see langword="true"/>입니다.</returns>
        public static bool TryCompile(string expression, out PolyDamageFormula formula, out string error)
        {
            formula = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(expression))
            {
                error = "수식이 비어 있습니다.";
                return false;
            }

            if (!TryTokenize(expression, out List<Token> tokens, out error))
                return false;

            if (!TryBuildRpn(tokens, out List<Token> rpn, out error))
                return false;

            formula = new PolyDamageFormula(expression, rpn);
            return true;
        }

        /// <summary>
        /// 전달된 변수 값을 기준으로 수식을 계산합니다.
        /// </summary>
        /// <param name="variables">수식 변수 컨테이너입니다.</param>
        /// <returns>계산된 실수 값입니다.</returns>
        public double Evaluate(DamageFormulaVariableBag variables)
        {
            if (_rpn.Count == 0)
                return 0d;

            Stack<double> stack = new Stack<double>(_rpn.Count);
            for (int i = 0; i < _rpn.Count; i++)
            {
                Token token = _rpn[i];
                switch (token.Kind)
                {
                    case TokenKind.Number:
                        stack.Push(token.Number);
                        break;
                    case TokenKind.Variable:
                        stack.Push(variables != null && variables.TryGet(token.Text, out double value) ? value : 0d);
                        break;
                    case TokenKind.Operator:
                        ApplyOperator(token.Text, stack);
                        break;
                    case TokenKind.Function:
                        ApplyFunction(token.Text, stack);
                        break;
                }
            }

            return stack.Count > 0 ? Sanitize(stack.Pop()) : 0d;
        }

        /// <summary>
        /// 연산자 토큰을 스택에 적용합니다.
        /// </summary>
        private static void ApplyOperator(string op, Stack<double> stack)
        {
            double right = stack.Count > 0 ? stack.Pop() : 0d;
            double left = stack.Count > 0 ? stack.Pop() : 0d;
            double result = op switch
            {
                "+" => left + right,
                "-" => left - right,
                "*" => left * right,
                "/" => Math.Abs(right) <= double.Epsilon ? 0d : left / right,
                _ => 0d
            };
            stack.Push(Sanitize(result));
        }

        /// <summary>
        /// 지원 함수 토큰을 스택에 적용합니다.
        /// </summary>
        private static void ApplyFunction(string functionName, Stack<double> stack)
        {
            string name = functionName?.ToLowerInvariant() ?? string.Empty;
            switch (name)
            {
                case "min":
                {
                    double right = stack.Count > 0 ? stack.Pop() : 0d;
                    double left = stack.Count > 0 ? stack.Pop() : 0d;
                    stack.Push(Math.Min(left, right));
                    break;
                }
                case "max":
                {
                    double right = stack.Count > 0 ? stack.Pop() : 0d;
                    double left = stack.Count > 0 ? stack.Pop() : 0d;
                    stack.Push(Math.Max(left, right));
                    break;
                }
                case "clamp":
                {
                    double max = stack.Count > 0 ? stack.Pop() : 0d;
                    double min = stack.Count > 0 ? stack.Pop() : 0d;
                    double value = stack.Count > 0 ? stack.Pop() : 0d;
                    stack.Push(Math.Min(Math.Max(value, min), max));
                    break;
                }
                default:
                    stack.Push(0d);
                    break;
            }
        }

        /// <summary>
        /// 수식 문자열을 토큰 목록으로 분해합니다.
        /// </summary>
        private static bool TryTokenize(string expression, out List<Token> tokens, out string error)
        {
            tokens = new List<Token>();
            error = string.Empty;

            for (int i = 0; i < expression.Length;)
            {
                char c = expression[i];
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (char.IsDigit(c) || c == '.')
                {
                    int start = i;
                    while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
                        i++;

                    string numberText = expression.Substring(start, i - start);
                    if (!double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
                    {
                        error = $"숫자 토큰을 해석할 수 없습니다. token={numberText}";
                        return false;
                    }

                    tokens.Add(Token.CreateNumberToken(number));
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    int start = i;
                    while (i < expression.Length && (char.IsLetterOrDigit(expression[i]) || expression[i] == '_'))
                        i++;

                    string text = expression.Substring(start, i - start);
                    tokens.Add(Token.Variable(text));
                    continue;
                }

                if (c is '+' or '-' or '*' or '/')
                {
                    bool unaryMinus = c == '-' && (tokens.Count == 0 || tokens[^1].Kind == TokenKind.Operator || tokens[^1].Kind == TokenKind.LeftParen || tokens[^1].Kind == TokenKind.Comma);
                    if (unaryMinus)
                        tokens.Add(Token.CreateNumberToken(0d));

                    tokens.Add(Token.Operator(c.ToString()));
                    i++;
                    continue;
                }

                if (c == '(')
                {
                    tokens.Add(Token.LeftParen());
                    i++;
                    continue;
                }

                if (c == ')')
                {
                    tokens.Add(Token.RightParen());
                    i++;
                    continue;
                }

                if (c == ',')
                {
                    tokens.Add(Token.Comma());
                    i++;
                    continue;
                }

                error = $"지원하지 않는 문자입니다. char={c}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 토큰 목록을 Shunting-yard 방식으로 RPN 목록으로 변환합니다.
        /// </summary>
        private static bool TryBuildRpn(List<Token> tokens, out List<Token> rpn, out string error)
        {
            rpn = new List<Token>();
            error = string.Empty;
            Stack<Token> operators = new Stack<Token>();

            for (int i = 0; i < tokens.Count; i++)
            {
                Token token = tokens[i];
                if (token.Kind == TokenKind.Variable && i + 1 < tokens.Count && tokens[i + 1].Kind == TokenKind.LeftParen && IsSupportedFunction(token.Text))
                {
                    operators.Push(Token.Function(token.Text));
                    continue;
                }

                switch (token.Kind)
                {
                    case TokenKind.Number:
                    case TokenKind.Variable:
                        rpn.Add(token);
                        break;
                    case TokenKind.Comma:
                        while (operators.Count > 0 && operators.Peek().Kind != TokenKind.LeftParen)
                            rpn.Add(operators.Pop());
                        break;
                    case TokenKind.Operator:
                        while (operators.Count > 0 && operators.Peek().Kind == TokenKind.Operator && Precedence(operators.Peek().Text) >= Precedence(token.Text))
                            rpn.Add(operators.Pop());
                        operators.Push(token);
                        break;
                    case TokenKind.LeftParen:
                        operators.Push(token);
                        break;
                    case TokenKind.RightParen:
                        bool foundLeftParen = false;
                        while (operators.Count > 0)
                        {
                            Token popped = operators.Pop();
                            if (popped.Kind == TokenKind.LeftParen)
                            {
                                foundLeftParen = true;
                                break;
                            }
                            rpn.Add(popped);
                        }

                        if (!foundLeftParen)
                        {
                            error = "닫는 괄호에 대응하는 여는 괄호가 없습니다.";
                            return false;
                        }

                        if (operators.Count > 0 && operators.Peek().Kind == TokenKind.Function)
                            rpn.Add(operators.Pop());
                        break;
                }
            }

            while (operators.Count > 0)
            {
                Token popped = operators.Pop();
                if (popped.Kind == TokenKind.LeftParen || popped.Kind == TokenKind.RightParen)
                {
                    error = "괄호 짝이 맞지 않습니다.";
                    return false;
                }
                rpn.Add(popped);
            }

            return true;
        }

        /// <summary>
        /// 지원하는 함수 이름인지 확인합니다.
        /// </summary>
        private static bool IsSupportedFunction(string name)
        {
            return string.Equals(name, "min", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "max", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "clamp", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 연산자 우선순위를 반환합니다.
        /// </summary>
        private static int Precedence(string op)
        {
            return op is "*" or "/" ? 2 : 1;
        }

        /// <summary>
        /// 계산 불가능한 실수 값을 0으로 보정합니다.
        /// </summary>
        private static double Sanitize(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? 0d : value;
        }

        private enum TokenKind
        {
            Number,
            Variable,
            Operator,
            Function,
            LeftParen,
            RightParen,
            Comma
        }

        private readonly struct Token
        {
            public readonly TokenKind Kind;
            public readonly string Text;
            public readonly double Number;

            private Token(TokenKind kind, string text, double number)
            {
                Kind = kind;
                Text = text;
                Number = number;
            }

            public static Token CreateNumberToken(double value) => new(TokenKind.Number, string.Empty, value);
            public static Token Variable(string text) => new(TokenKind.Variable, text, 0d);
            public static Token Operator(string text) => new(TokenKind.Operator, text, 0d);
            public static Token Function(string text) => new(TokenKind.Function, text, 0d);
            public static Token LeftParen() => new(TokenKind.LeftParen, "(", 0d);
            public static Token RightParen() => new(TokenKind.RightParen, ")", 0d);
            public static Token Comma() => new(TokenKind.Comma, ",", 0d);
        }
    }
}
