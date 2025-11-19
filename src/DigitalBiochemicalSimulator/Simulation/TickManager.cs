using System;
using System.Diagnostics;

namespace DigitalBiochemicalSimulator.Simulation
{
    /// <summary>
    /// Manages simulation time steps and timing control.
    /// Based on section 2.2.1 of the design specification.
    /// </summary>
    public class TickManager
    {
        public long CurrentTick { get; private set; }
        public double TicksPerSecond { get; set; }
        public int TickDurationMs { get; set; }
        public bool IsPaused { get; set; }

        private Stopwatch _stopwatch;
        private double _accumulator;
        private DateTime _lastTickTime;

        // Statistics
        public double ActualTicksPerSecond { get; private set; }
        public double AverageTickDuration { get; private set; }
        public long TotalTicksExecuted { get; private set; }

        public TickManager(int tickDurationMs = 100)
        {
            CurrentTick = 0;
            TickDurationMs = tickDurationMs;
            TicksPerSecond = 1000.0 / tickDurationMs;
            IsPaused = false;

            _stopwatch = new Stopwatch();
            _accumulator = 0;
            _lastTickTime = DateTime.Now;

            ActualTicksPerSecond = 0;
            AverageTickDuration = 0;
            TotalTicksExecuted = 0;
        }

        /// <summary>
        /// Starts the tick manager
        /// </summary>
        public void Start()
        {
            _stopwatch.Start();
            _lastTickTime = DateTime.Now;
        }

        /// <summary>
        /// Stops the tick manager
        /// </summary>
        public void Stop()
        {
            _stopwatch.Stop();
        }

        /// <summary>
        /// Resets the tick counter
        /// </summary>
        public void Reset()
        {
            CurrentTick = 0;
            TotalTicksExecuted = 0;
            _accumulator = 0;
            _stopwatch.Reset();
        }

        /// <summary>
        /// Checks if it's time for the next tick
        /// </summary>
        public bool ShouldTick()
        {
            if (IsPaused)
                return false;

            var elapsed = _stopwatch.Elapsed.TotalMilliseconds;
            _accumulator += elapsed;
            _stopwatch.Restart();

            return _accumulator >= TickDurationMs;
        }

        /// <summary>
        /// Executes a tick and updates statistics
        /// </summary>
        public void Tick()
        {
            if (IsPaused)
                return;

            var tickStart = DateTime.Now;

            CurrentTick++;
            TotalTicksExecuted++;
            _accumulator -= TickDurationMs;

            // Update statistics
            var tickDuration = (DateTime.Now - tickStart).TotalMilliseconds;
            AverageTickDuration = (AverageTickDuration * (TotalTicksExecuted - 1) + tickDuration) / TotalTicksExecuted;

            var timeSinceLastTick = (DateTime.Now - _lastTickTime).TotalSeconds;
            if (timeSinceLastTick > 0)
            {
                ActualTicksPerSecond = 1.0 / timeSinceLastTick;
            }
            _lastTickTime = DateTime.Now;
        }

        /// <summary>
        /// Forces a single tick (for debugging/stepping)
        /// </summary>
        public void StepOnce()
        {
            bool wasPaused = IsPaused;
            IsPaused = false;
            Tick();
            IsPaused = wasPaused;
        }

        /// <summary>
        /// Adjusts tick speed
        /// </summary>
        public void SetTicksPerSecond(double ticksPerSecond)
        {
            TicksPerSecond = Math.Clamp(ticksPerSecond, 0.1, 100.0);
            TickDurationMs = (int)(1000.0 / TicksPerSecond);
        }

        /// <summary>
        /// Increases tick speed
        /// </summary>
        public void SpeedUp(double factor = 1.5)
        {
            SetTicksPerSecond(TicksPerSecond * factor);
        }

        /// <summary>
        /// Decreases tick speed
        /// </summary>
        public void SlowDown(double factor = 1.5)
        {
            SetTicksPerSecond(TicksPerSecond / factor);
        }

        public override string ToString()
        {
            return $"Tick {CurrentTick} ({ActualTicksPerSecond:F1} TPS, Avg: {AverageTickDuration:F2}ms)";
        }
    }
}
