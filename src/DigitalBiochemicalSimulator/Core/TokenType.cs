namespace DigitalBiochemicalSimulator.Core
{
    /// <summary>
    /// Defines all token types that can exist in the simulation.
    /// Based on section 3.1.2 of the design specification.
    /// </summary>
    public enum TokenType
    {
        // Literals
        INTEGER_LITERAL,
        FLOAT_LITERAL,
        STRING_LITERAL,
        BOOLEAN_LITERAL,

        // Keywords
        KEYWORD_IF,
        KEYWORD_ELSE,
        KEYWORD_WHILE,
        KEYWORD_FOR,
        KEYWORD_FUNCTION,
        KEYWORD_CLASS,
        KEYWORD_RETURN,
        KEYWORD_VAR,
        KEYWORD_CONST,
        KEYWORD_LET,
        KEYWORD_NEW,
        KEYWORD_THIS,
        KEYWORD_TRUE,
        KEYWORD_FALSE,
        KEYWORD_NULL,

        // Type Keywords
        TYPE_INT,
        TYPE_FLOAT,
        TYPE_STRING,
        TYPE_BOOL,
        TYPE_VOID,
        TYPE_OBJECT,

        // Operators - Arithmetic
        OPERATOR_PLUS,
        OPERATOR_MINUS,
        OPERATOR_MULTIPLY,
        OPERATOR_DIVIDE,
        OPERATOR_MODULO,
        OPERATOR_POWER,

        // Operators - Assignment
        OPERATOR_ASSIGN,
        OPERATOR_PLUS_ASSIGN,
        OPERATOR_MINUS_ASSIGN,
        OPERATOR_MULTIPLY_ASSIGN,
        OPERATOR_DIVIDE_ASSIGN,

        // Operators - Comparison
        OPERATOR_EQUALS,
        OPERATOR_NOT_EQUALS,
        OPERATOR_LESS_THAN,
        OPERATOR_GREATER_THAN,
        OPERATOR_LESS_THAN_OR_EQUAL,
        OPERATOR_GREATER_THAN_OR_EQUAL,

        // Operators - Logical
        OPERATOR_AND,
        OPERATOR_OR,
        OPERATOR_NOT,

        // Operators - Unary
        OPERATOR_INCREMENT,
        OPERATOR_DECREMENT,

        // Structural - Parentheses
        PAREN_OPEN,
        PAREN_CLOSE,

        // Structural - Braces
        BRACE_OPEN,
        BRACE_CLOSE,

        // Structural - Brackets
        BRACKET_OPEN,
        BRACKET_CLOSE,

        // Punctuation
        SEMICOLON,
        COMMA,
        DOT,
        COLON,
        QUESTION_MARK,

        // Identifiers
        IDENTIFIER,

        // Comments
        COMMENT_SINGLE,
        COMMENT_MULTI_START,
        COMMENT_MULTI_END,

        // Special
        WHITESPACE,
        NEWLINE,
        EOF
    }
}
