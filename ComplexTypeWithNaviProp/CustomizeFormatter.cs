using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;
using System.Web.OData.Extensions;
using System.Web.OData.Formatter;
using System.Web.OData.Formatter.Deserialization;
using System.Web.OData.Formatter.Serialization;
using System.Web.OData.Routing;
using System.Xml;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;

namespace ODataReferentialConstraintSample
{
    public class CustomODataMediaTypeFormatter : ODataMediaTypeFormatter
    {
        public HttpRequestMessage Request { get; set; }

        public List<ODataPayloadKind> PayloadKinds { get; set; }

        public CustomODataMediaTypeFormatter(ODataSerializerProvider serializerProvider,
            ODataDeserializerProvider deserializerProvider, params ODataPayloadKind[] payloadKinds)
            : base(deserializerProvider, serializerProvider, payloadKinds.ToList())
        {
            PayloadKinds = payloadKinds.ToList();
        }
        public CustomODataMediaTypeFormatter(IEnumerable<ODataPayloadKind> payloadKinds)
            : base(payloadKinds)
        {
            PayloadKinds = payloadKinds.ToList();
        }

        public CustomODataMediaTypeFormatter(ODataDeserializerProvider deserializerProvider,
            ODataSerializerProvider serializerProvider,
            IEnumerable<ODataPayloadKind> payloadKinds)
            : base(deserializerProvider, serializerProvider, payloadKinds)
        {

        }

        public override MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request,
            MediaTypeHeaderValue mediaType)
        {
            if (typeof(IEdmModel).IsAssignableFrom(type))
            {
                this.Request = request;
                return this;
            }
            else
            {
                return base.GetPerRequestFormatterInstance(type, request, mediaType);
            }
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content,
            TransportContext transportContext, CancellationToken cancellationToken)
        {

            if (typeof(IEdmModel).IsAssignableFrom(type))
            {
                IEdmModel model = Request.ODataProperties().Model;

                HttpContentHeaders contentHeaders = content == null ? null : content.Headers;
                ODataSerializer serializer = new ODataMetadataSerializer();

                UrlHelper urlHelper = Request.GetUrlHelper() ?? new UrlHelper(Request);

                ODataPath path = Request.ODataProperties().Path;
                IEdmNavigationSource targetNavigationSource = path == null ? null : path.NavigationSource;

                // serialize a response
                HttpConfiguration configuration = Request.GetConfiguration();

                var stream = new MemoryStream();
                IODataResponseMessage responseMessage = new ODataMessageWrapper(stream, content.Headers);

                Uri baseAddress = GetBaseAddress(Request);
                ODataMessageWriterSettings writerSettings = new ODataMessageWriterSettings(MessageWriterSettings)
                {
                    PayloadBaseUri = baseAddress,
                    Version = ODataVersion.V4,
                };

                string metadataLink = urlHelper.CreateODataLink(new MetadataPathSegment());

                writerSettings.ODataUri = new ODataUri
                {
                    ServiceRoot = baseAddress,

                    // TODO: 1604 Convert webapi.odata's ODataPath to ODL's ODataPath, or use ODL's ODataPath.
                    SelectAndExpand = Request.ODataProperties().SelectExpandClause,
                };

                MediaTypeHeaderValue contentType = null;
                if (contentHeaders != null && contentHeaders.ContentType != null)
                {
                    contentType = contentHeaders.ContentType;
                }

                using (
                    ODataMessageWriter messageWriter = new ODataMessageWriter(responseMessage, writerSettings, model)
                    )
                {
                    ODataSerializerContext writeContext = new ODataSerializerContext()
                    {
                        Request = Request,
                        RequestContext = Request.GetRequestContext(),
                        Url = urlHelper,
                        NavigationSource = targetNavigationSource,
                        Model = model,
                        RootElementName = GetRootElementName(path) ?? "root",
                        SkipExpensiveAvailabilityChecks = serializer.ODataPayloadKind == ODataPayloadKind.Feed,
                        Path = path,
                        MetadataLevel = ODataMetadataLevel.FullMetadata,
                        SelectExpandClause = Request.ODataProperties().SelectExpandClause
                    };

                    serializer.WriteObject(value, type, messageWriter, writeContext);
                    XmlDocument doc = new XmlDocument();
                    if (stream.Position > 0)
                    {
                        stream.Position = 0;
                    }
                    doc.Load(stream);
                    MetadataModify(doc, "Order");

                    using (XmlWriter writer = XmlWriter.Create(writeStream))
                    {
                        doc.WriteTo(writer);
                        writer.Flush();
                    }
                }

                return Task.FromResult<AsyncVoid>(default(AsyncVoid));
            }
            else
            {
                return base.WriteToStreamAsync(type, null, writeStream, content, transportContext, cancellationToken);
            }
        }

