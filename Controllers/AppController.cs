using Microsoft.AspNetCore.Mvc;

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
            Subscriber.CreatePublishers();
            return "Created multiple subscribers to the publisher!";
        }

        [HttpGet]
        [Route("sayhello")]
        public ActionResult<string> sayhello()
        {
            return "Hello World!";
        }
    }
}
