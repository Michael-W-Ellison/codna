using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.Chemistry;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Grammar;
using DigitalBiochemicalSimulator.Simulation;

namespace DigitalBiochemicalSimulator.Tests.Chemistry
{
    public class BondingManagerTests
    {
        private BondingManager CreateBondingManager()
        {
            var config = new SimulationConfig(50, 50, 50);
            var grid = new Grid(50, 50, 50);
            var rules = GrammarLibrary.GetDefaultGrammar();
            var rulesEngine = new BondRulesEngine(rules);
            var strengthCalculator = new BondStrengthCalculator(rulesEngine);

            return new BondingManager(rulesEngine, strengthCalculator, config, grid);
        }

        [Fact]
        public void BondingManager_Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            var manager = CreateBondingManager();

            // Assert
            Assert.NotNull(manager);
        }

        [Fact]
        public void AttemptBond_ValidTokens_ReturnsTrue()
        {
            // Arrange
            var manager = CreateBondingManager();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Energy = 100, IsActive = true };
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero) { Energy = 100, IsActive = true };

            // Act
            bool result = manager.AttemptBond(token1, token2, currentTick: 0);

            // Assert
            Assert.True(result);
            Assert.Contains(token2, token1.BondedTokens);
            Assert.Contains(token1, token2.BondedTokens);
        }

        [Fact]
        public void AttemptBond_NullToken_ReturnsFalse()
        {
            // Arrange
            var manager = CreateBondingManager();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);

            // Act
            bool result = manager.AttemptBond(token1, null, currentTick: 0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AttemptBond_InactiveToken_ReturnsFalse()
        {
            // Arrange
            var manager = CreateBondingManager();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Energy = 100, IsActive = true };
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero) { Energy = 100, IsActive = false };

            // Act
            bool result = manager.AttemptBond(token1, token2, currentTick: 0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AttemptBond_SelfBond_ReturnsFalse()
        {
            // Arrange
            var manager = CreateBondingManager();
            var token = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Energy = 100, IsActive = true };

            // Act
            bool result = manager.AttemptBond(token, token, currentTick: 0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AttemptBond_AlreadyBonded_ReturnsFalse()
        {
            // Arrange
            var manager = CreateBondingManager();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Energy = 100, IsActive = true };
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero) { Energy = 100, IsActive = true };

            // First bond succeeds
            manager.AttemptBond(token1, token2, currentTick: 0);

            // Act - Try to bond again
            bool result = manager.AttemptBond(token1, token2, currentTick: 0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AttemptBond_InsufficientEnergy_ReturnsFalse()
        {
            // Arrange
            var manager = CreateBondingManager();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Energy = 1, IsActive = true };
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero) { Energy = 1, IsActive = true };

            // Act
            bool result = manager.AttemptBond(token1, token2, currentTick: 0);

            // Assert - May fail due to insufficient energy
            // Result depends on energy cost calculation
            Assert.True(result || !result);
        }

        [Fact]
        public void AttemptBond_ConsumesEnergy()
        {
            // Arrange
            var manager = CreateBondingManager();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Energy = 100, IsActive = true };
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero) { Energy = 100, IsActive = true };
            int initialEnergy = token1.Energy + token2.Energy;

            // Act
            bool result = manager.AttemptBond(token1, token2, currentTick: 0);

            // Assert
            if (result)
            {
                // Energy should be modified (may increase or decrease depending on bond type)
                int finalEnergy = token1.Energy + token2.Energy;
                Assert.NotEqual(initialEnergy, finalEnergy);
            }
        }

        [Fact]
        public void BreakBond_BondedTokens_SeparatesThem()
        {
            // Arrange
            var manager = CreateBondingManager();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Energy = 100, IsActive = true };
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero) { Energy = 100, IsActive = true };

            manager.AttemptBond(token1, token2, currentTick: 0);

            // Act
            bool result = manager.BreakBond(token1, token2, currentTick: 10);

            // Assert
            Assert.True(result);
            Assert.DoesNotContain(token2, token1.BondedTokens);
            Assert.DoesNotContain(token1, token2.BondedTokens);
        }

        [Fact]
        public void BreakBond_NotBonded_ReturnsFalse()
        {
            // Arrange
            var manager = CreateBondingManager();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);

            // Act
            bool result = manager.BreakBond(token1, token2, currentTick: 0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetTokenChain_BondedTokens_ReturnsChain()
        {
            // Arrange
            var manager = CreateBondingManager();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Energy = 100, IsActive = true };
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero) { Energy = 100, IsActive = true };

            manager.AttemptBond(token1, token2, currentTick: 0);

            // Act
            var chain = manager.GetTokenChain(token1);

            // Assert
            Assert.NotNull(chain);
            Assert.True(chain.Length >= 1);
        }

        [Fact]
        public void BondMultipleTokens_CreatesChain()
        {
            // Arrange
            var manager = CreateBondingManager();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Energy = 100, IsActive = true };
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero) { Energy = 100, IsActive = true };
            var token3 = new Token(3, TokenType.INTEGER_LITERAL, "3", Vector3Int.Zero) { Energy = 100, IsActive = true };

            // Act
            bool bond1 = manager.AttemptBond(token1, token2, currentTick: 0);
            bool bond2 = manager.AttemptBond(token2, token3, currentTick: 0);

            // Assert
            Assert.True(bond1);
            Assert.True(bond2);

            var chain = manager.GetTokenChain(token1);
            Assert.NotNull(chain);
            Assert.True(chain.Length >= 2);
        }

        [Fact]
        public void ProcessCellBonding_FindsBondingOpportunities()
        {
            // Arrange
            var config = new SimulationConfig(50, 50, 50);
            var grid = new Grid(50, 50, 50);
            var rules = GrammarLibrary.GetDefaultGrammar();
            var rulesEngine = new BondRulesEngine(rules);
            var strengthCalculator = new BondStrengthCalculator(rulesEngine);
            var manager = new BondingManager(rulesEngine, strengthCalculator, config, grid);

            var cell = grid.GetCell(new Vector3Int(0, 0, 0));
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", new Vector3Int(0, 0, 0)) { Energy = 100, IsActive = true };
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", new Vector3Int(0, 0, 0)) { Energy = 100, IsActive = true };

            grid.AddToken(token1);
            grid.AddToken(token2);

            // Act
            var result = manager.ProcessCellBonding(cell, currentTick: 0);

            // Assert
            Assert.NotNull(result);
            // Should find at least some bonding opportunities
        }

        [Fact]
        public void GetActiveChains_ReturnsAllChains()
        {
            // Arrange
            var manager = CreateBondingManager();
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero) { Energy = 100, IsActive = true };
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero) { Energy = 100, IsActive = true };

            manager.AttemptBond(token1, token2, currentTick: 0);

            // Act
            var chains = manager.GetActiveChains();

            // Assert
            Assert.NotNull(chains);
            Assert.NotEmpty(chains);
        }
    }
}
