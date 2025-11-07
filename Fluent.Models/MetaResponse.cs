using System;

namespace Fluent.Models
{
    public class MetaResponse
    {
        public Guid Guid { get; set; }
        public string Uri { get; set; }
        public int NumberOfPages { get; set; }
    }
}
