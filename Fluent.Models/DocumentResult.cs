using System;
using System.Net;
using System.Xml.Serialization;

namespace Fluent.Models
{
    [XmlRoot("Document")]
    public record DocumentResult
    {
        public Guid Guid { get; init; } = Guid.Empty;
        public HttpStatusCode StatusCode { get; init; }
        public int NumberOfPages { get; init; }
        public string Uri { get; init; } = string.Empty;
    }
}
