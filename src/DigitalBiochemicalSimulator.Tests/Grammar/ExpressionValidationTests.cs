using System.Collections.Generic;
using System.Linq;
using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Grammar;
using DigitalBiochemicalSimulator.Tests.TestHelpers;

namespace DigitalBiochemicalSimulator.Tests.Grammar
{
    /// <summary>
    /// The syntax check (TokenChain.ValidateChain) and semantic check (SemanticValidator) used to
    /// select chains, on space-separated code samples
    /// </summary>
    public class ExpressionValidationTests
    {
        private static readonly Dictionary<string, TokenType> Symbols = new()
        {
            ["+"] = TokenType.OPERATOR_PLUS, ["-"] = TokenType.OPERATOR_MINUS,
            ["*"] = TokenType.OPERATOR_MULTIPLY, ["/"] = TokenType.OPERATOR_DIVIDE,
            ["%"] = TokenType.OPERATOR_MODULO, ["="] = TokenType.OPERATOR_ASSIGN,
            ["=="] = TokenType.OPERATOR_EQUALS, ["<"] = TokenType.OPERATOR_LESS_THAN,
            [">"] = TokenType.OPERATOR_GREATER_THAN, ["&&"] = TokenType.OPERATOR_AND,
            ["!"] = TokenType.OPERATOR_NOT, ["("] = TokenType.PAREN_OPEN, [")"] = TokenType.PAREN_CLOSE,
            ["{"] = TokenType.BRACE_OPEN, ["}"] = TokenType.BRACE_CLOSE, [";"] = TokenType.SEMICOLON,
            ["if"] = TokenType.KEYWORD_IF, ["while"] = TokenType.KEYWORD_WHILE,
            ["return"] = TokenType.KEYWORD_RETURN, ["int"] = TokenType.TYPE_INT,
            ["true"] = TokenType.KEYWORD_TRUE, ["string"] = TokenType.TYPE_STRING,
            ["float"] = TokenType.TYPE_FLOAT
        };

        private static TokenChain Parse(string code)
        {
            var tokens = code.Split(' ').Select((text, i) =>
            {
                var type = Symbols.TryGetValue(text, out var symbol) ? symbol
                    : char.IsDigit(text[0]) ? TokenType.INTEGER_LITERAL
                    : TokenType.IDENTIFIER;
                return new Token(i + 1, type, text, Vector3Int.Zero);
            }).ToArray();

            return TestChains.Build(tokens);
        }

        [Theory]
        [InlineData("5 + 3")]
        [InlineData("x = 5 ;")]
        [InlineData("int x = 5 ;")]
        [InlineData("a < b && c > d")]
        [InlineData("( 5 + 3 ) * 2")]
        [InlineData("! x")]
        [InlineData("return x ;")]
        [InlineData("while ( 3 / x - y )")]
        [InlineData("if ( x > 3 ) { y = 1 ; }")]
        [InlineData("float x = 5 ; x = x * 2 ;")]
        [InlineData("int x ;")]
        public void ValidCode_PassesSyntaxAndSemantics(string code)
        {
            var chain = Parse(code);

            Assert.True(chain.ValidateChain().IsValid, "syntax");
            Assert.True(new SemanticValidator().ValidateChain(chain).IsValid, "semantics");
        }

        [Theory]
        [InlineData("5 +")]
        [InlineData("var13 + var73 / var0 - var88 /")]
        [InlineData("if 5")]
        [InlineData("if (")]
        [InlineData("5 3")]
        [InlineData("( )")]
        [InlineData("int 5")]
        [InlineData("x = ;")]
        [InlineData("x ; ;")]
        [InlineData("{ x ; }")]
        [InlineData("int x - 73 * y")]                    // declaration not followed by = or ;
        [InlineData("float a * 58 - b = c ;")]           // assignment with an expression on its left
        [InlineData("x = 84 * y = z ;")]                  // second assignment mid-expression
        [InlineData("x + if ( y )")]                      // if must start a statement
        [InlineData("x * return y")]                      // return must start a statement
        [InlineData("5 = x")]                             // assignment to a literal
        public void MalformedCode_FailsSyntax(string code)
        {
            Assert.False(Parse(code).ValidateChain().IsValid);
        }

        [Theory]
        [InlineData("33 * 47 / 0 / 73")]
        [InlineData("x % 0")]
        [InlineData("5 + true")]
        [InlineData("string x = 5 ;")]
        [InlineData("int x = true ;")]
        public void MeaninglessCode_PassesSyntaxButFailsSemantics(string code)
        {
            var chain = Parse(code);

            Assert.True(chain.ValidateChain().IsValid, "syntax");
            Assert.False(new SemanticValidator().ValidateChain(chain).IsValid, "semantics");
        }
    }
}
