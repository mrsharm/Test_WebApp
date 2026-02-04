using Microsoft.AspNetCore.Mvc;

namespace WebApp_AppService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        /// <summary>
        /// Extracts a critical segment from User-Agent string with proper bounds checking
        /// to prevent IndexOutOfRangeException
        /// </summary>
        /// <param name="userAgent">The User-Agent string to parse</param>
        /// <returns>Critical segment or a safe default value</returns>
        private string ExtractCriticalSegment(string? userAgent)
        {
            // Defensive check: Handle null or empty User-Agent
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return "Unknown";
            }

            try
            {
                // Split by common delimiters in User-Agent strings
                var segments = userAgent.Split(new[] { ' ', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Bounds check: Ensure we have at least one segment
                if (segments.Length == 0)
                {
                    return "Unknown";
                }

                // Safely access the first segment
                var criticalSegment = segments[0];

                // Additional validation: Ensure the segment is not empty after trim
                criticalSegment = criticalSegment.Trim();
                if (string.IsNullOrEmpty(criticalSegment))
                {
                    return "Unknown";
                }

                // Limit the length to prevent potential buffer issues
                const int maxLength = 100;
                if (criticalSegment.Length > maxLength)
                {
                    criticalSegment = criticalSegment.Substring(0, maxLength);
                }

                return criticalSegment;
            }
            catch (ArgumentOutOfRangeException)
            {
                // Handle substring operations that may exceed bounds
                return "Unknown";
            }
            catch (ArgumentException)
            {
                // Handle specific exceptions that could occur during string operations
                return "Unknown";
            }
        }

        /// <summary>
        /// Admin dashboard endpoint that processes User-Agent information
        /// </summary>
        /// <returns>Dashboard information including parsed User-Agent segment</returns>
        [HttpGet]
        [Route("admin")]
        public ActionResult<object> GetAdminDashboard()
        {
            try
            {
                // Safely retrieve User-Agent from request headers
                var userAgent = Request.Headers["User-Agent"].ToString();
                
                // Extract critical segment with bounds checking
                var criticalSegment = ExtractCriticalSegment(userAgent);

                return Ok(new
                {
                    Status = "Success",
                    Message = "Admin dashboard loaded successfully",
                    UserAgentSegment = criticalSegment,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception)
            {
                // Safety net for any unexpected exceptions during request processing
                // ExtractCriticalSegment has its own specific exception handling
                // In production, log the exception details server-side
                // Do not expose internal exception details to clients
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "Failed to load admin dashboard"
                });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet]
        [Route("health")]
        public ActionResult<object> GetHealth()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
