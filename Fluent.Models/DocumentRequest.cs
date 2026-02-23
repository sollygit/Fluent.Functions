using System.Collections.Generic;

namespace Fluent.Models
{
    public record DocumentRequest(string OutputFormat, string Data, string ConnectionString, string Format, List<Datasource> Datasources);
    public record Datasource(string Name, string Type, string ConnectionString, string Data);
}
