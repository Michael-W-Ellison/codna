using System;
using System.Collections.Generic;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;

namespace DigitalBiochemicalSimulator.Utilities
{
    /// <summary>
    /// Factory for creating tokens with proper metadata and configuration.
    /// Based on section 2.2.4 of the design specification.
    /// </summary>
    public class TokenFactory
    {
        private TokenPool _tokenPool;
        private Random _random;
        private Dictionary<TokenType, TokenMetadata> _metadataTemplates;

        public TokenFactory(TokenPool tokenPool)
        {
            _tokenPool = tokenPool;
            _random = new Random();
            _metadataTemplates = new Dictionary<TokenType, TokenMetadata>();

            InitializeMetadataTemplates();
        }

        /// <summary>
        /// Creates a token with appropriate metadata
        /// </summary>
        public Token CreateToken(TokenType type, Vector3Int position, int initialEnergy = 50)
        {
            string value = GetDefaultValue(type);
            var token = _tokenPool.GetToken(type, value, position);

            token.Energy = initialEnergy;

            // Set metadata from template
            if (_metadataTemplates.TryGetValue(type, out var template))
            {
                token.Metadata = template.Clone();
            }
            else
            {
                token.Metadata = new TokenMetadata();
            }

            return token;
        }

        /// <summary>
        /// Gets the default string value for a token type
        /// </summary>
        public string GetDefaultValue(TokenType type)
        {
            return type switch
            {
                // Literals
                TokenType.INTEGER_LITERAL => _random.Next(0, 100).ToString(),
                TokenType.FLOAT_LITERAL => (_random.NextDouble() * 100).ToString("F2"),
                TokenType.STRING_LITERAL => $"\"str{_random.Next(0, 100)}\"",
                TokenType.BOOLEAN_LITERAL => _random.Next(0, 2) == 0 ? "false" : "true",

                // Keywords
                TokenType.KEYWORD_IF => "if",
                TokenType.KEYWORD_ELSE => "else",
                TokenType.KEYWORD_WHILE => "while",
                TokenType.KEYWORD_FOR => "for",
                TokenType.KEYWORD_FUNCTION => "function",
                TokenType.KEYWORD_CLASS => "class",
                TokenType.KEYWORD_RETURN => "return",
                TokenType.KEYWORD_VAR => "var",
                TokenType.KEYWORD_CONST => "const",
                TokenType.KEYWORD_LET => "let",
                TokenType.KEYWORD_NEW => "new",
                TokenType.KEYWORD_THIS => "this",
                TokenType.KEYWORD_TRUE => "true",
                TokenType.KEYWORD_FALSE => "false",
                TokenType.KEYWORD_NULL => "null",

                // Type Keywords
                TokenType.TYPE_INT => "int",
                TokenType.TYPE_FLOAT => "float",
                TokenType.TYPE_STRING => "string",
                TokenType.TYPE_BOOL => "bool",
                TokenType.TYPE_VOID => "void",
                TokenType.TYPE_OBJECT => "object",

                // Operators
                TokenType.OPERATOR_PLUS => "+",
                TokenType.OPERATOR_MINUS => "-",
                TokenType.OPERATOR_MULTIPLY => "*",
                TokenType.OPERATOR_DIVIDE => "/",
                TokenType.OPERATOR_MODULO => "%",
                TokenType.OPERATOR_POWER => "**",
                TokenType.OPERATOR_ASSIGN => "=",
                TokenType.OPERATOR_PLUS_ASSIGN => "+=",
                TokenType.OPERATOR_MINUS_ASSIGN => "-=",
                TokenType.OPERATOR_MULTIPLY_ASSIGN => "*=",
                TokenType.OPERATOR_DIVIDE_ASSIGN => "/=",
                TokenType.OPERATOR_EQUALS => "==",
                TokenType.OPERATOR_NOT_EQUALS => "!=",
                TokenType.OPERATOR_LESS_THAN => "<",
                TokenType.OPERATOR_GREATER_THAN => ">",
                TokenType.OPERATOR_LESS_THAN_OR_EQUAL => "<=",
                TokenType.OPERATOR_GREATER_THAN_OR_EQUAL => ">=",
                TokenType.OPERATOR_AND => "&&",
                TokenType.OPERATOR_OR => "||",
                TokenType.OPERATOR_NOT => "!",
                TokenType.OPERATOR_INCREMENT => "++",
                TokenType.OPERATOR_DECREMENT => "--",

                // Structural
                TokenType.PAREN_OPEN => "(",
                TokenType.PAREN_CLOSE => ")",
                TokenType.BRACE_OPEN => "{",
                TokenType.BRACE_CLOSE => "}",
                TokenType.BRACKET_OPEN => "[",
                TokenType.BRACKET_CLOSE => "]",

                // Punctuation
                TokenType.SEMICOLON => ";",
                TokenType.COMMA => ",",
                TokenType.DOT => ".",
                TokenType.COLON => ":",
                TokenType.QUESTION_MARK => "?",

                // Identifiers
                TokenType.IDENTIFIER => $"var{_random.Next(0, 100)}",

                _ => "?"
            };
        }

