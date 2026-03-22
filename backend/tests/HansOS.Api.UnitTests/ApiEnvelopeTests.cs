using FluentAssertions;
using HansOS.Api.Common;

namespace HansOS.Api.UnitTests;

public class ApiEnvelopeTests
{
    [Fact]
    public void Success_WrapsDataCorrectly()
    {
        var envelope = ApiEnvelope<string>.Success("test-data");

        envelope.Code.Should().Be(0);
        envelope.Data.Should().Be("test-data");
        envelope.Error.Should().BeNull();
        envelope.Message.Should().Be("ok");
    }

    [Fact]
    public void Success_WithCustomMessage_SetsMessage()
    {
        var envelope = ApiEnvelope<int>.Success(42, "custom");

        envelope.Code.Should().Be(0);
        envelope.Data.Should().Be(42);
        envelope.Message.Should().Be("custom");
    }

    [Fact]
    public void Fail_WrapsErrorCorrectly()
    {
        var envelope = ApiEnvelope<object>.Fail("something went wrong");

        envelope.Code.Should().Be(-1);
        envelope.Data.Should().BeNull();
        envelope.Error.Should().Be("something went wrong");
        envelope.Message.Should().Be("something went wrong");
    }

    [Fact]
    public void Fail_WithSeparateMessage_SetsBoth()
    {
        var envelope = ApiEnvelope<object>.Fail("error_code", "使用者友善訊息");

        envelope.Code.Should().Be(-1);
        envelope.Error.Should().Be("error_code");
        envelope.Message.Should().Be("使用者友善訊息");
    }
}
