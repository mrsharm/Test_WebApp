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
            _logger.LogWarning("DANGEROUS ENDPOINT: appinvoke called - creates memory leak with 2100 event subscribers");
            
            try
            {
                Subscriber.CreatePublishers();
                _logger.LogError("Memory leak created: 2100 subscribers with 1MB each = ~2.1GB memory leak");
                return "Created multiple subscribers to the publisher! WARNING: This creates a memory leak.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in appinvoke endpoint");
                Debug.WriteLine(ex.ToString());
                return "Error: " + ex.Message;
            }
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
            _logger.LogWarning("DANGEROUS ENDPOINT: memleak called with {KB}KB - will cause memory leak", kb);
            
            try
            {
                int it = (kb * 1000) / 100;
                _logger.LogWarning("Adding {Iterations} customer objects to static cache (memory leak)", it);
                
                for (int i = 0; i < it; i++)
                {
                    p.ProcessTransaction(new Customer(Guid.NewGuid().ToString()));
                }

                _logger.LogError("Memory leak created: {KB}KB worth of objects added to static cache", kb);
                return $"success:memleak - Added {it} objects to cache (WARNING: Memory leak created)";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in memleak endpoint with {KB}KB", kb);
                return "Error: " + ex.Message;
            }
        }

        [HttpGet]
        [Route("work")]
        public async Task<ActionResult<string>> doWork(int? durationInSeconds)
        {
            var seconds = durationInSeconds ?? 10;
            _logger.LogWarning("High CPU work started for {Seconds} seconds", seconds);
            
            try
            {
                var start = DateTime.UtcNow;
                var endTime = start.AddSeconds(seconds);

                double result = 0;
                long iterations = 0;
                int threadCount = (Environment.ProcessorCount > 2) ? (int)Math.Ceiling((decimal)Environment.ProcessorCount / 2) : 1;
                object lockObj = new object();

                _logger.LogInformation("Starting high CPU task with {ThreadCount} threads for {Seconds}s", threadCount, seconds);

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
                
                _logger.LogWarning("High CPU work completed: {Iterations:N0} iterations, {Result:F2} result", iterations, result);
                return $"High CPU task completed! Iterations: {iterations:N0}, Result: {result:F2} for Duration: {durationInSeconds}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in work endpoint with duration {Seconds}", seconds);
                return "Error: " + ex.Message;
            }
        }

        [HttpGet]
        [Route("test")]
        public ActionResult<string> sayhello()
        {
            _logger.LogInformation("Storage connection test requested");
            
            try
            {
                var connectionString = app.GetConnectionString("StorageAccount");
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("Storage account connection string is not configured");
                    throw new InvalidOperationException("Storage account connection string is not configured.");
                }

                var blobServiceClient = new BlobServiceClient(connectionString);
                var accountInfo = blobServiceClient.GetAccountInfo();
                
                _logger.LogInformation("Storage account connection successful");
                return "Hello, the storage account is connected successfully! Account Name: "; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to storage account");
                throw new InvalidOperationException($"Failed to connect to storage account: {ex.Message}", ex);
            }
        }

        private static readonly List<byte[]> memoryHog = new();

        [HttpGet]
        [Route("crash")]
        public ActionResult<string> crash()
        {
            _logger.LogCritical("CRITICAL DANGER: Crash endpoint called - will cause OutOfMemory exception and application crash");
            
            try
            {
                double bytesSize = 0;
                int iterations = 0;
                
                while (true || bytesSize < 1_000_000)
                {
                    bytesSize += 10 * 1024 * 1024; // Track 10MB
                    memoryHog.Add(new byte[10 * 1024 * 1024]); // Allocate 10MB
                    iterations++;
                    
                    if (iterations % 10 == 0) // Log every 100MB
                    {
                        _logger.LogCritical("Memory allocation: {SizeMB}MB allocated, iteration {Iterations}", 
                            bytesSize / (1024 * 1024), iterations);
                    }
                }

                return "success:oomd";
            }
            catch (OutOfMemoryException ex)
            {
                _logger.LogCritical(ex, "OutOfMemory exception occurred as expected in crash endpoint");
                return "OutOfMemory exception occurred - application may crash";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception in crash endpoint");
                return "Unexpected error: " + ex.Message;
            }
        }
    }
}
