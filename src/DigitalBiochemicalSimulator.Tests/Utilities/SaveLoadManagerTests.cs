using Xunit;
using DigitalBiochemicalSimulator.Core;
using DigitalBiochemicalSimulator.DataStructures;
using DigitalBiochemicalSimulator.Simulation;
using DigitalBiochemicalSimulator.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DigitalBiochemicalSimulator.Tests.Utilities
{
    public class SaveLoadManagerTests
    {
        private readonly string _testDirectory;

        public SaveLoadManagerTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "BiochemSimTests", Path.GetRandomFileName());
        }

        [Fact]
        public void SaveLoadManager_Creation_InitializesCorrectly()
        {
            // Arrange & Act
            var manager = new SaveLoadManager(_testDirectory);

            // Assert
            Assert.NotNull(manager);
            Assert.Equal(_testDirectory, manager.DefaultDirectory);
            Assert.True(Directory.Exists(_testDirectory));

            // Cleanup
            Directory.Delete(_testDirectory, true);
        }

        [Fact]
        public void SaveLoadManager_Save_CreatesFile()
        {
            // Arrange
            var manager = new SaveLoadManager(_testDirectory);
            var state = CreateTestState();

            // Act
            var result = manager.Save(state, "test_save");

            // Assert
            Assert.True(result.Success, result.ErrorMessage);
            Assert.True(File.Exists(result.FilePath));
            Assert.True(result.FileSizeBytes > 0);
            Assert.True(result.SaveDurationMs >= 0);

            // Cleanup
            Directory.Delete(_testDirectory, true);
        }

        [Fact]
        public void SaveLoadManager_Load_ReturnsCorrectState()
        {
            // Arrange
            var manager = new SaveLoadManager(_testDirectory);
            var originalState = CreateTestState();
            manager.Save(originalState, "test_load");

            // Act
            var result = manager.Load("test_load");

            // Assert
            Assert.True(result.Success, result.ErrorMessage);
            Assert.NotNull(result.State);
            Assert.Equal(originalState.Metadata.CurrentTick, result.State.Metadata.CurrentTick);
            Assert.Equal(originalState.Tokens.Count, result.State.Tokens.Count);
            Assert.Equal(originalState.Chains.Count, result.State.Chains.Count);

            // Cleanup
            Directory.Delete(_testDirectory, true);
        }

        [Fact]
        public void SaveLoadManager_SaveLoad_PreservesTokenData()
        {
            // Arrange
            var manager = new SaveLoadManager(_testDirectory);
            var state = CreateTestState();
            var originalToken = state.Tokens[0];

            // Act
            manager.Save(state, "token_test");
            var loadedState = manager.Load("token_test").State;
            var loadedToken = loadedState.Tokens[0];

            // Assert
            Assert.Equal(originalToken.Id, loadedToken.Id);
            Assert.Equal(originalToken.Type, loadedToken.Type);
            Assert.Equal(originalToken.Value, loadedToken.Value);
            Assert.Equal(originalToken.Energy, loadedToken.Energy);
            Assert.Equal(originalToken.Position.X, loadedToken.Position.X);
            Assert.Equal(originalToken.Position.Y, loadedToken.Position.Y);
            Assert.Equal(originalToken.Position.Z, loadedToken.Position.Z);

            // Cleanup
            Directory.Delete(_testDirectory, true);
        }

        [Fact]
        public void SaveLoadManager_SaveLoad_PreservesChainData()
        {
            // Arrange
            var manager = new SaveLoadManager(_testDirectory);
            var state = CreateTestState();
            var originalChain = state.Chains[0];

            // Act
            manager.Save(state, "chain_test");
            var loadedState = manager.Load("chain_test").State;
            var loadedChain = loadedState.Chains[0];

            // Assert
            Assert.Equal(originalChain.Id, loadedChain.Id);
            Assert.Equal(originalChain.Length, loadedChain.Length);
            Assert.Equal(originalChain.StabilityScore, loadedChain.StabilityScore);
            Assert.Equal(originalChain.IsValid, loadedChain.IsValid);
            Assert.Equal(originalChain.TokenIds.Count, loadedChain.TokenIds.Count);

            // Cleanup
            Directory.Delete(_testDirectory, true);
        }

        [Fact]
        public void SaveLoadManager_Load_NonExistentFile_ReturnsFailure()
        {
            // Arrange
            var manager = new SaveLoadManager(_testDirectory);

            // Act
            var result = manager.Load("nonexistent_file");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("File not found", result.ErrorMessage);

            // Cleanup
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }

        [Fact]
        public void SaveLoadManager_Save_NullState_ReturnsFailure()
        {
            // Arrange
            var manager = new SaveLoadManager(_testDirectory);

            // Act
            var result = manager.Save(null, "null_test");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("null", result.ErrorMessage.ToLower());

            // Cleanup
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }

        [Fact]
        public void SaveLoadManager_ListSaves_ReturnsAllFiles()
        {
            // Arrange
            var manager = new SaveLoadManager(_testDirectory);
            var state = CreateTestState();

            manager.Save(state, "save1");
            manager.Save(state, "save2");
            manager.Save(state, "save3");

            // Act
            var saves = manager.ListSaves();

            // Assert
            Assert.Equal(3, saves.Length);
            Assert.Contains("save1", saves);
            Assert.Contains("save2", saves);
            Assert.Contains("save3", saves);

            // Cleanup
            Directory.Delete(_testDirectory, true);
        }

        [Fact]
        public void SaveLoadManager_DeleteSave_RemovesFile()
        {
            // Arrange
            var manager = new SaveLoadManager(_testDirectory);
            var state = CreateTestState();
            manager.Save(state, "delete_test");

            // Act
            bool deleted = manager.DeleteSave("delete_test");
            var saves = manager.ListSaves();

            // Assert
            Assert.True(deleted);
            Assert.DoesNotContain("delete_test", saves);

            // Cleanup
            Directory.Delete(_testDirectory, true);
        }

        [Fact]
        public void SaveLoadManager_GetSaveInfo_ReturnsMetadata()
        {
            // Arrange
            var manager = new SaveLoadManager(_testDirectory);
            var state = CreateTestState();
            state.Metadata.Description = "Test save file";
            manager.Save(state, "info_test");

            // Act
            var info = manager.GetSaveInfo("info_test");

            // Assert
            Assert.NotNull(info);
            Assert.Equal("info_test", info.FileName);
            Assert.Equal(100, info.CurrentTick);
            Assert.Equal("Test save file", info.Description);
            Assert.Equal(3, info.TokenCount);
            Assert.Equal(1, info.ChainCount);
            Assert.True(info.FileSizeBytes > 0);

            // Cleanup
            Directory.Delete(_testDirectory, true);
        }

        [Fact]
        public void SaveLoadManager_GenerateAutoSaveFileName_CreatesUniqueNames()
        {
            // Arrange
            var manager = new SaveLoadManager(_testDirectory);

            // Act
            var name1 = manager.GenerateAutoSaveFileName();
            System.Threading.Thread.Sleep(1100); // Wait to ensure different timestamp
            var name2 = manager.GenerateAutoSaveFileName();

            // Assert
            Assert.NotEqual(name1, name2);
            Assert.Contains("autosave", name1);
            Assert.Contains("autosave", name2);
            Assert.EndsWith(".json", name1);
            Assert.EndsWith(".json", name2);

            // Cleanup
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }

        [Fact]
        public async void SaveLoadManager_SaveAsync_CreatesFile()
        {
            // Arrange
            var manager = new SaveLoadManager(_testDirectory);
            var state = CreateTestState();

            // Act
            var result = await manager.SaveAsync(state, "async_save");

            // Assert
            Assert.True(result.Success);
            Assert.True(File.Exists(result.FilePath));

            // Cleanup
            Directory.Delete(_testDirectory, true);
        }

        [Fact]
        public async void SaveLoadManager_LoadAsync_ReturnsCorrectState()
        {
            // Arrange
            var manager = new SaveLoadManager(_testDirectory);
            var state = CreateTestState();
            await manager.SaveAsync(state, "async_load");

            // Act
            var result = await manager.LoadAsync("async_load");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.State);
            Assert.Equal(state.Tokens.Count, result.State.Tokens.Count);

            // Cleanup
            Directory.Delete(_testDirectory, true);
        }

        [Fact]
        public void SimulationStateBuilder_Build_CreatesValidState()
        {
            // Arrange
            var config = new SimulationConfig(10, 10, 10);
            var tokens = new List<Token>
            {
                new Token(1, TokenType.INTEGER_LITERAL, "42", new Vector3Int(5, 5, 5))
            };

            var builder = new SimulationStateBuilder();

            // Act
            var state = builder
                .WithMetadata(100, "Test state")
                .WithConfiguration(config)
                .WithTokens(tokens)
                .WithGrid(new Grid(10, 10, 10))
                .Build();

            // Assert
            Assert.True(state.IsValid());
            Assert.Equal(100, state.Metadata.CurrentTick);
            Assert.Equal("Test state", state.Metadata.Description);
            Assert.Single(state.Tokens);
        }

        [Fact]
        public void SimulationStateBuilder_BuildWithoutRequired_ThrowsException()
        {
            // Arrange
            var builder = new SimulationStateBuilder();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Fact]
        public void SimulationState_IsValid_ReturnsFalseWhenIncomplete()
        {
            // Arrange
            var state = new SimulationState
            {
                Metadata = new StateMetadata(),
                Tokens = new List<TokenState>()
                // Missing Configuration and Grid
            };

            // Act
            var isValid = state.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void TokenState_FromToken_ConvertsCorrectly()
        {
            // Arrange
            var token = new Token(1, TokenType.INTEGER_LITERAL, "42", new Vector3Int(5, 10, 3))
            {
                Energy = 100,
                Mass = 5,
                DamageLevel = 0.3f
            };

            // Act
            var tokenState = TokenState.FromToken(token);

            // Assert
            Assert.Equal(token.Id, tokenState.Id);
            Assert.Equal("INTEGER_LITERAL", tokenState.Type);
            Assert.Equal("42", tokenState.Value);
            Assert.Equal(5, tokenState.Position.X);
            Assert.Equal(10, tokenState.Position.Y);
            Assert.Equal(3, tokenState.Position.Z);
            Assert.Equal(100, tokenState.Energy);
            Assert.Equal(5, tokenState.Mass);
            Assert.Equal(0.3f, tokenState.DamageLevel);
        }

        [Fact]
        public void ChainState_FromChain_ConvertsCorrectly()
        {
            // Arrange
            var token1 = new Token(1, TokenType.INTEGER_LITERAL, "5", Vector3Int.Zero);
            var token2 = new Token(2, TokenType.OPERATOR_PLUS, "+", Vector3Int.Zero);

            var chain = new TokenChain(token1);
            chain.AddToken(token2, atTail: true);
            chain.StabilityScore = 0.85f;
            chain.IsValid = true;

            // Act
            var chainState = ChainState.FromChain(chain);

            // Assert
            Assert.Equal(chain.Length, chainState.Length);
            Assert.Equal(chain.StabilityScore, chainState.StabilityScore);
            Assert.Equal(chain.IsValid, chainState.IsValid);
            Assert.Equal(2, chainState.TokenIds.Count);
        }

        private SimulationState CreateTestState()
        {
            var config = new SimulationConfig(10, 10, 10);

            var tokens = new List<Token>
            {
                new Token(1, TokenType.INTEGER_LITERAL, "5", new Vector3Int(5, 5, 5)),
                new Token(2, TokenType.OPERATOR_PLUS, "+", new Vector3Int(5, 5, 5)),
                new Token(3, TokenType.INTEGER_LITERAL, "3", new Vector3Int(5, 5, 5))
            };

            var chain = new TokenChain(tokens[0]);
            chain.AddToken(tokens[1], atTail: true);
            chain.AddToken(tokens[2], atTail: true);

            var grid = new Grid(10, 10, 10);

            return new SimulationStateBuilder()
                .WithMetadata(100, "Test simulation")
                .WithConfiguration(config)
                .WithTokens(tokens)
                .WithChains(new List<TokenChain> { chain })
                .WithGrid(grid)
                .Build();
        }
    }
}
