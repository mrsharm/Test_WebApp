namespace WebApp_AppService.Configuration
{
    public class CpuWorkloadSettings
    {
        public const string SectionName = "CpuWorkloadSettings";

        /// <summary>
        /// Maximum allowed duration for CPU workload in seconds. Default: 300 (5 minutes)
        /// </summary>
        public int MaxDurationSeconds { get; set; } = 300;

        /// <summary>
        /// Default duration when not specified in request. Default: 10 seconds
        /// </summary>
        public int DefaultDurationSeconds { get; set; } = 10;

        /// <summary>
        /// Override for maximum thread count. If null, uses half of available cores (minimum 1)
        /// </summary>
        public int? MaxThreadCountOverride { get; set; }

        /// <summary>
        /// Whether to enable CPU-intensive prime number calculations. Default: true
        /// </summary>
        public bool EnablePrimeCalculations { get; set; } = true;

        /// <summary>
        /// Whether to enable detailed logging of CPU workload operations. Default: true
        /// </summary>
        public bool LoggingEnabled { get; set; } = true;
    }
}