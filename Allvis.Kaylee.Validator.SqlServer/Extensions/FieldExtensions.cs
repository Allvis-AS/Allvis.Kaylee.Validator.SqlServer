using Allvis.Kaylee.Analyzer.Enums;
using System;
using System.Text;

namespace Allvis.Kaylee.Validator.SqlServer.Extensions
{
    public static class FieldExtensions
    {
        public static string GetSqlServerSpecification(this Analyzer.Models.Field field, int nameWidth = 0)
        {
            var sb = new StringBuilder();
            if (nameWidth > 0)
            {
                sb.Append($"[{field.Name}]".PadRight(nameWidth));
            }
            else
            {
                sb.Append($"[{field.Name}]");
            }
            sb.Append(' ');
            sb.Append(field.GetSqlServerType());
            sb.Append(' ');
            if (field.Nullable)
            {
                sb.Append("NULL");
            }
            else
            {
                sb.Append("NOT NULL");
            }
            if (!string.IsNullOrWhiteSpace(field.DefaultExpression))
            {
                sb.Append(" DEFAULT ");
                sb.Append(field.DefaultExpression);
            }
            return sb.ToString();
        }

        public static string GetSqlServerType(this Analyzer.Models.Field field) => field.Type switch
        {
            FieldType.BIT => "BIT",
            FieldType.TINYINT => "TINYINT",
            FieldType.INT => $"INT{(field.AutoIncrement ? " IDENTITY(1, 1)" : string.Empty)}",
            FieldType.BIGINT => $"BIGINT{(field.AutoIncrement ? " IDENTITY(1, 1)" : string.Empty)}",
            FieldType.DECIMAL => $"DECIMAL({field.GetSqlServerSize()})",
            FieldType.CHAR => "NCHAR(1)",
            FieldType.TEXT => $"NVARCHAR({field.GetSqlServerSize()})",
            FieldType.GUID => "UNIQUEIDENTIFIER",
            FieldType.DATE => "DATETIMEOFFSET",
            FieldType.VARBINARY => $"VARBINARY({field.GetSqlServerSize()})",
            FieldType.BINARY => $"BINARY({field.GetSqlServerSize()})",
            FieldType.ROWVERSION => "ROWVERSION",
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };

        private static string GetSqlServerSize(this Analyzer.Models.Field field)
        {
            if (field.Size.IsMax)
            {
                return "MAX";
            }
            if (field.Type == FieldType.DECIMAL)
            {
                return $"{field.Size.Size}, {field.Size.Precision}";
            }
            return $"{field.Size.Size}";
        }
    }
}
