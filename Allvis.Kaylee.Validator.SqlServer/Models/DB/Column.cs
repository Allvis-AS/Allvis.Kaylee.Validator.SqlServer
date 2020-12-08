namespace Allvis.Kaylee.Validator.SqlServer.Models.DB
{
    public class Column
    {
        public string Schema { get; set; } = string.Empty;
        public string Table { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Default { get; set; }
        public bool Nullable { get; set; }
        public string Type { get; set; } = string.Empty;
        public int? Length { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }

        public string TypeArgument
            => HasLength
                ? $"({(Length == -1 ? "MAX" : Length)})"
                : HasPrecision
                    ? $"({Precision}, {Scale})"
                    : string.Empty;

        public bool HasLength
            => Type switch
            {
                "BINARY" => true,
                "VARBINARY" => true,
                "CHAR" => true,
                "NCHAR" => true,
                "VARCHAR" => true,
                "NVARCHAR" => true,
                _ => false,
            };

        public bool HasPrecision
            => Type switch
            {
                "DECIMAL" => true,
                _ => false,
            };
    }
}
