using Microsoft.AspNetCore.Mvc;

namespace WebApp_AppService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        /// <summary>
        /// Extracts a critical segment from the User-Agent header for security audit purposes.
        /// This method implements defensive parsing to handle malformed User-Agent strings.
        /// </summary>
        /// <returns>Extracted segment or safe default</returns>
        [HttpGet]
        [Route("admin")]
        public ActionResult<string> GetAdminDashboard()
        {
            var userAgent = Request.Headers["User-Agent"].ToString();
            var segment = ExtractCriticalSegment(userAgent);
            
            return Ok(new
            {
                Message = "Admin Dashboard",
                UserAgentSegment = segment,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Extracts the critical segment from a User-Agent string with defensive parsing.
        /// Expected format: "Mozilla/5.0 (Platform) Engine/Version"
        /// Extracts the Platform portion or returns a safe default.
        /// </summary>
        /// <param name="userAgent">The User-Agent string to parse</param>
        /// <returns>The extracted platform segment or "Unknown"</returns>
        public string ExtractCriticalSegment(string? userAgent)
        {
            // Defensive check: handle null or empty input
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return "Unknown";
            }

            try
            {
                // Split by common delimiters: parentheses and spaces
                var openParenIndex = userAgent.IndexOf('(');
                var closeParenIndex = userAgent.IndexOf(')');

                // Validate that parentheses exist and are in the correct order
                if (openParenIndex == -1 || closeParenIndex == -1 || closeParenIndex <= openParenIndex)
                {
                    // Fallback: try to extract first meaningful token
                    var tokens = userAgent.Split(new[] { ' ', '/', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    return tokens.Length > 0 ? tokens[0] : "Unknown";
                }

                // Extract content between parentheses
                var platform = userAgent.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1);

                // Further parse the platform string if needed
                var platformTokens = platform.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Return first non-empty token or the whole platform string
                if (platformTokens.Length > 0)
                {
                    return platformTokens[0].Trim();
                }

                return platform.Trim();
            }
            catch (Exception)
            {
                // If any unexpected error occurs during parsing, return safe default
                return "Unknown";
            }
        }
    }
}
