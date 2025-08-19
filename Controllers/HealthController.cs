using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime;

namespace WebApp_AppService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<HealthController> _logger;
        private static readonly DateTime _startTime = DateTime.UtcNow;

        public HealthController(IConfiguration configuration, ILogger<HealthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Provides detailed health and diagnostics information for SRE monitoring
        /// </summary>
        [HttpGet]
        [Route("diagnostics")]
        public ActionResult<object> GetDiagnostics()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var gcInfo = GC.GetTotalMemory(false);
                
                var diagnostics = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Uptime = DateTime.UtcNow - _startTime,
                    Memory = new
                    {
                        WorkingSetMB = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2),
                        PrivateMemoryMB = Math.Round(process.PrivateMemorySize64 / (1024.0 * 1024.0), 2),
                        GcMemoryMB = Math.Round(gcInfo / (1024.0 * 1024.0), 2),
                        Gen0Collections = GC.CollectionCount(0),
                        Gen1Collections = GC.CollectionCount(1),
                        Gen2Collections = GC.CollectionCount(2)
                    },
                    Performance = new
                    {
                        ProcessorCount = Environment.ProcessorCount,
                        TotalProcessorTimeMs = process.TotalProcessorTime.TotalMilliseconds,
                        UserProcessorTimeMs = process.UserProcessorTime.TotalMilliseconds
                    },
                    Configuration = new
                    {
                        Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                        FrameworkVersion = Environment.Version.ToString(),
                        StorageAccountConfigured = !string.IsNullOrEmpty(_configuration.GetConnectionString("StorageAccount"))
                    },
                    Application = new
                    {
                        Name = "cpu-app",
                        Version = "1.0.0",
                        ProcessId = process.Id,
                        MachineName = Environment.MachineName
                    }
                };

                _logger.LogInformation("Health diagnostics requested - Status: Healthy, Memory: {MemoryMB}MB, Uptime: {Uptime}", 
                    diagnostics.Memory.WorkingSetMB, diagnostics.Uptime);

                return Ok(diagnostics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving health diagnostics");
                return StatusCode(500, new { Status = "Unhealthy", Error = "Failed to retrieve diagnostics", Timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>
        /// Simple health check endpoint for load balancers and monitoring systems
        /// </summary>
        [HttpGet]
        [Route("status")]
        public ActionResult<object> GetStatus()
        {
            try
            {
                var status = new
                {
                    Status = "OK",
                    Timestamp = DateTime.UtcNow,
                    Service = "cpu-app"
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health status check failed");
                return StatusCode(500, new { Status = "ERROR", Timestamp = DateTime.UtcNow, Service = "cpu-app" });
            }
        }
    }
}