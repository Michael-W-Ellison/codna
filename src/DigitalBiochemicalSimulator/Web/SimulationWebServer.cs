using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DigitalBiochemicalSimulator.Simulation;

namespace DigitalBiochemicalSimulator.Web
{
    /// <summary>
    /// Simple HTTP web server for simulation API and frontend
    /// Provides REST endpoints and serves static files
    /// </summary>
    public class SimulationWebServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly IntegratedSimulationEngine _simulation;
        private readonly string _webRoot;
        private bool _isRunning;
        private Thread _serverThread;
        private bool _disposed;

        public int Port { get; private set; }
        public bool IsRunning => _isRunning;

        public SimulationWebServer(IntegratedSimulationEngine simulation, int port = 8080, string webRoot = null)
        {
            _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            Port = port;
            _webRoot = webRoot ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        }

        /// <summary>
        /// Starts the web server
        /// </summary>
        public void Start()
        {
            if (_isRunning)
                return;

            Directory.CreateDirectory(_webRoot);

            _listener.Start();
            _isRunning = true;

            _serverThread = new Thread(ServerLoop)
            {
                IsBackground = true,
                Name = "WebServerThread"
            };
            _serverThread.Start();

            Console.WriteLine($"Web server started at http://localhost:{Port}/");
        }

        /// <summary>
        /// Stops the web server
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            _listener.Stop();
            _serverThread?.Join(TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Main server loop
        /// </summary>
        private void ServerLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var context = _listener.GetContext();
                    Task.Run(() => HandleRequest(context));
                }
                catch (HttpListenerException)
                {
                    // Expected when stopping
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Server error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles an incoming HTTP request
        /// </summary>
        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                // Add CORS headers
                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }

                var path = request.Url.AbsolutePath;

                // API endpoints
                if (path.StartsWith("/api/"))
                {
                    HandleApiRequest(path, request, response);
                }
                // Static files
                else
                {
                    HandleStaticFile(path, response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request handling error: {ex.Message}");
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch { }
            }
        }

        /// <summary>
        /// Handles API requests
        /// </summary>
        private void HandleApiRequest(string path, HttpListenerRequest request, HttpListenerResponse response)
        {
            string jsonResponse = null;

            switch (path)
            {
                case "/api/status":
                    jsonResponse = GetStatus();
                    break;

                case "/api/stats":
                    jsonResponse = GetStatistics();
                    break;

                case "/api/dashboard":
                    jsonResponse = GetDashboard();
                    break;

                case "/api/grid":
                    jsonResponse = GetGridData();
                    break;

                case "/api/chains":
                    jsonResponse = GetChains();
                    break;

                case "/api/evolution":
                    jsonResponse = GetEvolution();
                    break;

                case "/api/export/json":
                    jsonResponse = _simulation?.ExportAnalyticsToJSON() ?? "{}";
                    break;

                case "/api/export/csv":
                    var csvData = _simulation?.ExportAnalyticsToCSV() ?? "";
                    SendCSVResponse(csvData, response);
                    return;

                default:
                    response.StatusCode = 404;
                    jsonResponse = JsonSerializer.Serialize(new { error = "Endpoint not found" });
                    break;
            }

            SendJSONResponse(jsonResponse, response);
        }

        /// <summary>
        /// Gets simulation status
        /// </summary>
        private string GetStatus()
        {
            if (_simulation == null)
            {
                return JsonSerializer.Serialize(new
                {
                    isRunning = false,
                    isPaused = false,
                    currentTick = 0,
                    tps = 0.0,
                    gridSize = new { width = 0, height = 0, depth = 0 }
                });
            }

            var data = new
            {
                isRunning = _simulation.IsRunning,
                isPaused = _simulation.TickManager?.IsPaused ?? false,
                currentTick = _simulation.TickManager?.CurrentTick ?? 0,
                tps = _simulation.TickManager?.ActualTicksPerSecond ?? 0.0,
                gridSize = new
                {
                    width = _simulation.Grid?.Width ?? 0,
                    height = _simulation.Grid?.Height ?? 0,
                    depth = _simulation.Grid?.Depth ?? 0
                }
            };

            return JsonSerializer.Serialize(data);
        }

        /// <summary>
        /// Gets simulation statistics
        /// </summary>
        private string GetStatistics()
        {
            if (_simulation == null)
                return JsonSerializer.Serialize(new { });

            var stats = _simulation.GetStatistics();
            return JsonSerializer.Serialize(stats);
        }

