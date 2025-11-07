using System;
using System.Net;
using System.Xml.Serialization;

namespace Fluent.Models
{
    [XmlRoot("Document")]
    public class DocumentResponse
    {
        public Guid Guid { get; set; }
        public int NumberOfPages { get; set; }
    }

    public class DocumentSequenceResponse : DocumentResponse
    {
        [XmlIgnore]
        public HttpStatusCode StatusCode { get; set; }

        public string Uri { get; set; }

        [XmlIgnore]
        public string Status 
        { 
            get 
            { 
                return StatusCode.ToString(); 
            }
        }
    }
}
