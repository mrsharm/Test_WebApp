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
        [Route("dowork/{maxNumber?}")]
        public ActionResult<string> dowork(long? maxNumber = null)
        {
            long max = maxNumber ?? 100000; // Default to 100k if not specified
            var primes = new List<long>();
            
            // CPU-intensive prime number calculation - this is the problematic implementation
            // This will cause high CPU usage due to inefficient prime checking
            for (long i = 2; i <= max; i++)
            {
                if (IsPrime(i))
                {
                    primes.Add(i);
                }
            }

            return $"Found {primes.Count} prime numbers up to {max}. Last few primes: {string.Join(", ", primes.TakeLast(5))}";

            // Local function that causes high CPU usage - inefficient prime checking
            static bool IsPrime(long number)
            {
                if (number < 2) return false;
                if (number == 2) return true;
                if (number % 2 == 0) return false;

                // Inefficient: checking all odd numbers up to the number itself
                // This creates the CPU hotspot mentioned in the diagnostics
                // Time complexity: O(n) for each number being tested
                for (long i = 3; i < number; i += 2)
                {
                    if (number % i == 0)
                        return false;
                }
                return true;
            }
        }

        [HttpGet]
        [Route("dowork-optimized/{maxNumber?}")]
        public ActionResult<string> doworkOptimized(long? maxNumber = null)
        {
            long max = maxNumber ?? 100000; // Default to 100k if not specified
            var primes = new List<long>();
            
            // Optimized prime number calculation using Sieve of Eratosthenes
            // This algorithm is much more efficient for finding all primes up to a number
            if (max >= 2)
            {
                primes = SieveOfEratosthenes((int)Math.Min(max, int.MaxValue));
            }

            return $"Found {primes.Count} prime numbers up to {max}. Last few primes: {string.Join(", ", primes.TakeLast(5))}";
        }

        // Optimized Sieve of Eratosthenes algorithm
        private static List<long> SieveOfEratosthenes(int max)
        {
            var primes = new List<long>();
            var isPrime = new bool[max + 1];
            
            // Initialize all numbers as potentially prime
            for (int i = 2; i <= max; i++)
                isPrime[i] = true;

            // Sieve algorithm - mark multiples of each prime as composite
            for (int i = 2; i * i <= max; i++)
            {
                if (isPrime[i])
                {
                    // Mark all multiples of i as composite
                    for (int j = i * i; j <= max; j += i)
                        isPrime[j] = false;
                }
            }

            // Collect all prime numbers
            for (int i = 2; i <= max; i++)
            {
                if (isPrime[i])
                    primes.Add(i);
            }

            return primes;
        }
    }
}
