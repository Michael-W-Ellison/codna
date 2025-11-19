using System.Collections.Generic;
using DigitalBiochemicalSimulator.Core;

namespace DigitalBiochemicalSimulator.Chemistry
{
    /// <summary>
    /// Electronegativity values for all token types.
    /// Based on section 3.4.2 of the design specification.
    ///
    /// Electronegativity represents a token's tendency to attract bonding partners:
    /// - HIGH (0.8-1.0): Type keywords, control structures, assignment
    /// - MEDIUM (0.4-0.8): Operators, comparison
    /// - LOW (0.0-0.4): Literals, identifiers, punctuation
    /// </summary>
    public static class ElectronegativityTable
    {
        private static readonly Dictionary<TokenType, float> _values = new Dictionary<TokenType, float>();

        static ElectronegativityTable()
        {
            InitializeValues();
        }

        /// <summary>
        /// Gets the electronegativity value for a token type
        /// </summary>
        public static float GetValue(TokenType type)
        {
            return _values.TryGetValue(type, out var value) ? value : 0.5f; // Default medium
        }

        /// <summary>
        /// Calculates bond strength based on electronegativity difference
        /// </summary>
        public static float CalculateBondStrength(TokenType type1, TokenType type2)
        {
            float en1 = GetValue(type1);
            float en2 = GetValue(type2);
            float difference = System.Math.Abs(en1 - en2);

            // Large difference = ionic (strong)
            // Small difference = covalent (medium)
            // Very small = van der waals (weak)
            return 1.0f - (difference * 0.5f); // Inverse relationship: small diff = stronger bond
        }

        /// <summary>
        /// Determines bond type based on electronegativity difference
        /// </summary>
        public static BondType DetermineBondType(TokenType type1, TokenType type2)
        {
            float en1 = GetValue(type1);
            float en2 = GetValue(type2);
            float difference = System.Math.Abs(en1 - en2);

            if (difference < 0.2f)
                return BondType.COVALENT; // Similar values = shared equally
            else if (difference < 0.5f)
                return BondType.IONIC; // Moderate difference = partial transfer
            else
                return BondType.VAN_DER_WAALS; // Large difference = weak interaction
        }

