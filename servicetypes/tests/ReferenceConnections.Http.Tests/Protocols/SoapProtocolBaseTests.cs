using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Http;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Protocols;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReferenceConnections.Http.Tests.Protocols;

/// <summary>
/// Testable implementation of SoapProtocolBase that exposes methods for testing.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class TestableSoapProtocol : SoapProtocolBase
{
    public TestableSoapProtocol()
        : base(998, "TestSoap", "Test SOAP protocol", "text/xml")
    {
    }

    public override SoapVersion Version => SoapVersion.Soap11;

    // Expose protected methods for testing
    public XDocument TestWrapInEnvelope(XElement bodyContent, HttpProtocolContext context)
        => WrapInEnvelope(bodyContent, context);

    public XElement? TestExtractSoapBody(XDocument doc)
        => ExtractSoapBody(doc);

    public IGenericResult TestCheckForSoapFault(XDocument doc)
        => CheckForSoapFault(doc);
}

public class SoapProtocolBaseTests
{
    private readonly TestableSoapProtocol _protocol;
    private readonly HttpProtocolContext _context;

    public SoapProtocolBaseTests()
    {
        _protocol = new TestableSoapProtocol();
        _context = CreateTestContext();
    }

    private static HttpProtocolContext CreateTestContext(
        HttpSoapSettings? soap = null,
        string securityType = "None",
        Dictionary<string, string?>? security = null)
    {
        var config = new HttpConnectionConfiguration
        {
            BaseUrl = "https://api.example.com/soap",
            Protocol = "Soap11",
            Soap = soap,
            AuthenticationType = securityType,
            AdditionalProperties = security ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        };
        return new HttpProtocolContext(
            Configuration: config,
            LoggerFactory: NullLoggerFactory.Instance,
            ResolvedCertificate: null,
            ResolvedPassword: null,
            ResolvedApiKey: null);
    }

