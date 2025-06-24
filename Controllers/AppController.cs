using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace WebApp_AppService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppController : ControllerBase
    {

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

            public void Clear()
            {
                cache.Clear();
            }
        }

        class Processor
        {
            private CustomerCache cache = new CustomerCache();

            public void ProcessTransaction(Customer customer)
            {
                cache.AddCustomer(customer);
            }

            public void ClearCache()
            {
                cache.Clear();
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

            // Clear the cache to prevent memory accumulation
            p.ClearCache();

            return "success:memleak";
        }

        [HttpGet]
        [Route("sayhello")]
        public ActionResult<string> sayhello()
        {
            return "Hello World!";
        }

        [HttpGet]
        [Route("crash")]
        public ActionResult<string> crash()
        {
            // Use local variable instead of static to allow garbage collection
            var localMemoryHog = new List<byte[]>();
            double bytesSize = 0;
            while (true || bytesSize < 1_000_000)
            {
                bytesSize += 10 * 1024 * 1024; // 10MB
                localMemoryHog.Add(new byte[10 * 1024 * 1024]); // Allocate 10MB (comment was incorrect)
            }
            // localMemoryHog will be eligible for GC when method exits

            return "success:oomd";
        }
    }
}
