using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Data;

namespace Fluent.Common
{
    public static class ObjectExtensions
    {
        public static string ToJson(this object value)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver(),
                Converters = new List<JsonConverter> { new StringEnumConverter() },
                Formatting = Formatting.Indented
            };

            return JsonConvert.SerializeObject(value, settings);
        }

        public static T FromJson<T>(this string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static string ConvertToJson(this DataTable dt)
        {
            var lstRows = new List<Dictionary<string, object>>();

            foreach (DataRow row in dt.Rows)
            {
                var dictionary = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    dictionary.Add(col.ColumnName, row[col]);
                }
                lstRows.Add(dictionary);
            }

            return lstRows.ToJson();

        }
    }
}
