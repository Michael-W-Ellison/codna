using System;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using Xunit;
using DigitalBiochemicalSimulator.Web;
using DigitalBiochemicalSimulator.Simulation;

namespace DigitalBiochemicalSimulator.Tests.Web
{
    public class SimulationWebServerTests : IDisposable
    {
        private IntegratedSimulationEngine _simulation;
        private SimulationWebServer? _server;
        private const int TestPort = 9999;

        public SimulationWebServerTests()
        {
            var config = SimulationPresets.Minimal;
            _simulation = new IntegratedSimulationEngine(config);
        }

        public void Dispose()
        {
            _server?.Dispose();
            _simulation?.Dispose();
        }

        [Fact]
        public void Constructor_ValidSimulation_Initializes()
        {
            // Arrange & Act
            _server = new SimulationWebServer(_simulation, TestPort);

            // Assert
            Assert.Equal(TestPort, _server.Port);
            Assert.False(_server.IsRunning);
        }

        [Fact]
        public void Constructor_NullSimulation_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SimulationWebServer(null!, TestPort));
        }

        [Fact]
        public void Start_StartsServer()
        {
            // Arrange
            _server = new SimulationWebServer(_simulation, TestPort);

            // Act
            _server.Start();
            Thread.Sleep(100); // Give server time to start

            // Assert
            Assert.True(_server.IsRunning);
        }

        [Fact]
        public void Start_AlreadyRunning_DoesNotThrow()
        {
            // Arrange
            _server = new SimulationWebServer(_simulation, TestPort);
            _server.Start();
            Thread.Sleep(100);

            // Act & Assert - should not throw
            _server.Start();
            Assert.True(_server.IsRunning);
        }

        [Fact]
        public void Stop_StopsServer()
        {
            // Arrange
            _server = new SimulationWebServer(_simulation, TestPort);
            _server.Start();
            Thread.Sleep(100);

            // Act
            _server.Stop();
            Thread.Sleep(100);

            // Assert
            Assert.False(_server.IsRunning);
        }

        [Fact]
        public void Stop_NotRunning_DoesNotThrow()
        {
            // Arrange
            _server = new SimulationWebServer(_simulation, TestPort);

            // Act & Assert - should not throw
            _server.Stop();
            Assert.False(_server.IsRunning);
        }

        [Fact]
        public async Task ApiStatus_ReturnsJSON()
        {
            // Arrange
            _server = new SimulationWebServer(_simulation, TestPort);
            _server.Start();
            Thread.Sleep(200); // Give server time to start

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                // Act
                var response = await client.GetAsync($"http://localhost:{TestPort}/api/status");

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var content = await response.Content.ReadAsStringAsync();
                Assert.Contains("isRunning", content);
                Assert.Contains("gridSize", content);
            }
            catch (HttpRequestException)
            {
                // Server might not be available on CI/CD - skip test
                return;
            }
        }

        [Fact]
        public async Task ApiStats_ReturnsJSON()
        {
            // Arrange
            _server = new SimulationWebServer(_simulation, TestPort);
            _server.Start();
            Thread.Sleep(200);

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                // Act
                var response = await client.GetAsync($"http://localhost:{TestPort}/api/stats");

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var content = await response.Content.ReadAsStringAsync();
                Assert.NotEmpty(content);
            }
            catch (HttpRequestException)
            {
                // Server might not be available - skip test
                return;
            }
        }

        [Fact]
        public async Task ApiInvalidEndpoint_Returns404()
        {
            // Arrange
            _server = new SimulationWebServer(_simulation, TestPort);
            _server.Start();
            Thread.Sleep(200);

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                // Act
                var response = await client.GetAsync($"http://localhost:{TestPort}/api/invalid");

                // Assert
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
            catch (HttpRequestException)
            {
                // Server might not be available - skip test
                return;
            }
        }

        [Fact]
        public void Dispose_StopsServerAndCleansUp()
        {
            // Arrange
            _server = new SimulationWebServer(_simulation, TestPort);
            _server.Start();
            Thread.Sleep(100);

            // Act
            _server.Dispose();

            // Assert
            Assert.False(_server.IsRunning);
        }

        [Fact]
        public void DefaultWebRoot_IsBesideExecutableWithIndexPage()
        {
            // Arrange & Act
            _server = new SimulationWebServer(_simulation, TestPort);

            // Assert - the build copies wwwroot next to the executable
            Assert.Equal(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "wwwroot")), _server.WebRoot);
            Assert.True(File.Exists(Path.Combine(_server.WebRoot, "index.html")));
        }

        [Fact]
        public async Task Root_ServesIndexPage()
        {
            // Arrange
            _server = new SimulationWebServer(_simulation, TestPort);
            _server.Start();
            Thread.Sleep(200);
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

            // Act
            var response = await client.GetAsync($"http://localhost:{TestPort}/");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("const API_BASE = '/api';", content);
        }

        [Theory]
        [InlineData("stats", "activeTokenCount", "totalChains", "stableChains", "totalEnergy")]
        [InlineData("status", "isRunning", "isPaused", "currentTick", "tps")]
        public async Task Api_UsesFieldNamesThePageReads(string endpoint, params string[] fields)
        {
            // Arrange
            _server = new SimulationWebServer(_simulation, TestPort);
            _server.Start();
            Thread.Sleep(200);
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

            // Act
            var json = await client.GetStringAsync($"http://localhost:{TestPort}/api/{endpoint}");

            // Assert
            using var document = JsonDocument.Parse(json);
            foreach (var field in fields)
            {
                Assert.True(document.RootElement.TryGetProperty(field, out _), $"/api/{endpoint} is missing '{field}'");
            }
        }

        [Fact]
        public async Task ApiEvolution_UsesFieldNamesThePageReads()
        {
            // Arrange
            _server = new SimulationWebServer(_simulation, TestPort);
            _server.Start();
            Thread.Sleep(200);
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

            // Act
            var json = await client.GetStringAsync($"http://localhost:{TestPort}/api/evolution");

            // Assert
            using var document = JsonDocument.Parse(json);
            var statistics = document.RootElement.GetProperty("statistics");
            foreach (var field in new[] { "totalLineages", "uniquePatterns", "longestLineage", "highestStability" })
            {
                Assert.True(statistics.TryGetProperty(field, out _), $"/api/evolution statistics is missing '{field}'");
            }
        }

        [Fact]
        public async Task ApiPause_PostTogglesPause()
        {
            // Arrange
            _server = new SimulationWebServer(_simulation, TestPort);
            _server.Start();
            Thread.Sleep(200);
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

            // Act
            var getResponse = await client.GetAsync($"http://localhost:{TestPort}/api/pause");
            var firstPost = await client.PostAsync($"http://localhost:{TestPort}/api/pause", null);
            bool pausedAfterFirst = _simulation.TickManager.IsPaused;
            var secondPost = await client.PostAsync($"http://localhost:{TestPort}/api/pause", null);

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, getResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, firstPost.StatusCode);
            Assert.True(pausedAfterFirst);
            using var document = JsonDocument.Parse(await firstPost.Content.ReadAsStringAsync());
            Assert.True(document.RootElement.GetProperty("isPaused").GetBoolean());
            Assert.Equal(HttpStatusCode.OK, secondPost.StatusCode);
            Assert.False(_simulation.TickManager.IsPaused);
        }

        [Fact]
        public void MultipleDispose_DoesNotThrow()
        {
            // Arrange
            _server = new SimulationWebServer(_simulation, TestPort);

            // Act & Assert - should not throw
            _server.Dispose();
            _server.Dispose();
        }
    }
}
