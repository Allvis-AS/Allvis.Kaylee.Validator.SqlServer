

using System;
using Allvis.Kaylee.Analyzer.Enums;

namespace Allvis.Kaylee.Validator.SqlServer.Extensions
{
    public static class FieldTypeExtensions
    {
        public static string GetSqlServerType(this FieldType type)
            => type switch
            {
                FieldType.BIT => "BIT",
                FieldType.TINYINT => "TINYINT",
                FieldType.INT => "INT",
                FieldType.BIGINT => "BIGINT",
                FieldType.DECIMAL => "DECIMAL",
                FieldType.CHAR => "NCHAR",
                FieldType.TEXT => "NVARCHAR",
                FieldType.GUID => "UNIQUEIDENTIFIER",
                FieldType.DATE => "DATETIMEOFFSET",
                FieldType.VARBINARY => "VARBINARY",
                FieldType.BINARY => "BINARY",
                FieldType.ROWVERSION => "ROWVERSION",
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

        public static string GetRawSqlServerType(this FieldType type)
            => type switch
            {
                FieldType.BIT => "BIT",
                FieldType.TINYINT => "TINYINT",
                FieldType.INT => "INT",
                FieldType.BIGINT => "BIGINT",
                FieldType.DECIMAL => "DECIMAL",
                FieldType.CHAR => "NCHAR",
                FieldType.TEXT => "NVARCHAR",
                FieldType.GUID => "UNIQUEIDENTIFIER",
                FieldType.DATE => "DATETIMEOFFSET",
                FieldType.VARBINARY => "VARBINARY",
                FieldType.BINARY => "BINARY",
                FieldType.ROWVERSION => "TIMESTAMP",
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
    }
}