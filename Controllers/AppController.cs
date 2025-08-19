using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using WebApp_AppService.Configuration;

namespace WebApp_AppService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppController : ControllerBase
    {
        private readonly IConfiguration app;
        private readonly ILogger<AppController> _logger;
        private readonly CpuWorkloadSettings _cpuSettings;
        
        public AppController(IConfiguration configuration, ILogger<AppController> logger, IOptions<CpuWorkloadSettings> cpuSettings)
        {
            app = configuration;
            _logger = logger;
            _cpuSettings = cpuSettings.Value;
        }

        [HttpGet]
        [Route("appinvoke")]
        public ActionResult<string> appinvoke()
        {
            try
            {
                _logger.LogWarning("Event subscription memory leak simulation started");
                var startMemory = GC.GetTotalMemory(false);
                
                Subscriber.CreatePublishers();
                
                var endMemory = GC.GetTotalMemory(false);
                var memoryIncrease = (endMemory - startMemory) / 1024 / 1024; // MB
                
                _logger.LogWarning("Event subscription memory leak simulation completed. " +
                    "Memory increase: {MemoryIncreaseMB}MB, Total memory: {TotalMemoryMB}MB", 
                    memoryIncrease, endMemory / 1024 / 1024);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during event subscription memory leak simulation");
                Debug.WriteLine(ex.ToString());
                return "Error: " + ex.Message;
            }

            return "Created multiple subscribers to the publisher!";
        }
        [HttpGet]
        [Route("health")]
        public ActionResult<object> health()
        {
            var healthInfo = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow.ToString("O"),
                Environment = new
                {
                    ProcessorCount = Environment.ProcessorCount,
                    MachineName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    WorkingSet = GC.GetTotalMemory(false) / 1024 / 1024, // MB
                    Is64BitProcess = Environment.Is64BitProcess
                },
                Configuration = new
                {
                    CpuWorkload = new
                    {
                        MaxDurationSeconds = _cpuSettings.MaxDurationSeconds,
                        DefaultDurationSeconds = _cpuSettings.DefaultDurationSeconds,
                        MaxThreadCountOverride = _cpuSettings.MaxThreadCountOverride,
                        EnablePrimeCalculations = _cpuSettings.EnablePrimeCalculations,
                        LoggingEnabled = _cpuSettings.LoggingEnabled
                    }
                },
                Endpoints = new[]
                {
                    new { Name = "CPU Load Test", Path = "/api/app/work?durationInSeconds={seconds}", Description = "Generates CPU load for specified duration" },
                    new { Name = "Memory Leak Test", Path = "/api/app/memleak/{kb}", Description = "Creates memory leak of specified size" },
                    new { Name = "Crash Test", Path = "/api/app/crash", Description = "Forces out of memory crash" },
                    new { Name = "Event Leak Test", Path = "/api/app/appinvoke", Description = "Creates memory leak via event subscriptions" },
                    new { Name = "Storage Test", Path = "/api/app/test", Description = "Tests Azure Storage connectivity" }
                }
            };

            if (_cpuSettings.LoggingEnabled)
            {
                _logger.LogInformation("Health check requested. Status: Healthy, Memory: {MemoryMB}MB, Cores: {ProcessorCount}, " +
                    "CPU Max Duration: {MaxDurationSeconds}s", 
                    healthInfo.Environment.WorkingSet, Environment.ProcessorCount, _cpuSettings.MaxDurationSeconds);
            }

            return healthInfo;
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
            _logger.LogWarning("Memory leak simulation started. Requested size: {SizeKB}KB", kb);
            
            int it = (kb * 1000) / 100;
            var startMemory = GC.GetTotalMemory(false);
            
            for (int i = 0; i < it; i++)
            {
                p.ProcessTransaction(new Customer(Guid.NewGuid().ToString()));
            }

            var endMemory = GC.GetTotalMemory(false);
            var memoryIncrease = (endMemory - startMemory) / 1024 / 1024; // MB

            _logger.LogWarning("Memory leak simulation completed. Size: {SizeKB}KB, Memory increase: {MemoryIncreaseMB}MB, " +
                "Total memory: {TotalMemoryMB}MB", kb, memoryIncrease, endMemory / 1024 / 1024);

            return "success:memleak";
        }

        [HttpGet]
        [Route("work")]
        public async Task<ActionResult<string>> doWork(int? durationInSeconds)
        {
            var seconds = durationInSeconds ?? _cpuSettings.DefaultDurationSeconds;

            // Enforce maximum duration limit
            if (seconds > _cpuSettings.MaxDurationSeconds)
            {
                var errorMessage = $"Requested duration {seconds} seconds exceeds maximum allowed {_cpuSettings.MaxDurationSeconds} seconds";
                if (_cpuSettings.LoggingEnabled)
                {
                    _logger.LogWarning("CPU workload rejected: {ErrorMessage}", errorMessage);
                }
                return BadRequest(errorMessage);
            }

            var start = DateTime.UtcNow;
            var endTime = start.AddSeconds(seconds);

            if (_cpuSettings.LoggingEnabled)
            {
                _logger.LogInformation("CPU workload started. Duration: {DurationSeconds} seconds, Start: {StartTime}", 
                    seconds, start);
            }

            double result = 0;
            long iterations = 0;
            int threadCount = _cpuSettings.MaxThreadCountOverride ?? 
                ((Environment.ProcessorCount > 2) ? (int)Math.Ceiling((decimal)Environment.ProcessorCount / 2) : 1);
            object lockObj = new object();

            if (_cpuSettings.LoggingEnabled)
            {
                _logger.LogInformation("CPU workload configuration - Threads: {ThreadCount}, Available Cores: {ProcessorCount}, " +
                    "Prime calculations: {PrimeCalculationsEnabled}", 
                    threadCount, Environment.ProcessorCount, _cpuSettings.EnablePrimeCalculations);
            }

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

                        // Prime calculation (expensive) - only if enabled
                        if (_cpuSettings.EnablePrimeCalculations)
                        {
                            bool isPrime = IsPrime(localIterations % 10000 + 2);
                            if (isPrime) localResult += 1;
                        }

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
            
            var endActual = DateTime.UtcNow;
            var actualDuration = (endActual - start).TotalSeconds;
            
            if (_cpuSettings.LoggingEnabled)
            {
                _logger.LogInformation("CPU workload completed. Iterations: {Iterations:N0}, Result: {Result:F2}, " +
                    "Requested Duration: {RequestedDuration}s, Actual Duration: {ActualDuration:F2}s, " +
                    "Iterations/Second: {IterationsPerSecond:N0}, Prime calculations: {PrimeCalculationsEnabled}",
                    iterations, result, seconds, actualDuration, (long)(iterations / actualDuration), _cpuSettings.EnablePrimeCalculations);
            }

            return $"High CPU task completed! Iterations: {iterations:N0}, Result: {result:F2} for Duration: {durationInSeconds}";
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
        [Route("crash")]
        public ActionResult<string> crash()
        {
            _logger.LogCritical("OUT OF MEMORY CRASH SIMULATION STARTED - Application will crash intentionally");
            
            double bytesSize = 0;
            var startMemory = GC.GetTotalMemory(false);
            int allocations = 0;
            
            while (true || bytesSize < 1_000_000)
            {
                try
                {
                    bytesSize += 10 * 1024 * 1024; // 10MB
                    memoryHog.Add(new byte[10 * 1024 * 1024]); // Allocate 10MB
                    allocations++;
                    
                    if (allocations % 10 == 0) // Log every 100MB allocated
                    {
                        var currentMemory = GC.GetTotalMemory(false);
                        _logger.LogCritical("OOM simulation progress: {AllocationsCount} allocations, " +
                            "{TotalAllocatedMB}MB total allocated, Current memory: {CurrentMemoryMB}MB", 
                            allocations, bytesSize / 1024 / 1024, currentMemory / 1024 / 1024);
                    }
                }
                catch (OutOfMemoryException ex)
                {
                    _logger.LogCritical(ex, "OUT OF MEMORY EXCEPTION OCCURRED - Application crashing as expected");
                    throw;
                }
            }

            return "success:oomd";
        }
    }
}
