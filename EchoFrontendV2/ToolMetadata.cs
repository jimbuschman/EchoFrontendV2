//using Microsoft.AspNetCore.Mvc.Routing;
namespace TestSQLLite
{
    public class ToolMetadata
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, (string Type, string Description, bool Required)> Arguments { get; set; }
    }
}