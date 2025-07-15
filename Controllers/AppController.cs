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
        [Route("test")]
        public ActionResult<string> sayhello()
        {
            var connectionString = app.GetConnectionString("StorageAccount");
            if (string.IsNullOrEmpty(connectionString))
            {
                return BadRequest(new { 
                    error = "Storage account connection string is not configured.", 
                    details = "Please configure the 'StorageAccount' connection string in app settings or appsettings.json",
                    configurationKey = "ConnectionStrings:StorageAccount"
                });
            }

            try
            {
                var blobServiceClient = new BlobServiceClient(connectionString);
                var accountInfo = blobServiceClient.GetAccountInfo();
                return "Hello, the storage account is connected successfully! Account Name: "; 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = "Failed to connect to storage account", 
                    details = ex.Message,
                    connectionString = connectionString.Length > 20 ? connectionString.Substring(0, 20) + "..." : connectionString
                });
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
    }
}
