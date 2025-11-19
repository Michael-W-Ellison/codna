using System.Collections.Generic;
using DigitalBiochemicalSimulator.Core;

namespace DigitalBiochemicalSimulator.Grammar
{
    /// <summary>
    /// Library of predefined grammar rules for common programming constructs.
    /// Based on section 3.5.2 of the design specification.
    /// </summary>
    public static class GrammarLibrary
    {
        /// <summary>
        /// Gets a simple arithmetic expression grammar
        /// </summary>
        public static List<GrammarRule> GetArithmeticGrammar()
        {
            var rules = new List<GrammarRule>();

            // Binary Expression: operand operator operand
            var binaryExpr = new GrammarRule("binary_expression", "Binary Expression");
            binaryExpr.Pattern.Add(new TokenPattern(new List<TokenType>
            {
                TokenType.INTEGER_LITERAL,
                TokenType.FLOAT_LITERAL,
                TokenType.IDENTIFIER
            }, Quantifier.ONE));
            binaryExpr.Pattern.Add(new TokenPattern(new List<TokenType>
            {
                TokenType.OPERATOR_PLUS,
                TokenType.OPERATOR_MINUS,
                TokenType.OPERATOR_MULTIPLY,
                TokenType.OPERATOR_DIVIDE
            }, Quantifier.ONE));
            binaryExpr.Pattern.Add(new TokenPattern(new List<TokenType>
            {
                TokenType.INTEGER_LITERAL,
                TokenType.FLOAT_LITERAL,
                TokenType.IDENTIFIER
            }, Quantifier.ONE));
            binaryExpr.BondType = BondType.IONIC;
            binaryExpr.BondStrength = 0.75f;
            binaryExpr.ValidationLevel = ValidationLevel.DELAYED;
            rules.Add(binaryExpr);

            // Parenthesized Expression: ( expression )
            var parenExpr = new GrammarRule("paren_expression", "Parenthesized Expression");
            parenExpr.Pattern.Add(new TokenPattern(TokenType.PAREN_OPEN, Quantifier.ONE));
            parenExpr.Pattern.Add(new TokenPattern(new List<TokenType>
            {
                TokenType.INTEGER_LITERAL,
                TokenType.IDENTIFIER
            }, Quantifier.ONE_OR_MORE));
            parenExpr.Pattern.Add(new TokenPattern(TokenType.PAREN_CLOSE, Quantifier.ONE));
            parenExpr.BondType = BondType.COVALENT;
            parenExpr.BondStrength = 0.95f;
            parenExpr.ValidationLevel = ValidationLevel.IMMEDIATE;
            rules.Add(parenExpr);

            // Assignment: identifier = expression
            var assignment = new GrammarRule("assignment", "Assignment");
            assignment.Pattern.Add(new TokenPattern(TokenType.IDENTIFIER, Quantifier.ONE));
            assignment.Pattern.Add(new TokenPattern(TokenType.OPERATOR_ASSIGN, Quantifier.ONE));
            assignment.Pattern.Add(new TokenPattern(new List<TokenType>
            {
                TokenType.INTEGER_LITERAL,
                TokenType.FLOAT_LITERAL,
                TokenType.IDENTIFIER
            }, Quantifier.ONE));
            assignment.BondType = BondType.COVALENT;
            assignment.BondStrength = 0.90f;
            assignment.ValidationLevel = ValidationLevel.DELAYED;
            rules.Add(assignment);

            // Variable Declaration: type identifier
            var varDecl = new GrammarRule("variable_declaration", "Variable Declaration");
            varDecl.Pattern.Add(new TokenPattern(new List<TokenType>
            {
                TokenType.TYPE_INT,
                TokenType.TYPE_FLOAT,
                TokenType.TYPE_STRING,
                TokenType.TYPE_BOOL
            }, Quantifier.ONE));
            varDecl.Pattern.Add(new TokenPattern(TokenType.IDENTIFIER, Quantifier.ONE));
            varDecl.BondType = BondType.COVALENT;
            varDecl.BondStrength = 0.90f;
            varDecl.ValidationLevel = ValidationLevel.IMMEDIATE;
            rules.Add(varDecl);

            // Statement Termination: statement ;
            var terminator = new GrammarRule("statement_terminator", "Statement Terminator");
            terminator.Pattern.Add(new TokenPattern(new List<TokenType>
            {
                TokenType.INTEGER_LITERAL,
                TokenType.IDENTIFIER
            }, Quantifier.ONE));
            terminator.Pattern.Add(new TokenPattern(TokenType.SEMICOLON, Quantifier.ONE));
            terminator.BondType = BondType.VAN_DER_WAALS;
            terminator.BondStrength = 0.4f;
            terminator.ValidationLevel = ValidationLevel.DEFERRED;
            rules.Add(terminator);

            return rules;
        }

        /// <summary>
        /// Gets grammar for control flow structures
        /// </summary>
        public static List<GrammarRule> GetControlFlowGrammar()
        {
            var rules = new List<GrammarRule>();

            // If Statement: if ( condition ) { }
            var ifStmt = new GrammarRule("if_statement", "If Statement");
            ifStmt.Pattern.Add(new TokenPattern(TokenType.KEYWORD_IF, Quantifier.ONE));
            ifStmt.Pattern.Add(new TokenPattern(TokenType.PAREN_OPEN, Quantifier.ONE));
            ifStmt.Pattern.Add(new TokenPattern(TokenType.IDENTIFIER, Quantifier.ONE)); // condition
            ifStmt.Pattern.Add(new TokenPattern(TokenType.PAREN_CLOSE, Quantifier.ONE));
            ifStmt.Pattern.Add(new TokenPattern(TokenType.BRACE_OPEN, Quantifier.ONE));
            ifStmt.BondType = BondType.COVALENT;
            ifStmt.BondStrength = 0.85f;
            ifStmt.ValidationLevel = ValidationLevel.IMMEDIATE;
            rules.Add(ifStmt);

            // While Loop: while ( condition ) { }
            var whileLoop = new GrammarRule("while_loop", "While Loop");
            whileLoop.Pattern.Add(new TokenPattern(TokenType.KEYWORD_WHILE, Quantifier.ONE));
            whileLoop.Pattern.Add(new TokenPattern(TokenType.PAREN_OPEN, Quantifier.ONE));
            whileLoop.Pattern.Add(new TokenPattern(TokenType.IDENTIFIER, Quantifier.ONE));
            whileLoop.Pattern.Add(new TokenPattern(TokenType.PAREN_CLOSE, Quantifier.ONE));
            whileLoop.Pattern.Add(new TokenPattern(TokenType.BRACE_OPEN, Quantifier.ONE));
            whileLoop.BondType = BondType.COVALENT;
            whileLoop.BondStrength = 0.85f;
            whileLoop.ValidationLevel = ValidationLevel.IMMEDIATE;
            rules.Add(whileLoop);

            return rules;
        }

        /// <summary>
        /// Gets the complete default grammar (arithmetic + control flow)
        /// </summary>
        public static List<GrammarRule> GetDefaultGrammar()
        {
            var rules = new List<GrammarRule>();
            rules.AddRange(GetArithmeticGrammar());
            rules.AddRange(GetControlFlowGrammar());
            return rules;
        }
    }
}