        private void MetadataModify(XmlDocument doc, string entityTypeName)
        {
            string entitySetName = entityTypeName + 's';
            XmlNode newNode = null;
            XmlNode oldNode = null;
            foreach (XmlNode node in doc.GetElementsByTagName("EntityType"))
            {
                if (node.Attributes["Name"].InnerText == entityTypeName)
                {
                    node.RemoveChild(node.FirstChild);
                    node.RemoveChild(node.FirstChild);
                }
            }
            XmlNode entitySetNode = null;
            foreach (XmlNode node in doc.GetElementsByTagName("EntitySet"))
            {
                if (node.Attributes["Name"].InnerText == entitySetName)
                {
                    entitySetNode = node;
                }
                else
                {
                    XmlNode removeChild = null;
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        if (child.Attributes["Target"].InnerText == entitySetName)
                        {
                            removeChild = child;
                        }
                    }
                    if (removeChild != null)
                    {
                        node.RemoveChild(removeChild);
                    }
                }
            }
            var containerNode = doc.GetElementsByTagName("EntityContainer")[0];
            containerNode.RemoveChild(entitySetNode);
            containerNode.InnerXml = containerNode.InnerXml.Replace(" EntitySet=\"" + entitySetName + "\"", "");
            var schema = doc.GetElementsByTagName("Schema")[0];
            Regex rgx = new Regex("EntityType Name=\"" + entityTypeName + "\".*</EntityType>");
            var result = rgx.Match(schema.InnerXml);
            var expected = result.Groups[0].Value.Replace("EntityType", "ComplexType");
            schema.InnerXml = schema.InnerXml.Replace(result.Groups[0].Value, expected);
            schema.InnerXml = schema.InnerXml.Replace("NavigationProperty Name=\"" + entityTypeName + "\"", "Property Name=\"" + entityTypeName + "\"");
        }

        private static string GetRootElementName(ODataPath path)
        {
            if (path != null)
            {
                ODataPathSegment lastSegment = path.Segments.LastOrDefault();
                if (lastSegment != null)
                {
                    BoundActionPathSegment actionSegment = lastSegment as BoundActionPathSegment;
                    if (actionSegment != null)
                    {
                        return actionSegment.Action.Name;
                    }

                    PropertyAccessPathSegment propertyAccessSegment = lastSegment as PropertyAccessPathSegment;
                    if (propertyAccessSegment != null)
                    {
                        return propertyAccessSegment.Property.Name;
                    }
                }
            }
            return null;
        }
        private static Uri GetBaseAddress(HttpRequestMessage request)
        {
            UrlHelper urlHelper = request.GetUrlHelper() ?? new UrlHelper(request);

            string baseAddress = urlHelper.CreateODataLink();

            return baseAddress[baseAddress.Length - 1] != '/' ? new Uri(baseAddress + '/') : new Uri(baseAddress);
        }

        private struct AsyncVoid
        {
        }

        public override bool CanReadType(Type type)
        {
            if (typeof(IEdmModel).IsAssignableFrom(type))
                return true;
            else
                return false;
        }

        /// <inheritdoc/>
        public override bool CanWriteType(Type type)
        {
            if (typeof(IEdmModel).IsAssignableFrom(type))
                return true;
            else
                return false;
        }
        public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
        {
            if (!typeof(IEdmModel).IsAssignableFrom(type))
            {
                return;
            }

            if (mediaType != null)
            {
                if (mediaType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase) &&
                    !mediaType.Parameters.Any(p => p.Name.Equals("odata.metadata", StringComparison.OrdinalIgnoreCase)))
                {
                    mediaType.Parameters.Add(new NameValueHeaderValue("odata.metadata", "minimal"));
                }

                headers.ContentType = (MediaTypeHeaderValue)((ICloneable)mediaType).Clone();
            }
            else
            {
                // This is the case when a user creates a new ObjectContent<T> passing in a null mediaType
                base.SetDefaultContentHeaders(type, headers, mediaType);
            }

            // In general, in Web API we pick a default charset based on the supported character sets
            // of the formatter. However, according to the OData spec, the service shouldn't be sending
            // a character set unless explicitly specified, so if the client didn't send the charset we chose
            // we just clean it.
            if (headers.ContentType != null &&
                !Request.Headers.AcceptCharset
                    .Any(cs => cs.Value.Equals(headers.ContentType.CharSet, StringComparison.OrdinalIgnoreCase)))
            {
                headers.ContentType.CharSet = String.Empty;
            }

            headers.TryAddWithoutValidation(
                "OData-Version",
                ODataUtils.ODataVersionToString(ODataVersion.V4));
        }
    }

    internal class ODataMessageWrapper : IODataRequestMessage, IODataResponseMessage, IODataUrlResolver
    {
        private Stream _stream;
        private Dictionary<string, string> _headers;
        private IDictionary<string, string> _contentIdMapping;
        private static readonly Regex ContentIdReferencePattern = new Regex(@"\$\d", RegexOptions.Compiled);

        public ODataMessageWrapper()
            : this(stream: null, headers: null)
        {
        }

        public ODataMessageWrapper(Stream stream)
            : this(stream: stream, headers: null)
        {
        }

        public ODataMessageWrapper(Stream stream, HttpHeaders headers)
            : this(stream: stream, headers: headers, contentIdMapping: null)
        {
        }

        public ODataMessageWrapper(Stream stream, HttpHeaders headers, IDictionary<string, string> contentIdMapping)
        {
            _stream = stream;
            if (headers != null)
            {
                _headers = headers.ToDictionary(kvp => kvp.Key, kvp => String.Join(";", kvp.Value));
            }
            else
            {
                _headers = new Dictionary<string, string>();
            }
            _contentIdMapping = contentIdMapping ?? new Dictionary<string, string>();
        }

        public IEnumerable<KeyValuePair<string, string>> Headers
        {
            get
            {
                return _headers;
            }
        }

        public string Method
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public Uri Url
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int StatusCode
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string GetHeader(string headerName)
        {
            string value;
            if (_headers.TryGetValue(headerName, out value))
            {
                return value;
            }

            return null;
        }

        public Stream GetStream()
        {
            return _stream;
        }

        public void SetHeader(string headerName, string headerValue)
        {
            _headers[headerName] = headerValue;
        }

        public Uri ResolveUrl(Uri baseUri, Uri payloadUri)
        {
            if (payloadUri == null)
            {
                throw new ArgumentNullException("payloadUri");
            }

            string originalPayloadUri = payloadUri.OriginalString;
            if (ContentIdReferencePattern.IsMatch(originalPayloadUri))
            {
                string resolvedUri = ResolveContentId(originalPayloadUri, _contentIdMapping);
                return new Uri(resolvedUri, UriKind.RelativeOrAbsolute);
            }

            // Returning null for default resolution.
            return null;
        }

        public static string ResolveContentId(string url, IDictionary<string, string> contentIdToLocationMapping)
        {

            int startIndex = 0;

            while (true)
            {
                startIndex = url.IndexOf('$', startIndex);

                if (startIndex == -1)
                {
                    break;
                }

                int keyLength = 0;

                while (startIndex + keyLength < url.Length - 1 && IsContentIdCharacter(url[startIndex + keyLength + 1]))
                {
                    keyLength++;
                }

                if (keyLength > 0)
                {
                    // Might have matched a $<content-id> alias.
                    string locationKey = url.Substring(startIndex + 1, keyLength);
                    string locationValue;

                    if (contentIdToLocationMapping.TryGetValue(locationKey, out locationValue))
                    {
                        // As location headers MUST be absolute URL's, we can ignore everything 
                        // before the $content-id while resolving it.
                        return locationValue + url.Substring(startIndex + 1 + keyLength);
                    }
                }

                startIndex++;
            }

            return url;
        }

        private static bool IsContentIdCharacter(char c)
        {
            // According to the OData ABNF grammar, Content-IDs follow the scheme.
            // content-id = "Content-ID" ":" OWS 1*unreserved
            // unreserved    = ALPHA / DIGIT / "-" / "." / "_" / "~"
            switch (c)
            {
                case '-':
                case '.':
                case '_':
                case '~':
                    return true;
                default:
                    return Char.IsLetterOrDigit(c);
            }
        }
    }
}
