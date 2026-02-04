using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace WebApp_AppService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ILogger<DashboardController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Admin dashboard endpoint that displays security audit information
        /// </summary>
        [HttpGet]
        [Route("admin")]
        public ActionResult<object> GetAdminDashboard()
        {
            try
            {
                var userAgent = Request.Headers["User-Agent"].ToString();
                var criticalSegment = ExtractCriticalSegment(userAgent);
                
                return new
                {
                    Status = "OK",
                    Timestamp = DateTime.UtcNow,
                    UserAgent = userAgent,
                    CriticalSegment = criticalSegment,
                    Message = "Admin dashboard loaded successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in admin dashboard");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Extracts critical segment from User-Agent for security audit.
        /// Fixed version with defensive parsing to prevent IndexOutOfRangeException.
        /// </summary>
        private string ExtractCriticalSegment(string? userAgent)
        {
            // Defensive check: handle null or empty input
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                _logger.LogWarning("User-Agent header is null or empty, using default value");
                return "Unknown";
            }

            try
            {
                // Parse User-Agent format: Mozilla/5.0 (Platform) Browser/Version
                // Expected format has parentheses with platform info
                var startIndex = userAgent.IndexOf('(');
                var endIndex = userAgent.IndexOf(')');

                // Bounds check: ensure both parentheses exist and are in correct order
                if (startIndex == -1 || endIndex == -1 || startIndex >= endIndex)
                {
                    _logger.LogWarning("User-Agent does not contain valid parentheses structure: {UserAgent}", userAgent);
                    return "Malformed";
                }

                // Extract platform segment with bounds validation
                var platformSegment = userAgent.Substring(startIndex + 1, endIndex - startIndex - 1);
                
                // Split platform segment and validate array access
                var parts = platformSegment.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                
                // Defensive array access with bounds check
                if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
                {
                    _logger.LogWarning("User-Agent platform segment is empty: {UserAgent}", userAgent);
                    return "Empty";
                }

                // Safely access first element (primary platform identifier)
                var criticalPart = parts[0];
                
                // Additional validation for browser segment if available
                if (parts.Length > 2)
                {
                    // Try to get browser info from third segment if it exists
                    var browserInfo = parts[2];
                    return $"{criticalPart}/{browserInfo}";
                }

                return criticalPart;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // This should not happen with our bounds checks, but log if it does
                _logger.LogError(ex, "Unexpected ArgumentOutOfRangeException in User-Agent parsing: {UserAgent}", userAgent);
                return "ParseError";
            }
            catch (Exception ex)
            {
                // Catch any other unexpected parsing errors
                _logger.LogError(ex, "Unexpected error parsing User-Agent: {UserAgent}", userAgent);
                return "Error";
            }
        }
    }
}
