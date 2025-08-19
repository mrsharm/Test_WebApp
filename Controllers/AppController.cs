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
        public AppController(IConfiguration configuration)
        {
            app = configuration;
        }

        [HttpGet]
        [Route("appinvoke")]
        public ActionResult<string> appinvoke()
        {
            try
            {
                Subscriber.CreatePublishers();
            }

            catch (Exception ex)
            {
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
            double bytesSize = 0;
            while (true || bytesSize < 1_000_000)
            {
                bytesSize += 10 * 1024 * 1024; // 10MB
                memoryHog.Add(new byte[10 * 1024 * 1024]); // Allocate 1MB
            }

            return "success:oomd";
        }

        [HttpGet]
        [Route("health")]
        public ActionResult<object> GetHealthStatus()
        {
            var healthStatus = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                WorkingSet = Environment.WorkingSet,
                Scaling = new
                {
                    CurrentInstances = 1, // App Service always reports as 1 for single instance
                    MinimumInstances = 1, // Free tier minimum
                    MaximumInstances = 1, // Free tier maximum
                    Tier = "Free",
                    CanScaleDown = false,
                    CanScaleUp = false,
                    ScalingPolicy = "Manual - Free tier does not support auto-scaling"
                }
            };
            
            return Ok(healthStatus);
        }

        [HttpGet]
        [Route("scaling-status")]
        public ActionResult<object> GetScalingStatus()
        {
            var scalingInfo = new
            {
                AppName = "cpu-app",
                ServicePlan = new
                {
                    Name = "cpu-app20250714124552Plan",
                    Tier = "Free",
                    Sku = "F1"
                },
                CurrentState = new
                {
                    InstanceCount = 1,
                    IsAtMinimum = true,
                    IsAtMaximum = true
                },
                ScalingLimits = new
                {
                    MinimumInstances = 1,
                    MaximumInstances = 1,
                    ScaleDownPossible = false,
                    ScaleUpPossible = false,
                    Reason = "Free tier (F1) does not support scaling. Minimum and maximum instances are both 1."
                },
                Recommendations = new[]
                {
                    "App is already at minimum instance count for Free tier",
                    "To enable scaling, upgrade to Basic (B1) or higher tier",
                    "Scale-down operations will always fail on Free tier"
                },
                Timestamp = DateTime.UtcNow,
                StatusForSRE = "EXPECTED_STATE_NO_ACTION_NEEDED"
            };
            
            return Ok(scalingInfo);
        }
    }
}
