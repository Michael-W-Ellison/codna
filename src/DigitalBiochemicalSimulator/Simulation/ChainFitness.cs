using DigitalBiochemicalSimulator.Core;

namespace DigitalBiochemicalSimulator.Simulation
{
    /// <summary>
    /// Fitness of a chain for fitness-weighted replication: rewards longer and more complete code.
    /// Score = token count, doubled if the chain ends a statement (";" or "}"), then multiplied by
    /// 1 + 0.5 for each kind of statement element present: declaration, assignment, control flow
    /// (if/while), block, return.
    /// </summary>
    public static class ChainFitness
    {
        public static double Calculate(TokenChain chain)
        {
            var tokens = chain.Tokens;
            if (tokens.Count == 0)
                return 0.0;

            double score = tokens.Count;

            var last = tokens[tokens.Count - 1].Type;
            if (last == TokenType.SEMICOLON || last == TokenType.BRACE_CLOSE)
                score *= 2.0;

            bool declaration = false, assignment = false, control = false, block = false, returns = false;
            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case TokenType.TYPE_INT:
                    case TokenType.TYPE_FLOAT:
                    case TokenType.TYPE_STRING:
                    case TokenType.TYPE_BOOL:
                    case TokenType.KEYWORD_VAR:
                    case TokenType.KEYWORD_LET:
                    case TokenType.KEYWORD_CONST:
                        declaration = true;
                        break;
                    case TokenType.OPERATOR_ASSIGN:
                    case TokenType.OPERATOR_PLUS_ASSIGN:
                    case TokenType.OPERATOR_MINUS_ASSIGN:
                    case TokenType.OPERATOR_MULTIPLY_ASSIGN:
                    case TokenType.OPERATOR_DIVIDE_ASSIGN:
                        assignment = true;
                        break;
                    case TokenType.KEYWORD_IF:
                    case TokenType.KEYWORD_WHILE:
                        control = true;
                        break;
                    case TokenType.BRACE_OPEN:
                        block = true;
                        break;
                    case TokenType.KEYWORD_RETURN:
                        returns = true;
                        break;
                }
            }

            int kinds = (declaration ? 1 : 0) + (assignment ? 1 : 0) + (control ? 1 : 0) + (block ? 1 : 0) + (returns ? 1 : 0);
            return score * (1.0 + 0.5 * kinds);
        }
    }
}
