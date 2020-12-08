using System.Collections.Generic;

namespace Allvis.Kaylee.Validator.SqlServer.Models.DB
{
    public class Table
    {
        public string Schema { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        public bool IsTable => Type == "BASE TABLE";
        public bool IsView => Type == "VIEW";

        public List<Column> Columns { get; } = new List<Column>();
        public List<TableConstraint> Constraints { get; } = new List<TableConstraint>();
    }
}