    #region WrapInEnvelope Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void WrapInEnvelopeCreatesValidSoapEnvelope()
    {
        var body = new XElement("TestOperation");

        var result = _protocol.TestWrapInEnvelope(body, _context);

        result.ShouldNotBeNull();
        result.Root.ShouldNotBeNull();
        result.Root!.Name.LocalName.ShouldBe("Envelope");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void WrapInEnvelopeContainsHeaderAndBody()
    {
        var body = new XElement("TestOp");
        var ns = SoapVersion.Soap11.EnvelopeNamespace;

        var result = _protocol.TestWrapInEnvelope(body, _context);

        result.Root!.Element(ns + "Header").ShouldNotBeNull();
        result.Root!.Element(ns + "Body").ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void WrapInEnvelopeIncludesBodyContent()
    {
        var body = new XElement("MyOperation", new XElement("Param", "value"));

        var result = _protocol.TestWrapInEnvelope(body, _context);
        var ns = SoapVersion.Soap11.EnvelopeNamespace;

        var soapBody = result.Root!.Element(ns + "Body");
        soapBody!.Element("MyOperation").ShouldNotBeNull();
        soapBody!.Element("MyOperation")!.Element("Param")!.Value.ShouldBe("value");
    }

    #endregion

    #region ExtractSoapBody Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExtractSoapBodyReturnsBodyElement()
    {
        var ns = SoapVersion.Soap11.EnvelopeNamespace;
        var doc = new XDocument(
            new XElement(ns + "Envelope",
                new XElement(ns + "Header"),
                new XElement(ns + "Body",
                    new XElement("Result", "data"))));

        var body = _protocol.TestExtractSoapBody(doc);

        body.ShouldNotBeNull();
        body!.Element("Result")!.Value.ShouldBe("data");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExtractSoapBodyReturnsNullWhenNoBody()
    {
        var ns = SoapVersion.Soap11.EnvelopeNamespace;
        var doc = new XDocument(
            new XElement(ns + "Envelope",
                new XElement(ns + "Header")));

        var body = _protocol.TestExtractSoapBody(doc);

        body.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExtractSoapBodyReturnsNullWhenNoRoot()
    {
        var doc = new XDocument();

        var body = _protocol.TestExtractSoapBody(doc);

        body.ShouldBeNull();
    }

    #endregion

    #region CheckForSoapFault Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CheckForSoapFaultReturnsSuccessWhenNoFault()
    {
        var ns = SoapVersion.Soap11.EnvelopeNamespace;
        var doc = new XDocument(
            new XElement(ns + "Envelope",
                new XElement(ns + "Body",
                    new XElement("Result", "ok"))));

        var result = _protocol.TestCheckForSoapFault(doc);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CheckForSoapFaultDetectsSoap11Fault()
    {
        var ns = SoapVersion.Soap11.EnvelopeNamespace;
        var doc = new XDocument(
            new XElement(ns + "Envelope",
                new XElement(ns + "Body",
                    new XElement(ns + "Fault",
                        new XElement("faultcode", "soap:Server"),
                        new XElement("faultstring", "Internal error")))));

        var result = _protocol.TestCheckForSoapFault(doc);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CheckForSoapFaultDetectsSoap12StyleFault()
    {
        var ns = SoapVersion.Soap11.EnvelopeNamespace;
        var doc = new XDocument(
            new XElement(ns + "Envelope",
                new XElement(ns + "Body",
                    new XElement(ns + "Fault",
                        new XElement(ns + "Code",
                            new XElement(ns + "Value", "env:Receiver")),
                        new XElement(ns + "Reason",
                            new XElement(ns + "Text", "Server error"))))));

        var result = _protocol.TestCheckForSoapFault(doc);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CheckForSoapFaultReturnsSuccessWhenNoBody()
    {
        var ns = SoapVersion.Soap11.EnvelopeNamespace;
        var doc = new XDocument(
            new XElement(ns + "Envelope"));

        var result = _protocol.TestCheckForSoapFault(doc);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CheckForSoapFaultHandlesFaultWithoutDetails()
    {
        var ns = SoapVersion.Soap11.EnvelopeNamespace;
        var doc = new XDocument(
            new XElement(ns + "Envelope",
                new XElement(ns + "Body",
                    new XElement(ns + "Fault"))));

        var result = _protocol.TestCheckForSoapFault(doc);

        result.IsSuccess.ShouldBeFalse();
    }

    #endregion

    #region Translate Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TranslateReturnsPostRequest()
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns((IPath?)null!);
        container.Setup(c => c.Name).Returns("TestContainer");

        var result = await _protocol.Translate(command.Object, container.Object, _context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Method.ShouldBe(HttpMethod.Post);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TranslateContainsSoapEnvelopeInBody()
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("GetData");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns((IPath?)null!);
        container.Setup(c => c.Name).Returns("DataService");

        var result = await _protocol.Translate(command.Object, container.Object, _context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var content = await result.Value!.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("Envelope");
        content.ShouldContain("Body");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TranslateAddsSoapActionHeaderWhenMetadataHasSoapAction()
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("GetData");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>
        {
            ["SoapAction"] = "http://example.com/GetData"
        });
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns((IPath?)null!);
        container.Setup(c => c.Name).Returns("DataService");

        var result = await _protocol.Translate(command.Object, container.Object, _context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Headers.Contains("SOAPAction").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TranslateUsesSoapActionPatternFromConfig()
    {
        var soap = new HttpSoapSettings { SoapActionPattern = "http://example.com/{operation}" };
        var context = CreateTestContext(soap: soap);

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("GetData");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns((IPath?)null!);
        container.Setup(c => c.Name).Returns("DataService");

        var result = await _protocol.Translate(command.Object, container.Object, context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Headers.Contains("SOAPAction").ShouldBeTrue();
        var soapAction = result.Value!.Headers.GetValues("SOAPAction").First();
        soapAction.ShouldContain("GetData");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TranslateWithNoSecuritySkipsSecurityProcessing()
    {
        var context = CreateTestContext(securityType: "None");
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns((IPath?)null!);
        container.Setup(c => c.Name).Returns("Test");

        var result = await _protocol.Translate(command.Object, container.Object, context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TranslateWithNoneSecuritySkipsSecurityProcessing()
    {
        var context = CreateTestContext(securityType: "None");

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns((IPath?)null!);
        container.Setup(c => c.Name).Returns("Test");

        var result = await _protocol.Translate(command.Object, container.Object, context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TranslateWithUnknownAuthenticationTypeReturnsFailure()
    {
        // ApiKey security is not registered in SoapSecurityProcessors, so it returns failure
        var context = CreateTestContext(securityType: "ApiKey");

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns((IPath?)null!);
        container.Setup(c => c.Name).Returns("Test");

        var result = await _protocol.Translate(command.Object, container.Object, context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    #endregion

    #region ProcessResponse Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ProcessResponseWithEmptyContentAndSuccessReturnsNull()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("")
        };

        var container = new Mock<IStorageContainer>();
        var result = await _protocol.ProcessResponse(response, container.Object, typeof(string), _context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ProcessResponseWithEmptyContentAndErrorReturnsFailure()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(""),
            ReasonPhrase = "Server Error"
        };

        var container = new Mock<IStorageContainer>();
        var result = await _protocol.ProcessResponse(response, container.Object, typeof(string), _context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ProcessResponseWithInvalidXmlReturnsFailure()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not xml at all")
        };

        var container = new Mock<IStorageContainer>();
        var result = await _protocol.ProcessResponse(response, container.Object, typeof(string), _context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ProcessResponseWithSoapFaultReturnsFailure()
    {
        var ns = SoapVersion.Soap11.EnvelopeNamespace;
        var soapFault = new XDocument(
            new XElement(ns + "Envelope",
                new XElement(ns + "Body",
                    new XElement(ns + "Fault",
                        new XElement("faultcode", "soap:Server"),
                        new XElement("faultstring", "Server error")))));

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(soapFault.ToString())
        };

        var container = new Mock<IStorageContainer>();
        var result = await _protocol.ProcessResponse(response, container.Object, typeof(string), _context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ProcessResponseWithMissingBodyReturnsFailure()
    {
        var ns = SoapVersion.Soap11.EnvelopeNamespace;
        var doc = new XDocument(
            new XElement(ns + "Envelope",
                new XElement(ns + "Header")));

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(doc.ToString())
        };

        var container = new Mock<IStorageContainer>();
        var result = await _protocol.ProcessResponse(response, container.Object, typeof(string), _context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ProcessResponseExtractsStringResult()
    {
        var ns = SoapVersion.Soap11.EnvelopeNamespace;
        var doc = new XDocument(
            new XElement(ns + "Envelope",
                new XElement(ns + "Body",
                    new XElement("Result", "hello world"))));

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(doc.ToString())
        };

        var container = new Mock<IStorageContainer>();
        var result = await _protocol.ProcessResponse(response, container.Object, typeof(string), _context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ProcessResponseExtractsXElementResult()
    {
        var ns = SoapVersion.Soap11.EnvelopeNamespace;
        var doc = new XDocument(
            new XElement(ns + "Envelope",
                new XElement(ns + "Body",
                    new XElement("Response",
                        new XElement("Data", "value")))));

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(doc.ToString())
        };

        var container = new Mock<IStorageContainer>();
        var result = await _protocol.ProcessResponse(response, container.Object, typeof(XElement), _context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeOfType<XElement>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ProcessResponseReturnsNullForEmptyBody()
    {
        var ns = SoapVersion.Soap11.EnvelopeNamespace;
        var doc = new XDocument(
            new XElement(ns + "Envelope",
                new XElement(ns + "Body")));

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(doc.ToString())
        };

        var container = new Mock<IStorageContainer>();
        // Request a type that is not string or XElement, with empty body
        var result = await _protocol.ProcessResponse(response, container.Object, typeof(int), _context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ProcessResponseWithChildElementReturnsFirstChild()
    {
        var ns = SoapVersion.Soap11.EnvelopeNamespace;
        var doc = new XDocument(
            new XElement(ns + "Envelope",
                new XElement(ns + "Body",
                    new XElement("Response",
                        new XElement("Item1", "first"),
                        new XElement("Item2", "second")))));

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(doc.ToString())
        };

        var container = new Mock<IStorageContainer>();
        var result = await _protocol.ProcessResponse(response, container.Object, typeof(int), _context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    #endregion

    #region Property Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void VersionReturnsSoap11()
    {
        _protocol.Version.ShouldBe(SoapVersion.Soap11);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DescriptionReturnsExpectedValue()
    {
        _protocol.Description.ShouldBe("Test SOAP protocol");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultContentTypeReturnsExpectedValue()
    {
        _protocol.DefaultContentType.ShouldBe("text/xml");
    }

    #endregion

    #region GetSoapAction Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TranslateWithSoapActionPatternSubstitutesContainer()
    {
        var soap = new HttpSoapSettings { SoapActionPattern = "http://example.com/{container}/{operation}" };
        var context = CreateTestContext(soap: soap);

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("GetData");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns((IPath?)null!);
        container.Setup(c => c.Name).Returns("MyContainer");

        var result = await _protocol.Translate(command.Object, container.Object, context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var soapAction = result.Value!.Headers.GetValues("SOAPAction").First();
        soapAction.ShouldContain("MyContainer");
        soapAction.ShouldContain("GetData");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TranslateWithNoSoapActionAndNoMetadataOmitsHeader()
    {
        // Context with no soap settings, command with no SoapAction metadata
        var context = CreateTestContext(soap: null);
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns((IPath?)null!);
        container.Setup(c => c.Name).Returns("Test");

        var result = await _protocol.Translate(command.Object, container.Object, context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Headers.Contains("SOAPAction").ShouldBeFalse();
    }

    #endregion

    #region BuildSoapBody Default Implementation Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TranslateUsesOperationMetadataForBodyElementName()
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("FallbackType");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>
        {
            ["Operation"] = "CustomOperationName"
        });
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns((IPath?)null!);
        container.Setup(c => c.Name).Returns("Container");

        var result = await _protocol.Translate(command.Object, container.Object, _context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var content = await result.Value!.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("CustomOperationName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TranslateFallsBackToCommandTypeForBodyElementName()
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("DefaultQuery");
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns((IPath?)null!);
        container.Setup(c => c.Name).Returns("Container");

        var result = await _protocol.Translate(command.Object, container.Object, _context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var content = await result.Value!.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("DefaultQuery");
    }

    #endregion
}
