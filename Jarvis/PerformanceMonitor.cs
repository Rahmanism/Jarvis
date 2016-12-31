using System;
using System.Diagnostics;

namespace Jarvis
{
    /// <summary>
    /// Provides some performance counters
    /// </summary>
    class PerformanceMonitor : IDisposable
    {
        #region Fields
        PerformanceCounter perfCpuCount,
          perfMemCount,
          perfUpTimeCount;
        #endregion

        #region Properties
        /// <summary>
        /// Gets cpu load percentage
        /// </summary>
        public int CpuCount
        {
            get { return (int)perfCpuCount.NextValue(); }
        }

        /// <summary>
        /// Gets the available memory in megabytes
        /// </summary>
        public int AvailableMemory
        {
            get { return (int)perfMemCount.NextValue(); }
        }

        /// <summary>
        /// Gets the system up time in TimeSpan type
        /// </summary>
        public TimeSpan UpTime
        {
            get { return TimeSpan.FromSeconds( perfUpTimeCount.NextValue() ); }
        }
        #endregion

        #region Constructor
        public PerformanceMonitor()
        {
            InitializePerformanceCounters();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Initializes the performance counters
        /// </summary>
        private void InitializePerformanceCounters()
        {
            // This will pull Cpu load in percentage
            perfCpuCount = new PerformanceCounter( "Processor Information", "% Processor Time", "_Total" );
            perfCpuCount.NextValue();

            // This will pull memory available in megabytes
            perfMemCount = new PerformanceCounter( "Memory", "Available MBytes" );
            perfMemCount.NextValue();

            // This will pull system up time
            perfUpTimeCount = new PerformanceCounter( "System", "System Up Time" );
            perfUpTimeCount.NextValue();
        }
        #endregion

        #region Dispose
        /// <summary>
        /// Disposes all performance counters
        /// </summary>
        public void Dispose()
        {
            perfCpuCount.Dispose();
            perfMemCount.Dispose();
            perfUpTimeCount.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
