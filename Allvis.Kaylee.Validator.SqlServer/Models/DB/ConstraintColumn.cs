namespace Allvis.Kaylee.Validator.SqlServer.Models.DB
{
    public class ConstraintColumn
    {
        public string ConstraintSchema { get; set; } = string.Empty;
        public string ConstraintName { get; set; } = string.Empty;
        public string TableSchema { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public int Position { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
