using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace WebApp_AppService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppController : ControllerBase
    {
        private readonly IConfiguration app;
        private readonly ILogger<AppController> _logger;
        
        public AppController(IConfiguration configuration, ILogger<AppController> logger)
        {
            app = configuration;
            _logger = logger;
        }

        [HttpGet]
        [Route("appinvoke")]
        public ActionResult<string> appinvoke()
        {
            _logger.LogWarning("AppInvoke endpoint called - creates memory leak with 2100 subscribers");
            
            try
            {
                Subscriber.CreatePublishers();
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AppInvoke endpoint");
                Debug.WriteLine(ex.ToString());
                return "Error: " + ex.Message;
            }

            return "Created multiple subscribers to the publisher!";
        }
        private static Processor p = new Processor();

        class Customer
        {
            private string id;

            public Customer(string id)
            {
                this.id = id;
            }
        }

        class CustomerCache
        {
            private List<Customer> cache = new List<Customer>();

            public void AddCustomer(Customer c)
            {
                cache.Add(c);
            }
        }

        class Processor
        {
            private CustomerCache cache = new CustomerCache();

            public void ProcessTransaction(Customer customer)
            {
                cache.AddCustomer(customer);
            }
        }

        [HttpGet]
        [Route("memleak/{kb}")]
        public ActionResult<string> memleak(int kb)
        {
            _logger.LogWarning("MemLeak endpoint called with {KB}KB - creates memory leak via static processor", kb);
            
            int it = (kb * 1000) / 100;
            for (int i = 0; i < it; i++)
            {
                p.ProcessTransaction(new Customer(Guid.NewGuid().ToString()));
            }

            return "success:memleak";
        }

        [HttpGet]
        [Route("work")]
        public async Task<ActionResult<string>> doWork(int? durationInSeconds)
        {
            var seconds = durationInSeconds ?? 10;
            _logger.LogWarning("High CPU work endpoint called for {Seconds} seconds - will consume significant CPU", seconds);
            
            var start = DateTime.UtcNow;
            var endTime = start.AddSeconds(seconds);

            double result = 0;
            long iterations = 0;
            int threadCount = (Environment.ProcessorCount > 2) ? (int)Math.Ceiling((decimal)Environment.ProcessorCount / 2) : 1;
            object lockObj = new object();

            var tasks = new List<Task>();

            for (int i = 0; i < threadCount; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    double localResult = 0;
                    long localIterations = 0;

                    while (DateTime.UtcNow < endTime)
                    {
                        // More expensive operations
                        localResult += Math.Pow(Math.Sin(localIterations), 2) + Math.Cos(localIterations);
                        localResult += Math.Sqrt(Math.Abs(localResult));
                        localResult += Math.Log(Math.Abs(localResult) + 1);

                        // Prime calculation (expensive)
                        bool isPrime = IsPrime(localIterations % 10000 + 2);
                        if (isPrime) localResult += 1;

                        localIterations++;
                    }

                    lock (lockObj)
                    {
                        result += localResult;
                        iterations += localIterations;
                    }
                }));
            }

            bool IsPrime(long number)
            {
                if (number < 2) return false;
                for (long i = 2; i <= Math.Sqrt(number); i++)
                {
                    if (number % i == 0) return false;
                }
                return true;
            }

            await Task.WhenAll(tasks);
            _logger.LogInformation("High CPU work completed: {Iterations} iterations, {ThreadCount} threads", iterations, threadCount);
            return $"High CPU task completed! Iterations: {iterations:N0}, Result: {result:F2} for Duration: {seconds}";
        }

        [HttpGet]
        [Route("test")]
        public ActionResult<string> sayhello()
        {
            var connectionString = app.GetConnectionString("StorageAccount");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Storage account connection string is not configured.");
            }

            try
            {
                var blobServiceClient = new BlobServiceClient(connectionString);
                var accountInfo = blobServiceClient.GetAccountInfo();
                return "Hello, the storage account is connected successfully! Account Name: "; 
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to connect to storage account: {ex.Message}", ex);
            }
        }

        private static readonly List<byte[]> memoryHog = new();

        [HttpGet]
        [Route("diagnostics")]
        public ActionResult<object> diagnostics()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var gcInfo = GC.GetTotalMemory(false);
            
            return new
            {
                ProcessInfo = new
                {
                    ProcessId = process.Id,
                    ProcessName = process.ProcessName,
                    StartTime = process.StartTime,
                    WorkingSet64 = $"{process.WorkingSet64 / (1024 * 1024):F1}MB",
                    PrivateMemorySize64 = $"{process.PrivateMemorySize64 / (1024 * 1024):F1}MB"
                },
                MemoryInfo = new
                {
                    GCTotalMemory = $"{gcInfo / (1024 * 1024):F1}MB",
                    Gen0Collections = GC.CollectionCount(0),
                    Gen1Collections = GC.CollectionCount(1),  
                    Gen2Collections = GC.CollectionCount(2),
                    MemoryHogCount = memoryHog.Count,
                    MemoryHogSize = $"{memoryHog.Count * 10:F1}MB"
                },
                SystemInfo = new
                {
                    ProcessorCount = Environment.ProcessorCount,
                    MachineName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    UpTime = DateTime.UtcNow - process.StartTime
                }
            };
        }

        [HttpGet]
        [Route("health")]
        public ActionResult<object> health()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var workingSetMB = process.WorkingSet64 / (1024 * 1024);
            var gcMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
            
            var status = "healthy";
            var warnings = new List<string>();
            
            if (workingSetMB > 500)
            {
                status = "warning";
                warnings.Add($"High memory usage: {workingSetMB:F1}MB");
            }
            
            if (memoryHog.Count > 50)
            {
                status = "warning";
                warnings.Add($"Memory hog active: {memoryHog.Count} allocations");
            }

            return new
            {
                Status = status,
                Timestamp = DateTime.UtcNow,
                Warnings = warnings,
                Metrics = new
                {
                    WorkingSetMB = $"{workingSetMB:F1}MB",
                    GCMemoryMB = $"{gcMemoryMB:F1}MB"
                }
            };
        }

        [HttpGet]
        [Route("crash")]
        public ActionResult<string> crash()
        {
            _logger.LogWarning("Crash endpoint called - will allocate significant memory");
            
            double bytesSize = 0;
            int maxAllocations = 100; // Limit to prevent infinite loop
            int allocations = 0;
            
            while (bytesSize < 1_000_000_000 && allocations < maxAllocations) // 1GB limit and allocation limit
            {
                bytesSize += 10 * 1024 * 1024; // 10MB
                memoryHog.Add(new byte[10 * 1024 * 1024]); // Allocate 10MB
                allocations++;
            }

            _logger.LogWarning("Crash endpoint completed: {Allocations} allocations, {SizeMB}MB", allocations, bytesSize / (1024 * 1024));
            return $"success:allocated {allocations} chunks, total size: {bytesSize / (1024 * 1024):F1}MB";
        }

        [HttpPost]
        [Route("cleanup")]
        public ActionResult<object> cleanup()
        {
            _logger.LogInformation("Cleanup endpoint called - releasing memory hog allocations");
            
            var beforeCount = memoryHog.Count;
            var beforeMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
            
            memoryHog.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var afterMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
            
            return new
            {
                Status = "success",
                Timestamp = DateTime.UtcNow,
                ClearedAllocations = beforeCount,
                MemoryBefore = $"{beforeMemoryMB}MB",
                MemoryAfter = $"{afterMemoryMB}MB",
                MemoryFreed = $"{Math.Max(0, beforeMemoryMB - afterMemoryMB)}MB"
            };
        }
    }
}
