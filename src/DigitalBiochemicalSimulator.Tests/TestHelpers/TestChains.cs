using DigitalBiochemicalSimulator.Core;

namespace DigitalBiochemicalSimulator.Tests.TestHelpers
{
    /// <summary>
    /// Builds token chains for tests
    /// </summary>
    public static class TestChains
    {
        /// <summary>
        /// Bonds each token to the next and returns a chain of the tokens in order
        /// (TokenChain.AddToken only accepts tokens bonded to the chain end)
        /// </summary>
        public static TokenChain Build(params Token[] tokens)
        {
            var chain = new TokenChain(tokens[0]);

            for (int i = 1; i < tokens.Length; i++)
            {
                tokens[i - 1].BondedTokens.Add(tokens[i]);
                tokens[i].BondedTokens.Add(tokens[i - 1]);
                chain.AddToken(tokens[i], atTail: true);
            }

            return chain;
        }
    }
}
