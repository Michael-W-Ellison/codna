using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DigitalBiochemicalSimulator.Simulation;

namespace DigitalBiochemicalSimulator.Utilities
{
    /// <summary>
    /// Manages saving and loading simulation states to/from JSON files.
    /// Provides synchronous and asynchronous save/load operations.
    /// </summary>
    public class SaveLoadManager
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _defaultDirectory;

        public string DefaultDirectory => _defaultDirectory;

        public SaveLoadManager(string defaultDirectory = null)
        {
            _defaultDirectory = defaultDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "saves");

            // Configure JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };

            // Ensure default directory exists
            EnsureDirectoryExists(_defaultDirectory);
        }

        /// <summary>
        /// Saves simulation state to a JSON file synchronously
        /// </summary>
        public SaveResult Save(SimulationState state, string fileName)
        {
            var result = new SaveResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (state == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "SimulationState is null";
                    return result;
                }

                if (!state.IsValid())
                {
                    result.Success = false;
                    result.ErrorMessage = "SimulationState is invalid or incomplete";
                    return result;
                }

                var filePath = GetFullPath(fileName);
                var directoryPath = Path.GetDirectoryName(filePath);
                EnsureDirectoryExists(directoryPath);

                var json = JsonSerializer.Serialize(state, _jsonOptions);
                File.WriteAllText(filePath, json);

                stopwatch.Stop();

                result.Success = true;
                result.FilePath = filePath;
                result.FileSizeBytes = new FileInfo(filePath).Length;
                result.SaveDurationMs = stopwatch.ElapsedMilliseconds;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.ErrorMessage = $"Save failed: {ex.Message}";
                result.SaveDurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }
        }

        /// <summary>
        /// Saves simulation state to a JSON file asynchronously
        /// </summary>
        public async Task<SaveResult> SaveAsync(SimulationState state, string fileName)
        {
            var result = new SaveResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (state == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "SimulationState is null";
                    return result;
                }

                if (!state.IsValid())
                {
                    result.Success = false;
                    result.ErrorMessage = "SimulationState is invalid or incomplete";
                    return result;
                }

                var filePath = GetFullPath(fileName);
                var directoryPath = Path.GetDirectoryName(filePath);
                EnsureDirectoryExists(directoryPath);

                await using var stream = File.Create(filePath);
                await JsonSerializer.SerializeAsync(stream, state, _jsonOptions);

                stopwatch.Stop();

                result.Success = true;
                result.FilePath = filePath;
                result.FileSizeBytes = new FileInfo(filePath).Length;
                result.SaveDurationMs = stopwatch.ElapsedMilliseconds;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.ErrorMessage = $"Save failed: {ex.Message}";
                result.SaveDurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }
        }

        /// <summary>
        /// Loads simulation state from a JSON file synchronously
        /// </summary>
        public LoadResult Load(string fileName)
        {
            var result = new LoadResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var filePath = GetFullPath(fileName);

                if (!File.Exists(filePath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"File not found: {filePath}";
                    return result;
                }

                var json = File.ReadAllText(filePath);
                var state = JsonSerializer.Deserialize<SimulationState>(json, _jsonOptions);

                stopwatch.Stop();

                if (state == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to deserialize state (result was null)";
                    result.LoadDurationMs = stopwatch.ElapsedMilliseconds;
                    return result;
                }

                if (!state.IsValid())
                {
                    result.Success = false;
                    result.ErrorMessage = "Loaded state is invalid or incomplete";
                    result.State = state;
                    result.LoadDurationMs = stopwatch.ElapsedMilliseconds;
                    return result;
                }

                result.Success = true;
                result.State = state;
                result.FilePath = filePath;
                result.LoadDurationMs = stopwatch.ElapsedMilliseconds;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.ErrorMessage = $"Load failed: {ex.Message}";
                result.LoadDurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }
        }

        /// <summary>
        /// Loads simulation state from a JSON file asynchronously
        /// </summary>
        public async Task<LoadResult> LoadAsync(string fileName)
        {
            var result = new LoadResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var filePath = GetFullPath(fileName);

                if (!File.Exists(filePath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"File not found: {filePath}";
                    return result;
                }

                await using var stream = File.OpenRead(filePath);
                var state = await JsonSerializer.DeserializeAsync<SimulationState>(stream, _jsonOptions);

                stopwatch.Stop();

                if (state == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to deserialize state (result was null)";
                    result.LoadDurationMs = stopwatch.ElapsedMilliseconds;
                    return result;
                }

                if (!state.IsValid())
                {
                    result.Success = false;
                    result.ErrorMessage = "Loaded state is invalid or incomplete";
                    result.State = state;
                    result.LoadDurationMs = stopwatch.ElapsedMilliseconds;
                    return result;
                }

                result.Success = true;
                result.State = state;
                result.FilePath = filePath;
                result.LoadDurationMs = stopwatch.ElapsedMilliseconds;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.ErrorMessage = $"Load failed: {ex.Message}";
                result.LoadDurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }
        }

        /// <summary>
        /// Lists all save files in the default directory
        /// </summary>
        public string[] ListSaves()
        {
            if (!Directory.Exists(_defaultDirectory))
                return Array.Empty<string>();

            var files = Directory.GetFiles(_defaultDirectory, "*.json");
            return files.Select(Path.GetFileNameWithoutExtension).ToArray();
        }

        /// <summary>
        /// Deletes a save file
        /// </summary>
        public bool DeleteSave(string fileName)
        {
            try
            {
                var filePath = GetFullPath(fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets save file metadata without loading full state
        /// </summary>
        public SaveFileInfo GetSaveInfo(string fileName)
        {
            try
            {
                var filePath = GetFullPath(fileName);
                if (!File.Exists(filePath))
                    return null;

                var fileInfo = new FileInfo(filePath);
                var json = File.ReadAllText(filePath);

                // Parse only metadata to avoid loading entire file
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var info = new SaveFileInfo
                {
                    FileName = Path.GetFileNameWithoutExtension(fileName),
                    FilePath = filePath,
                    FileSizeBytes = fileInfo.Length,
                    CreatedAt = fileInfo.CreationTime,
                    ModifiedAt = fileInfo.LastWriteTime
                };

                if (root.TryGetProperty("metadata", out var metadata))
                {
                    if (metadata.TryGetProperty("currentTick", out var tick))
                        info.CurrentTick = tick.GetInt64();

                    if (metadata.TryGetProperty("savedAt", out var savedAt))
                        info.SavedAt = savedAt.GetDateTime();

                    if (metadata.TryGetProperty("description", out var desc))
                        info.Description = desc.GetString();

                    if (metadata.TryGetProperty("version", out var ver))
                        info.Version = ver.GetString();
                }

                if (root.TryGetProperty("tokens", out var tokens))
                {
                    info.TokenCount = tokens.GetArrayLength();
                }

                if (root.TryGetProperty("chains", out var chains))
                {
                    info.ChainCount = chains.GetArrayLength();
                }

                return info;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates an auto-save filename with timestamp
        /// </summary>
        public string GenerateAutoSaveFileName(string prefix = "autosave")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{prefix}_{timestamp}.json";
        }

        private string GetFullPath(string fileName)
        {
            // If already absolute path, use as-is
            if (Path.IsPathRooted(fileName))
                return fileName;

            // Ensure .json extension
            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                fileName += ".json";

            return Path.Combine(_defaultDirectory, fileName);
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }

    /// <summary>
    /// Result of a save operation
    /// </summary>
    public class SaveResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; }
        public long FileSizeBytes { get; set; }
        public long SaveDurationMs { get; set; }
        public string ErrorMessage { get; set; }

        public string FileSizeFormatted => FormatBytes(FileSizeBytes);

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public override string ToString()
        {
            return Success
                ? $"Saved successfully to {FilePath} ({FileSizeFormatted}) in {SaveDurationMs}ms"
                : $"Save failed: {ErrorMessage}";
        }
    }

    /// <summary>
    /// Result of a load operation
    /// </summary>
    public class LoadResult
    {
        public bool Success { get; set; }
        public SimulationState State { get; set; }
        public string FilePath { get; set; }
        public long LoadDurationMs { get; set; }
        public string ErrorMessage { get; set; }

        public override string ToString()
        {
            return Success
                ? $"Loaded successfully from {FilePath} in {LoadDurationMs}ms"
                : $"Load failed: {ErrorMessage}";
        }
    }

    /// <summary>
    /// Information about a save file
    /// </summary>
    public class SaveFileInfo
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public DateTime SavedAt { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public long CurrentTick { get; set; }
        public int TokenCount { get; set; }
        public int ChainCount { get; set; }

        public string FileSizeFormatted => FormatBytes(FileSizeBytes);

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public override string ToString()
        {
            return $"{FileName}: {TokenCount} tokens, {ChainCount} chains, Tick {CurrentTick} ({FileSizeFormatted})";
        }
    }
}
