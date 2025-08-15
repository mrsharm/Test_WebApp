using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;
using System.Runtime;

namespace WebApp_AppService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiagnosticsController : ControllerBase
    {
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(ILogger<DiagnosticsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("system-info")]
        public ActionResult<object> GetSystemInfo()
        {
            _logger.LogInformation("System info requested");
            
            var process = Process.GetCurrentProcess();
            var gcInfo = GC.GetTotalMemory(false);
            
            var systemInfo = new
            {
                Timestamp = DateTime.UtcNow,
                Environment = new
                {
                    MachineName = Environment.MachineName,
                    ProcessorCount = Environment.ProcessorCount,
                    OSVersion = Environment.OSVersion.ToString(),
                    RuntimeVersion = Environment.Version.ToString(),
                    Framework = Assembly.GetEntryAssembly()?.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkName
                },
                Memory = new
                {
                    WorkingSet = process.WorkingSet64,
                    PrivateMemory = process.PrivateMemorySize64,
                    VirtualMemory = process.VirtualMemorySize64,
                    GCTotalMemory = gcInfo,
                    GCGen0Collections = GC.CollectionCount(0),
                    GCGen1Collections = GC.CollectionCount(1),
                    GCGen2Collections = GC.CollectionCount(2)
                },
                Process = new
                {
                    Id = process.Id,
                    StartTime = process.StartTime,
                    TotalProcessorTime = process.TotalProcessorTime,
                    ThreadCount = process.Threads.Count,
                    HandleCount = process.HandleCount
                }
            };

            return Ok(systemInfo);
        }

        [HttpGet]
        [Route("memory-pressure")]
        public ActionResult<object> GetMemoryPressure()
        {
            _logger.LogInformation("Memory pressure check requested");

            var process = Process.GetCurrentProcess();
            var gcBefore = GC.GetTotalMemory(false);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var gcAfter = GC.GetTotalMemory(false);

            var memoryInfo = new
            {
                Timestamp = DateTime.UtcNow,
                BeforeGC = gcBefore,
                AfterGC = gcAfter,
                Collected = gcBefore - gcAfter,
                WorkingSet = process.WorkingSet64,
                PrivateMemory = process.PrivateMemorySize64,
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                IsServerGC = GCSettings.IsServerGC,
                LatencyMode = GCSettings.LatencyMode.ToString()
            };

            if (gcBefore > 500 * 1024 * 1024) // 500MB
            {
                _logger.LogWarning("High memory usage detected: {MemoryUsage} bytes", gcBefore);
            }

            return Ok(memoryInfo);
        }

        [HttpGet]
        [Route("incident-logs")]
        public ActionResult<object> GetIncidentLogs()
        {
            _logger.LogInformation("Incident logs requested for contoso-chat-net");

            var incidentInfo = new
            {
                Timestamp = DateTime.UtcNow,
                ResourceDetails = new
                {
                    Name = "contoso-chat-net",
                    Type = "Web App",
                    Location = "westus",
                    ResourceGroup = "mrsharm-operations-agent-3p-rg",
                    SubscriptionId = "be8d491e-109c-4ee1-aaee-dc7615af0a42"
                },
                KnownIssues = new[]
                {
                    new
                    {
                        Endpoint = "/api/app/crash",
                        Severity = "Critical",
                        Description = "Intentionally causes OutOfMemory exception - infinite memory allocation",
                        Recommendation = "This endpoint should be disabled in production"
                    },
                    new
                    {
                        Endpoint = "/api/app/memleak/{kb}",
                        Severity = "High",
                        Description = "Causes memory leaks by accumulating objects in static cache",
                        Recommendation = "Monitor memory usage and restart application if necessary"
                    },
                    new
                    {
                        Endpoint = "/api/app/appinvoke",
                        Severity = "High",
                        Description = "Creates 2100 event subscribers that are never cleaned up",
                        Recommendation = "Causes gradual memory leak - monitor memory pressure"
                    },
                    new
                    {
                        Endpoint = "/api/app/work",
                        Severity = "Medium",
                        Description = "Creates high CPU usage with expensive mathematical operations",
                        Recommendation = "Monitor CPU usage and consider rate limiting"
                    },
                    new
                    {
                        Endpoint = "/api/app/test",
                        Severity = "Medium",
                        Description = "May throw exceptions if storage connection string not configured",
                        Recommendation = "Ensure proper configuration and exception handling"
                    }
                },
                SystemHealth = GetQuickHealthCheck()
            };

            return Ok(incidentInfo);
        }

        [HttpPost]
        [Route("force-gc")]
        public ActionResult<object> ForceGarbageCollection()
        {
            _logger.LogWarning("Manual garbage collection triggered");

            var beforeGC = GC.GetTotalMemory(false);
            var stopwatch = Stopwatch.StartNew();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            stopwatch.Stop();
            var afterGC = GC.GetTotalMemory(false);

            var result = new
            {
                Timestamp = DateTime.UtcNow,
                MemoryBeforeGC = beforeGC,
                MemoryAfterGC = afterGC,
                MemoryFreed = beforeGC - afterGC,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            };

            return Ok(result);
        }

        private object GetQuickHealthCheck()
        {
            var process = Process.GetCurrentProcess();
            var memory = GC.GetTotalMemory(false);
            
            return new
            {
                Status = memory > 1024 * 1024 * 1024 ? "Warning" : "Healthy", // 1GB threshold
                MemoryUsage = memory,
                ProcessorTime = process.TotalProcessorTime,
                ThreadCount = process.Threads.Count,
                LastChecked = DateTime.UtcNow
            };
        }
    }
}