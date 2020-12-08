namespace Allvis.Kaylee.Validator.SqlServer.Models.DB
{
    public class ReferentialConstraint
    {
        public string Schema { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TargetSchema { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public bool CascadingUpdates { get; set; }
        public bool CascadingDeletes { get; set; }

        public TableConstraint? Source { get; set; }
        public TableConstraint? Target { get; set; }
    }
}
