using fiskaltrust.AndroidLauncher.Helpers;
using System;
using Xunit;

namespace fiskaltrust.AndroidLauncher.Tests.Helpers
{
    public class Base64UrlHelperTests
    {
        [Fact]
        public void Encode_SimpleString_ReturnsBase64UrlEncodedString()
        {
            // Arrange
            var input = "Hello, World!";
            
            // Act
            var result = Base64UrlHelper.Encode(input);
            
            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            // Base64URL should not contain +, /, or = characters
            Assert.DoesNotContain("+", result);
            Assert.DoesNotContain("/", result);
            Assert.DoesNotContain("=", result);
        }

        [Fact]
        public void Decode_Base64UrlString_ReturnsOriginalString()
        {
            // Arrange
            var original = "Hello, World!";
            var encoded = Base64UrlHelper.Encode(original);
            
            // Act
            var decoded = Base64UrlHelper.Decode(encoded);
            
            // Assert
            Assert.Equal(original, decoded);
        }

        [Fact]
        public void EncodeAndDecode_JsonString_ReturnsOriginalJson()
        {
            // Arrange
            var json = "{\"Message\":\"Hello\",\"Value\":123}";
            
            // Act
            var encoded = Base64UrlHelper.Encode(json);
            var decoded = Base64UrlHelper.Decode(encoded);
            
            // Assert
            Assert.Equal(json, decoded);
        }

        [Fact]
        public void EncodeAndDecode_EmptyString_ReturnsEmptyString()
        {
            // Arrange
            var input = "";
            
            // Act
            var encoded = Base64UrlHelper.Encode(input);
            var decoded = Base64UrlHelper.Decode(encoded);
            
            // Assert
            Assert.Equal("", encoded);
            Assert.Equal("", decoded);
        }

        [Fact]
        public void Encode_NullString_ReturnsEmptyString()
        {
            // Arrange
            string input = null;
            
            // Act
            var result = Base64UrlHelper.Encode(input);
            
            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Decode_NullString_ReturnsEmptyString()
        {
            // Arrange
            string input = null;
            
            // Act
            var result = Base64UrlHelper.Decode(input);
            
            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void EncodeAndDecode_SpecialCharacters_ReturnsOriginalString()
        {
            // Arrange
            var input = "Special chars: €£¥ ñ 中文 🎉";
            
            // Act
            var encoded = Base64UrlHelper.Encode(input);
            var decoded = Base64UrlHelper.Decode(encoded);
            
            // Assert
            Assert.Equal(input, decoded);
        }

        [Theory]
        [InlineData("Hello")]
        [InlineData("Test123")]
        [InlineData("https://example.com")]
        [InlineData("{\"key\":\"value\"}")]
        public void EncodeAndDecode_VariousStrings_ReturnsOriginalString(string input)
        {
            // Act
            var encoded = Base64UrlHelper.Encode(input);
            var decoded = Base64UrlHelper.Decode(encoded);
            
            // Assert
            Assert.Equal(input, decoded);
        }

        [Fact]
        public void EncodeBytes_ValidBytes_ReturnsBase64UrlString()
        {
            // Arrange
            var bytes = new byte[] { 1, 2, 3, 4, 5 };
            
            // Act
            var result = Base64UrlHelper.EncodeBytes(bytes);
            
            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void DecodeBytes_ValidBase64Url_ReturnsOriginalBytes()
        {
            // Arrange
            var original = new byte[] { 1, 2, 3, 4, 5 };
            var encoded = Base64UrlHelper.EncodeBytes(original);
            
            // Act
            var decoded = Base64UrlHelper.DecodeBytes(encoded);
            
            // Assert
            Assert.Equal(original, decoded);
        }

        [Fact]
        public void EncodeBytes_NullBytes_ReturnsEmptyString()
        {
            // Arrange
            byte[] input = null;
            
            // Act
            var result = Base64UrlHelper.EncodeBytes(input);
            
            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void DecodeBytes_EmptyString_ReturnsEmptyArray()
        {
            // Arrange
            var input = "";
            
            // Act
            var result = Base64UrlHelper.DecodeBytes(input);
            
            // Assert
            Assert.Empty(result);
        }
    }
}