        /// <summary>
        /// Gets dashboard data
        /// </summary>
        private string GetDashboard()
        {
            if (_simulation == null)
                return JsonSerializer.Serialize(new { });

            var dashboard = _simulation.GetDashboardData();
            return JsonSerializer.Serialize(dashboard);
        }

        /// <summary>
        /// Gets grid data for visualization
        /// </summary>
        private string GetGridData()
        {
            if (_simulation?.Grid == null)
                return JsonSerializer.Serialize(new { cells = new object[0] });

            var activeCells = _simulation.Grid.ActiveCells;
            var cellData = new System.Collections.Generic.List<object>();

            foreach (var pos in activeCells)
            {
                var cell = _simulation.Grid.GetCell(pos);
                if (cell != null && cell.Tokens.Count > 0)
                {
                    cellData.Add(new
                    {
                        x = pos.X,
                        y = pos.Y,
                        z = pos.Z,
                        tokenCount = cell.Tokens.Count,
                        totalMass = cell.TotalMass,
                        tokens = cell.Tokens.Select(t => new
                        {
                            id = t.Id,
                            type = t.Type.ToString(),
                            value = t.Value,
                            energy = t.Energy
                        })
                    });
                }
            }

            return JsonSerializer.Serialize(new { cells = cellData });
        }

        /// <summary>
        /// Gets chain data
        /// </summary>
        private string GetChains()
        {
            if (_simulation?.ChainRegistry == null)
                return JsonSerializer.Serialize(new { chains = new object[0] });

            var chains = _simulation.ChainRegistry.GetAllChains();
            var chainData = chains.Select(c => new
            {
                id = c.Id,
                length = c.Length,
                stability = c.StabilityScore,
                isValid = c.IsValid,
                energy = c.TotalEnergy,
                tokens = c.Tokens.Select(t => new
                {
                    type = t.Type.ToString(),
                    value = t.Value
                })
            });

            return JsonSerializer.Serialize(new { chains = chainData });
        }

        /// <summary>
        /// Gets evolution statistics
        /// </summary>
        private string GetEvolution()
        {
            if (_simulation?.Analytics?.Evolution == null)
            {
                return JsonSerializer.Serialize(new
                {
                    statistics = new { },
                    topLineages = new object[0],
                    commonPatterns = new object[0]
                });
            }

            var evolutionStats = _simulation.Analytics.Evolution.GetStatistics();
            var topLineages = _simulation.Analytics.Evolution.GetTopLineages(10);
            var patterns = _simulation.Analytics.Evolution.IdentifyCommonPatterns().Take(10);

            var data = new
            {
                statistics = evolutionStats,
                topLineages = topLineages,
                commonPatterns = patterns
            };

            return JsonSerializer.Serialize(data);
        }

        /// <summary>
        /// Sends JSON response
        /// </summary>
        private void SendJSONResponse(string json, HttpListenerResponse response)
        {
            if (json == null)
            {
                response.StatusCode = 500;
                json = JsonSerializer.Serialize(new { error = "Internal server error" });
            }

            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            response.StatusCode = 200;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        /// <summary>
        /// Sends CSV response
        /// </summary>
        private void SendCSVResponse(string csv, HttpListenerResponse response)
        {
            var buffer = Encoding.UTF8.GetBytes(csv);
            response.ContentType = "text/csv";
            response.ContentLength64 = buffer.Length;
            response.AddHeader("Content-Disposition", "attachment; filename=simulation-data.csv");
            response.StatusCode = 200;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        /// <summary>
        /// Handles static file requests
        /// </summary>
        private void HandleStaticFile(string path, HttpListenerResponse response)
        {
            if (path == "/")
                path = "/index.html";

            var filePath = Path.Combine(_webRoot, path.TrimStart('/'));

            if (!File.Exists(filePath))
            {
                response.StatusCode = 404;
                var buffer = Encoding.UTF8.GetBytes("<h1>404 - Not Found</h1>");
                response.ContentType = "text/html";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Close();
                return;
            }

            try
            {
                var content = File.ReadAllBytes(filePath);
                response.ContentType = GetContentType(filePath);
                response.ContentLength64 = content.Length;
                response.StatusCode = 200;
                response.OutputStream.Write(content, 0, content.Length);
                response.Close();
            }
            catch
            {
                response.StatusCode = 500;
                response.Close();
            }
        }

        /// <summary>
        /// Gets content type based on file extension
        /// </summary>
        private string GetContentType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".html" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".json" => "application/json",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Stop();
            _listener?.Close();
            _disposed = true;
        }
    }
}
