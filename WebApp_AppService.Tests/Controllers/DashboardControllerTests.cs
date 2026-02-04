using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApp_AppService.Controllers;
using Xunit;

namespace WebApp_AppService.Tests.Controllers
{
    public class DashboardControllerTests
    {
        private DashboardController CreateController()
        {
            var controller = new DashboardController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }

        [Fact]
        public void ExtractCriticalSegment_ValidUserAgent_ReturnsExpectedSegment()
        {
            // Arrange
            var controller = CreateController();
            var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

            // Act
            var result = controller.ExtractCriticalSegment(userAgent);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Windows NT 10.0", result);
        }

        [Fact]
        public void ExtractCriticalSegment_NullUserAgent_ReturnsUnknown()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.ExtractCriticalSegment(null);

            // Assert
            Assert.Equal("Unknown", result);
        }

        [Fact]
        public void ExtractCriticalSegment_EmptyUserAgent_ReturnsUnknown()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.ExtractCriticalSegment("");

            // Assert
            Assert.Equal("Unknown", result);
        }

        [Fact]
        public void ExtractCriticalSegment_WhitespaceUserAgent_ReturnsUnknown()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.ExtractCriticalSegment("   ");

            // Assert
            Assert.Equal("Unknown", result);
        }

        [Fact]
        public void ExtractCriticalSegment_NoParentheses_ReturnsFallback()
        {
            // Arrange
            var controller = CreateController();
            var userAgent = "SimpleBot/1.0";

            // Act
            var result = controller.ExtractCriticalSegment(userAgent);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("SimpleBot", result);
        }

        [Fact]
        public void ExtractCriticalSegment_OnlyOpeningParenthesis_ReturnsFallback()
        {
            // Arrange
            var controller = CreateController();
            var userAgent = "Mozilla/5.0 (Windows";

            // Act
            var result = controller.ExtractCriticalSegment(userAgent);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Mozilla", result);
        }

        [Fact]
        public void ExtractCriticalSegment_OnlyClosingParenthesis_ReturnsFallback()
        {
            // Arrange
            var controller = CreateController();
            var userAgent = "Mozilla/5.0 Windows)";

            // Act
            var result = controller.ExtractCriticalSegment(userAgent);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Mozilla", result);
        }

        [Fact]
        public void ExtractCriticalSegment_EmptyParentheses_ReturnsEmptyOrUnknown()
        {
            // Arrange
            var controller = CreateController();
            var userAgent = "Mozilla/5.0 () AppleWebKit";

            // Act
            var result = controller.ExtractCriticalSegment(userAgent);

            // Assert
            Assert.NotNull(result);
            // Should return empty string trimmed or handle gracefully
        }

        [Fact]
        public void ExtractCriticalSegment_MobileUserAgent_ReturnsExpectedSegment()
        {
            // Arrange
            var controller = CreateController();
            var userAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 14_6 like Mac OS X) AppleWebKit/605.1.15";

            // Act
            var result = controller.ExtractCriticalSegment(userAgent);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("iPhone", result);
        }

        [Fact]
        public void ExtractCriticalSegment_AndroidUserAgent_ReturnsExpectedSegment()
        {
            // Arrange
            var controller = CreateController();
            var userAgent = "Mozilla/5.0 (Linux; Android 11; SM-G991B) AppleWebKit/537.36";

            // Act
            var result = controller.ExtractCriticalSegment(userAgent);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Linux", result);
        }

        [Fact]
        public void ExtractCriticalSegment_MacOSUserAgent_ReturnsExpectedSegment()
        {
            // Arrange
            var controller = CreateController();
            var userAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15";

            // Act
            var result = controller.ExtractCriticalSegment(userAgent);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Macintosh", result);
        }

        [Fact]
        public void ExtractCriticalSegment_LinuxUserAgent_ReturnsExpectedSegment()
        {
            // Arrange
            var controller = CreateController();
            var userAgent = "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:89.0) Gecko/20100101";

            // Act
            var result = controller.ExtractCriticalSegment(userAgent);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("X11", result);
        }

        [Fact]
        public void ExtractCriticalSegment_BotUserAgent_HandlesGracefully()
        {
            // Arrange
            var controller = CreateController();
            var userAgent = "Googlebot/2.1 (+http://www.google.com/bot.html)";

            // Act
            var result = controller.ExtractCriticalSegment(userAgent);

            // Assert
            Assert.NotNull(result);
            // Should handle bot UA gracefully without throwing
        }

        [Fact]
        public void ExtractCriticalSegment_MalformedWithSpecialChars_HandlesGracefully()
        {
            // Arrange
            var controller = CreateController();
            var userAgent = "Mozilla/5.0 (Windows<>NT;\"10.0\") Test";

            // Act
            var result = controller.ExtractCriticalSegment(userAgent);

            // Assert
            Assert.NotNull(result);
            // Should not throw exception
        }

        [Fact]
        public void ExtractCriticalSegment_SingleCharacter_HandlesGracefully()
        {
            // Arrange
            var controller = CreateController();
            var userAgent = "A";

            // Act
            var result = controller.ExtractCriticalSegment(userAgent);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("A", result);
        }

        [Fact]
        public void ExtractCriticalSegment_VeryLongUserAgent_HandlesGracefully()
        {
            // Arrange
            var controller = CreateController();
            var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " + new string('A', 10000);

            // Act
            var result = controller.ExtractCriticalSegment(userAgent);

            // Assert
            Assert.NotNull(result);
            // Should handle long strings without throwing
        }

        [Fact]
        public void GetAdminDashboard_WithUserAgent_ReturnsOkResult()
        {
            // Arrange
            var controller = CreateController();
            controller.HttpContext.Request.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";

            // Act
            var result = controller.GetAdminDashboard();

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void GetAdminDashboard_WithoutUserAgent_ReturnsOkResult()
        {
            // Arrange
            var controller = CreateController();
            // No User-Agent header set

            // Act
            var result = controller.GetAdminDashboard();

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }
    }
}
