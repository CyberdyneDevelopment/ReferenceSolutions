using System.Xml.Linq;
using Fdw.Services.Connections.Http.Protocols;
using Shouldly;
using Xunit;

namespace ReferenceConnections.Http.Tests.Protocols;

public class SoapVersionTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Soap11HasCorrectNamespace()
    {
        SoapVersion.Soap11.EnvelopeNamespace.NamespaceName
            .ShouldBe("http://schemas.xmlsoap.org/soap/envelope/");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Soap11HasCorrectContentType()
    {
        SoapVersion.Soap11.ContentType.ShouldBe("text/xml; charset=utf-8");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Soap11HasCorrectMustUnderstandValue()
    {
        SoapVersion.Soap11.MustUnderstandValue.ShouldBe("1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Soap11HasCorrectName()
    {
        SoapVersion.Soap11.Name.ShouldBe("Soap11");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Soap12HasCorrectNamespace()
    {
        SoapVersion.Soap12.EnvelopeNamespace.NamespaceName
            .ShouldBe("http://www.w3.org/2003/05/soap-envelope");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Soap12HasCorrectContentType()
    {
        SoapVersion.Soap12.ContentType.ShouldBe("application/soap+xml; charset=utf-8");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Soap12HasCorrectMustUnderstandValue()
    {
        SoapVersion.Soap12.MustUnderstandValue.ShouldBe("true");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Soap12HasCorrectName()
    {
        SoapVersion.Soap12.Name.ShouldBe("Soap12");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Soap11AndSoap12AreDifferent()
    {
        SoapVersion.Soap11.ShouldNotBe(SoapVersion.Soap12);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RecordStructEqualityWorks()
    {
        var version1 = SoapVersion.Soap11;
        var version2 = SoapVersion.Soap11;

        version1.ShouldBe(version2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CustomSoapVersionCanBeCreated()
    {
        var custom = new SoapVersion(
            "Custom",
            XNamespace.Get("http://example.com/soap"),
            "application/xml",
            "true");

        custom.Name.ShouldBe("Custom");
        custom.ContentType.ShouldBe("application/xml");
    }
}
