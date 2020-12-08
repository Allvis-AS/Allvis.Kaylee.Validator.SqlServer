using System.Collections.Generic;

namespace Allvis.Kaylee.Validator.SqlServer.Models.DB
{
    public class TableConstraint
    {
        public string Schema { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TableSchema { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        public ReferentialConstraint? ReferentialConstraint { get; set; }
        public List<ConstraintColumn> Columns { get; } = new List<ConstraintColumn>();

        public bool IsPrimary => Type == "PRIMARY KEY";
        public bool IsForeign => Type == "FOREIGN KEY";
        public bool IsUnique => Type == "UNIQUE";
    }
}