        private static void InitializeValues()
        {
            // ==========================================
            // HIGH ELECTRONEGATIVITY (0.8-1.0)
            // ==========================================
            // Type Keywords - strongly attract identifiers
            AddValue(TokenType.TYPE_INT, 0.90f);
            AddValue(TokenType.TYPE_FLOAT, 0.90f);
            AddValue(TokenType.TYPE_STRING, 0.90f);
            AddValue(TokenType.TYPE_BOOL, 0.90f);
            AddValue(TokenType.TYPE_VOID, 0.90f);
            AddValue(TokenType.TYPE_OBJECT, 0.90f);
            AddValue(TokenType.TYPE_ARRAY, 0.90f);
            AddValue(TokenType.TYPE_CHAR, 0.90f);
            AddValue(TokenType.TYPE_DOUBLE, 0.90f);
            AddValue(TokenType.TYPE_LONG, 0.90f);

            // Control Flow Keywords - strongly attract conditions and blocks
            AddValue(TokenType.KEYWORD_IF, 0.85f);
            AddValue(TokenType.KEYWORD_ELSE, 0.85f);
            AddValue(TokenType.KEYWORD_WHILE, 0.85f);
            AddValue(TokenType.KEYWORD_FOR, 0.85f);
            AddValue(TokenType.KEYWORD_SWITCH, 0.85f);
            AddValue(TokenType.KEYWORD_CASE, 0.85f);
            AddValue(TokenType.KEYWORD_DEFAULT, 0.85f);
            AddValue(TokenType.KEYWORD_BREAK, 0.80f);
            AddValue(TokenType.KEYWORD_CONTINUE, 0.80f);
            AddValue(TokenType.KEYWORD_RETURN, 0.80f);

            // Definition Keywords - strongly attract identifiers
            AddValue(TokenType.KEYWORD_FUNCTION, 0.88f);
            AddValue(TokenType.KEYWORD_CLASS, 0.88f);
            AddValue(TokenType.KEYWORD_INTERFACE, 0.88f);
            AddValue(TokenType.KEYWORD_ENUM, 0.88f);

            // Declaration Keywords
            AddValue(TokenType.KEYWORD_VAR, 0.82f);
            AddValue(TokenType.KEYWORD_CONST, 0.82f);
            AddValue(TokenType.KEYWORD_LET, 0.82f);

            // Assignment Operator - strongly attracts both operands
            AddValue(TokenType.OPERATOR_ASSIGN, 0.80f);
            AddValue(TokenType.OPERATOR_PLUS_ASSIGN, 0.78f);
            AddValue(TokenType.OPERATOR_MINUS_ASSIGN, 0.78f);
            AddValue(TokenType.OPERATOR_MULTIPLY_ASSIGN, 0.78f);
            AddValue(TokenType.OPERATOR_DIVIDE_ASSIGN, 0.78f);

            // ==========================================
            // MEDIUM-HIGH ELECTRONEGATIVITY (0.6-0.8)
            // ==========================================
            // Comparison Operators
            AddValue(TokenType.OPERATOR_EQUALS, 0.72f);
            AddValue(TokenType.OPERATOR_NOT_EQUALS, 0.72f);
            AddValue(TokenType.OPERATOR_LESS_THAN, 0.70f);
            AddValue(TokenType.OPERATOR_GREATER_THAN, 0.70f);
            AddValue(TokenType.OPERATOR_LESS_THAN_OR_EQUAL, 0.70f);
            AddValue(TokenType.OPERATOR_GREATER_THAN_OR_EQUAL, 0.70f);

            // Logical Operators
            AddValue(TokenType.OPERATOR_AND, 0.68f);
            AddValue(TokenType.OPERATOR_OR, 0.68f);
            AddValue(TokenType.OPERATOR_NOT, 0.65f);

            // ==========================================
            // MEDIUM ELECTRONEGATIVITY (0.4-0.6)
            // ==========================================
            // Arithmetic Operators
            AddValue(TokenType.OPERATOR_PLUS, 0.60f);
            AddValue(TokenType.OPERATOR_MINUS, 0.60f);
            AddValue(TokenType.OPERATOR_MULTIPLY, 0.65f); // Higher precedence
            AddValue(TokenType.OPERATOR_DIVIDE, 0.65f);   // Higher precedence
            AddValue(TokenType.OPERATOR_MODULO, 0.65f);
            AddValue(TokenType.OPERATOR_POWER, 0.70f);    // Highest precedence

            // Unary Operators
            AddValue(TokenType.OPERATOR_INCREMENT, 0.55f);
            AddValue(TokenType.OPERATOR_DECREMENT, 0.55f);

            // Access Operators
            AddValue(TokenType.DOT, 0.75f); // Member access is strong
            AddValue(TokenType.ARROW, 0.75f);

            // Other Keywords
            AddValue(TokenType.KEYWORD_NEW, 0.65f);
            AddValue(TokenType.KEYWORD_THIS, 0.60f);
            AddValue(TokenType.KEYWORD_SUPER, 0.60f);
            AddValue(TokenType.KEYWORD_STATIC, 0.55f);
            AddValue(TokenType.KEYWORD_PUBLIC, 0.55f);
            AddValue(TokenType.KEYWORD_PRIVATE, 0.55f);
            AddValue(TokenType.KEYWORD_PROTECTED, 0.55f);
            AddValue(TokenType.KEYWORD_ABSTRACT, 0.55f);
            AddValue(TokenType.KEYWORD_FINAL, 0.55f);

            // ==========================================
            // LOW-MEDIUM ELECTRONEGATIVITY (0.2-0.4)
            // ==========================================
            // Literals - relatively inert, bond weakly
            AddValue(TokenType.INTEGER_LITERAL, 0.30f);
            AddValue(TokenType.FLOAT_LITERAL, 0.30f);
            AddValue(TokenType.STRING_LITERAL, 0.30f);
            AddValue(TokenType.BOOLEAN_LITERAL, 0.30f);
            AddValue(TokenType.CHAR_LITERAL, 0.30f);
            AddValue(TokenType.KEYWORD_NULL, 0.25f);
            AddValue(TokenType.KEYWORD_TRUE, 0.30f);
            AddValue(TokenType.KEYWORD_FALSE, 0.30f);

            // Identifiers - low electronegativity, easily bonded
            AddValue(TokenType.IDENTIFIER, 0.20f);

            // ==========================================
            // LOW ELECTRONEGATIVITY (0.0-0.2)
            // ==========================================
            // Punctuation - weak bonding
            AddValue(TokenType.SEMICOLON, 0.10f);
            AddValue(TokenType.COMMA, 0.15f);
            AddValue(TokenType.COLON, 0.15f);
            AddValue(TokenType.QUESTION_MARK, 0.50f); // Ternary operator

            // Comments (if tokens)
            AddValue(TokenType.COMMENT_LINE, 0.05f);
            AddValue(TokenType.COMMENT_BLOCK, 0.05f);

            // ==========================================
            // VERY HIGH ELECTRONEGATIVITY (0.9-1.0)
            // ==========================================
            // Structural delimiters - MUST pair strongly
            AddValue(TokenType.PAREN_OPEN, 0.95f);
            AddValue(TokenType.PAREN_CLOSE, 0.95f);
            AddValue(TokenType.BRACE_OPEN, 0.95f);
            AddValue(TokenType.BRACE_CLOSE, 0.95f);
            AddValue(TokenType.BRACKET_OPEN, 0.95f);
            AddValue(TokenType.BRACKET_CLOSE, 0.95f);

            // Exception Handling - high importance
            AddValue(TokenType.KEYWORD_TRY, 0.85f);
            AddValue(TokenType.KEYWORD_CATCH, 0.85f);
            AddValue(TokenType.KEYWORD_FINALLY, 0.85f);
            AddValue(TokenType.KEYWORD_THROW, 0.80f);

            // Async/Concurrency - high importance
            AddValue(TokenType.KEYWORD_ASYNC, 0.75f);
            AddValue(TokenType.KEYWORD_AWAIT, 0.75f);
            AddValue(TokenType.KEYWORD_YIELD, 0.70f);

            // Import/Export - high importance
            AddValue(TokenType.KEYWORD_IMPORT, 0.80f);
            AddValue(TokenType.KEYWORD_EXPORT, 0.80f);
            AddValue(TokenType.KEYWORD_FROM, 0.75f);

            // Special
            AddValue(TokenType.UNKNOWN, 0.01f);
            AddValue(TokenType.WHITESPACE, 0.01f);
            AddValue(TokenType.NEWLINE, 0.01f);
            AddValue(TokenType.EOF, 0.01f);
        }

        private static void AddValue(TokenType type, float value)
        {
            _values[type] = value;
        }

        /// <summary>
        /// Gets all electronegativity values for debugging/visualization
        /// </summary>
        public static Dictionary<TokenType, float> GetAllValues()
        {
            return new Dictionary<TokenType, float>(_values);
        }
    }
}
