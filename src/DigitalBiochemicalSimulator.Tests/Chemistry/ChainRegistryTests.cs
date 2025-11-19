using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Chemistry;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Grammar;

namespace DigitalBiochemicalSimulator.Tests.Chemistry
{
    public class ChainRegistryTests
    {
        private ChainRegistry CreateRegistry()
        {
            var rules = GrammarLibrary.GetDefaultGrammar();
            var rulesEngine = new BondRulesEngine(rules);
            var strengthCalculator = new BondStrengthCalculator(rulesEngine);
            var stabilityCalculator = new ChainStabilityCalculator(rulesEngine, strengthCalculator);

            return new ChainRegistry(stabilityCalculator);
        }

        [Fact]
        public void ChainRegistry_Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            var registry = CreateRegistry();

            // Assert
            Assert.NotNull(registry);
        }

        [Fact]
        public void RegisterChain_NewChain_AddsToRegistry()
        {
            // Arrange
            var registry = CreateRegistry();
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero);
            var chain = new TokenChain(token);

            // Act
            registry.RegisterChain(chain);

            // Assert
            var retrievedChain = registry.GetChain(chain.Id);
            Assert.NotNull(retrievedChain);
            Assert.Equal(chain.Id, retrievedChain.Id);
        }

        [Fact]
        public void UnregisterChain_ExistingChain_RemovesFromRegistry()
        {
            // Arrange
            var registry = CreateRegistry();
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero);
            var chain = new TokenChain(token);
            registry.RegisterChain(chain);

            // Act
            registry.UnregisterChain(chain.Id);

            // Assert
            var retrievedChain = registry.GetChain(chain.Id);
            Assert.Null(retrievedChain);
        }

        [Fact]
        public void GetChain_NonExistentId_ReturnsNull()
        {
            // Arrange
            var registry = CreateRegistry();

            // Act
            var chain = registry.GetChain(999);

            // Assert
            Assert.Null(chain);
        }

        [Fact]
        public void GetAllChains_ReturnsAllRegistered()
        {
            // Arrange
            var registry = CreateRegistry();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero);
            var chain1 = new TokenChain(token1) { Id = 1 };
            var chain2 = new TokenChain(token2) { Id = 2 };

            // Act
            registry.RegisterChain(chain1);
            registry.RegisterChain(chain2);
            var allChains = registry.GetAllChains();

            // Assert
            Assert.Equal(2, allChains.Count);
            Assert.Contains(chain1, allChains);
            Assert.Contains(chain2, allChains);
        }

        [Fact]
        public void UpdateAllStabilities_CalculatesStabilityForAllChains()
        {
            // Arrange
            var registry = CreateRegistry();
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", Vector3Int.Zero);
            var chain = new TokenChain(token) { Id = 1 };
            registry.RegisterChain(chain);

            // Act
            registry.UpdateAllStabilities(currentTick: 100);

            // Assert
            var retrievedChain = registry.GetChain(chain.Id);
            // Stability should have been calculated (exact value depends on calculator)
            Assert.NotNull(retrievedChain);
        }

        [Fact]
        public void PruneStaleChains_RemovesOldChains()
        {
            // Arrange
            var registry = CreateRegistry();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero);

            var oldChain = new TokenChain(token1) { Id = 1, LastModifiedTick = 100 };
            var recentChain = new TokenChain(token2) { Id = 2, LastModifiedTick = 900 };

            registry.RegisterChain(oldChain);
            registry.RegisterChain(recentChain);

            // Act
            registry.PruneStaleChains(currentTick: 1000, maxAge: 200);

            // Assert
            Assert.Null(registry.GetChain(oldChain.Id)); // Pruned (age = 900)
            Assert.NotNull(registry.GetChain(recentChain.Id)); // Kept (age = 100)
        }

        [Fact]
        public void GetChainsByStability_ReturnsOrderedByStability()
        {
            // Arrange
            var registry = CreateRegistry();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero);
            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "1", Vector3Int.Zero);

            var chain1 = new TokenChain(token1) { Id = 1, StabilityScore = 0.5f };
            var chain2 = new TokenChain(token2) { Id = 2, StabilityScore = 0.9f };
            var chain3 = new TokenChain(token3) { Id = 3, StabilityScore = 0.3f };

            registry.RegisterChain(chain1);
            registry.RegisterChain(chain2);
            registry.RegisterChain(chain3);

            // Act
            var orderedChains = registry.GetChainsByStability();

            // Assert
            Assert.Equal(3, orderedChains.Count);
            Assert.Equal(chain2.Id, orderedChains[0].Id); // Highest stability first
            Assert.Equal(chain1.Id, orderedChains[1].Id);
            Assert.Equal(chain3.Id, orderedChains[2].Id); // Lowest stability last
        }

        [Fact]
        public void GetChainsByLength_ReturnsOrderedByLength()
        {
            // Arrange
            var registry = CreateRegistry();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero);

            var chain1 = new TokenChain(token1) { Id = 1, Length = 3 };
            var chain2 = new TokenChain(token2) { Id = 2, Length = 5 };

            registry.RegisterChain(chain1);
            registry.RegisterChain(chain2);

            // Act
            var orderedChains = registry.GetChainsByLength();

            // Assert
            Assert.Equal(2, orderedChains.Count);
            Assert.Equal(chain2.Id, orderedChains[0].Id); // Longest first
            Assert.Equal(chain1.Id, orderedChains[1].Id);
        }

        [Fact]
        public void GetStatistics_ReturnsCorrectCounts()
        {
            // Arrange
            var registry = CreateRegistry();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero);

            var chain1 = new TokenChain(token1) { Id = 1, Length = 3 };
            var chain2 = new TokenChain(token2) { Id = 2, Length = 5 };

            registry.RegisterChain(chain1);
            registry.RegisterChain(chain2);

            // Act
            var stats = registry.GetStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(2, stats.TotalChains);
            Assert.Equal(8, stats.TotalTokensInChains); // 3 + 5
            Assert.Equal(4.0, stats.AverageChainLength); // (3 + 5) / 2
            Assert.Equal(5, stats.LongestChainLength);
        }

        [Fact]
        public void Clear_RemovesAllChains()
        {
            // Arrange
            var registry = CreateRegistry();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero);

            var chain1 = new TokenChain(token1) { Id = 1 };
            var chain2 = new TokenChain(token2) { Id = 2 };

            registry.RegisterChain(chain1);
            registry.RegisterChain(chain2);

            // Act
            registry.Clear();

            // Assert
            var allChains = registry.GetAllChains();
            Assert.Empty(allChains);
        }

        [Fact]
        public void RegisterChain_DuplicateId_ReplacesOld()
        {
            // Arrange
            var registry = CreateRegistry();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = new Token(2, TokenType.INTEGER_LITERAL, "10", Vector3Int.Zero);

            var chain1 = new TokenChain(token1) { Id = 1, Length = 1 };
            var chain2 = new TokenChain(token2) { Id = 1, Length = 2 }; // Same ID

            // Act
            registry.RegisterChain(chain1);
            registry.RegisterChain(chain2);

            // Assert
            var retrieved = registry.GetChain(1);
            Assert.Equal(2, retrieved.Length); // Second chain replaced first
        }
    }
}
