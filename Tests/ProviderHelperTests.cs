using Netka.Identity.Infrastructure.Helpers;
using Xunit;

namespace Netka.Identity.SDK.Tests;

public class ProviderHelperTests
{
    [Theory]
    [InlineData("1234567890121", true)]
    [InlineData("1101700203451", false)]
    [InlineData("1234567890122", false)]
    [InlineData("12345", false)]
    [InlineData("123456789012A", false)]
    [InlineData("", false)]
    public void IsValidThaiNationalId_ValidatesModulo11ChecksumCorrectly(string pid, bool expected)
    {
        bool actual = pid.IsValidThaiNationalId();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ParseJsonToDictionary_ConvertsJsonElementsToNativePrimitivesWithoutMutation()
    {
        // Arrange
        string json = """
        {
            "access_token": "tok_123",
            "expires_in": 3600,
            "active": true,
            "pid": "1234567890121",
            "address": {
                "province": "กรุงเทพมหานคร"
            }
        }
        """;

        // Act
        var dict = ProviderHelper.ParseJsonToDictionary(json, "TEST_PARSE_ERR");

        // Assert
        Assert.Equal("tok_123", dict["access_token"]);
        Assert.Equal(3600L, dict["expires_in"]);
        Assert.Equal(true, dict["active"]);
        Assert.Equal("1234567890121", dict["pid"]);

        var address = Assert.IsAssignableFrom<IDictionary<string, object>>(dict["address"]);
        Assert.Equal("กรุงเทพมหานคร", address["province"]);
    }
}
