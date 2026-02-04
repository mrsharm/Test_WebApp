using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApp_AppService.Controllers;
using Xunit;

namespace WebApp_AppService.Tests
{
    /// <summary>
    /// Unit tests for DashboardController focusing on User-Agent parsing
    /// and boundary conditions to prevent IndexOutOfRangeException
    /// </summary>
    public class DashboardControllerTests
    {
        private DashboardController CreateControllerWithUserAgent(string? userAgent)
        {
            var controller = new DashboardController();
            var httpContext = new DefaultHttpContext();
            
            if (userAgent != null)
            {
                httpContext.Request.Headers["User-Agent"] = userAgent;
            }
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            
            return controller;
        }

        [Fact]
        public void GetAdminDashboard_WithValidUserAgent_ReturnsSuccess()
        {
            // Arrange
            var controller = CreateControllerWithUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            // Act
            var result = controller.GetAdminDashboard();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
            
            var response = okResult.Value;
            var statusProperty = response.GetType().GetProperty("Status");
            Assert.NotNull(statusProperty);
            Assert.Equal("Success", statusProperty.GetValue(response));
        }

        [Fact]
        public void GetAdminDashboard_WithNullUserAgent_DoesNotThrowException()
        {
            // Arrange
            var controller = CreateControllerWithUserAgent(null);

            // Act
            var result = controller.GetAdminDashboard();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void GetAdminDashboard_WithEmptyUserAgent_DoesNotThrowException()
        {
            // Arrange
            var controller = CreateControllerWithUserAgent("");

            // Act
            var result = controller.GetAdminDashboard();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void GetAdminDashboard_WithWhitespaceUserAgent_DoesNotThrowException()
        {
            // Arrange
            var controller = CreateControllerWithUserAgent("   ");

            // Act
            var result = controller.GetAdminDashboard();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void GetAdminDashboard_WithShortUserAgent_DoesNotThrowException()
        {
            // Arrange
            var controller = CreateControllerWithUserAgent("A");

            // Act
            var result = controller.GetAdminDashboard();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void GetAdminDashboard_WithMalformedUserAgent_DoesNotThrowException()
        {
            // Arrange - User-Agent with only special characters
            var controller = CreateControllerWithUserAgent("///()()()");

            // Act
            var result = controller.GetAdminDashboard();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void GetAdminDashboard_WithCorruptedUserAgent_DoesNotThrowException()
        {
            // Arrange - Simulating corrupted/unexpected format
            var controller = CreateControllerWithUserAgent("@#$%^&*()");

            // Act
            var result = controller.GetAdminDashboard();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void GetAdminDashboard_WithVeryLongUserAgent_DoesNotThrowException()
        {
            // Arrange - Very long User-Agent string
            var longUserAgent = new string('A', 10000);
            var controller = CreateControllerWithUserAgent(longUserAgent);

            // Act
            var result = controller.GetAdminDashboard();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void GetAdminDashboard_WithMultipleSlashes_DoesNotThrowException()
        {
            // Arrange
            var controller = CreateControllerWithUserAgent("///");

            // Act
            var result = controller.GetAdminDashboard();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void GetAdminDashboard_WithOnlyParentheses_DoesNotThrowException()
        {
            // Arrange
            var controller = CreateControllerWithUserAgent("()()()");

            // Act
            var result = controller.GetAdminDashboard();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void GetAdminDashboard_WithCommonBrowserUserAgents_ReturnsSuccess()
        {
            // Arrange - Test with multiple common browser User-Agents
            var userAgents = new[]
            {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.1 Safari/605.1.15",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.107 Safari/537.36",
                "Mozilla/5.0 (iPhone; CPU iPhone OS 14_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.0 Mobile/15E148 Safari/604.1"
            };

            foreach (var userAgent in userAgents)
            {
                var controller = CreateControllerWithUserAgent(userAgent);

                // Act
                var result = controller.GetAdminDashboard();

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                Assert.NotNull(okResult.Value);
            }
        }

        [Fact]
        public void GetHealth_ReturnsHealthy()
        {
            // Arrange
            var controller = new DashboardController();

            // Act
            var result = controller.GetHealth();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
            
            var response = okResult.Value;
            var statusProperty = response.GetType().GetProperty("Status");
            Assert.NotNull(statusProperty);
            Assert.Equal("Healthy", statusProperty.GetValue(response));
        }

        [Fact]
        public void GetAdminDashboard_WithBotUserAgent_DoesNotThrowException()
        {
            // Arrange - Bot/crawler User-Agent
            var controller = CreateControllerWithUserAgent("Googlebot/2.1 (+http://www.google.com/bot.html)");

            // Act
            var result = controller.GetAdminDashboard();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void GetAdminDashboard_WithMinimalUserAgent_DoesNotThrowException()
        {
            // Arrange - Minimal valid User-Agent
            var controller = CreateControllerWithUserAgent("curl/7.68.0");

            // Act
            var result = controller.GetAdminDashboard();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }
    }
}
