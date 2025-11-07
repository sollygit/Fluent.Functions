using System.Collections.Generic;

namespace Fluent.Models
{
    public class DocumentRequest
    {
        public string OutputFormat { get; set; }
        public string Data { get; set; }
        public string ConnectionString { get; set; }
        public string Format { get; set; }
        public List<Datasource> Datasources { get; set; }
        public bool TrackImports { get; set; }
        public int TrackErrors { get; set; }
    }

    public class Datasource
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string ConnectionString { get; set; }
        public string Data { get; set; }
    }
}