        /// <summary>
        /// Initializes metadata templates for all token types
        /// Based on section 3.4.2 electronegativity values
        /// </summary>
        private void InitializeMetadataTemplates()
        {
            // HIGH electronegativity (0.8-1.0): Type keywords, control keywords, assignment
            AddTemplate(TokenType.TYPE_INT, "type", "int", "type_keyword", 0.9f, 2);
            AddTemplate(TokenType.TYPE_FLOAT, "type", "float", "type_keyword", 0.9f, 2);
            AddTemplate(TokenType.TYPE_STRING, "type", "string", "type_keyword", 0.9f, 2);
            AddTemplate(TokenType.TYPE_BOOL, "type", "bool", "type_keyword", 0.9f, 2);

            AddTemplate(TokenType.KEYWORD_IF, "control", "conditional", "keyword", 0.85f, 2);
            AddTemplate(TokenType.KEYWORD_WHILE, "control", "loop", "keyword", 0.85f, 2);
            AddTemplate(TokenType.KEYWORD_FOR, "control", "loop", "keyword", 0.85f, 2);

            AddTemplate(TokenType.OPERATOR_ASSIGN, "operator", "assignment", "binary_operator", 0.8f, 2);

            // MEDIUM electronegativity (0.4-0.8): Arithmetic, comparison operators
            AddTemplate(TokenType.OPERATOR_PLUS, "operator", "arithmetic", "binary_operator", 0.6f, 2);
            AddTemplate(TokenType.OPERATOR_MINUS, "operator", "arithmetic", "binary_operator", 0.6f, 2);
            AddTemplate(TokenType.OPERATOR_MULTIPLY, "operator", "arithmetic", "binary_operator", 0.65f, 2);
            AddTemplate(TokenType.OPERATOR_DIVIDE, "operator", "arithmetic", "binary_operator", 0.65f, 2);

            AddTemplate(TokenType.OPERATOR_EQUALS, "operator", "comparison", "binary_operator", 0.7f, 2);
            AddTemplate(TokenType.OPERATOR_LESS_THAN, "operator", "comparison", "binary_operator", 0.7f, 2);
            AddTemplate(TokenType.OPERATOR_GREATER_THAN, "operator", "comparison", "binary_operator", 0.7f, 2);

            // LOW electronegativity (0.0-0.4): Literals, identifiers, punctuation
            AddTemplate(TokenType.INTEGER_LITERAL, "literal", "int", "operand", 0.3f, 2);
            AddTemplate(TokenType.FLOAT_LITERAL, "literal", "float", "operand", 0.3f, 2);
            AddTemplate(TokenType.STRING_LITERAL, "literal", "string", "operand", 0.3f, 2);
            AddTemplate(TokenType.BOOLEAN_LITERAL, "literal", "bool", "operand", 0.3f, 2);

            AddTemplate(TokenType.IDENTIFIER, "identifier", "variable", "operand", 0.2f, 2);

            AddTemplate(TokenType.SEMICOLON, "punctuation", "terminator", "separator", 0.1f, 1);
            AddTemplate(TokenType.COMMA, "punctuation", "separator", "separator", 0.1f, 2);

            // Structural tokens (medium-high for strong pairing)
            AddTemplate(TokenType.PAREN_OPEN, "structural", "grouping", "delimiter", 0.95f, 1);
            AddTemplate(TokenType.PAREN_CLOSE, "structural", "grouping", "delimiter", 0.95f, 1);
            AddTemplate(TokenType.BRACE_OPEN, "structural", "block", "delimiter", 0.95f, 1);
            AddTemplate(TokenType.BRACE_CLOSE, "structural", "block", "delimiter", 0.95f, 1);
        }

        private void AddTemplate(TokenType type, string syntaxCategory, string semanticType,
                                 string grammarRole, float electronegativity, int bondingCapacity)
        {
            _metadataTemplates[type] = new TokenMetadata(
                syntaxCategory, semanticType, grammarRole, electronegativity, bondingCapacity
            );
        }
    }
}
