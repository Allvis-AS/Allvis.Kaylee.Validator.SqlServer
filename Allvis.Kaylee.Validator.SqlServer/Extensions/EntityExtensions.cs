using Allvis.Kaylee.Analyzer.Enums;
using Allvis.Kaylee.Analyzer.Extensions;
using Allvis.Kaylee.Validator.SqlServer.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Allvis.Kaylee.Validator.SqlServer.Extensions
{
    public static class EntityExtensions
    {
        public static string GetFullyQualifiedTable(this Analyzer.Models.Entity entity)
            => $"[{entity.Schema.Name}].[{entity.GetTableName()}]";
        public static string GetFullyQualifiedView(this Analyzer.Models.Entity entity)
            => $"[{entity.Schema.Name}].[{entity.GetViewName()}]";

        public static string GetTableName(this Analyzer.Models.Entity entity)
            => $"tbl_{entity.GetFullName()}";
        public static string GetViewName(this Analyzer.Models.Entity entity)
            => $"v_{entity.GetFullName()}";

        private static string GetFullName(this Analyzer.Models.Entity entity)
            => string.Join(string.Empty, entity.Path);

        public static string GetCreateOrAlterViewStatement(this Analyzer.Models.Entity entity)
        {
            var sb = new SourceBuilder();
            sb.AL($"CREATE OR ALTER VIEW {entity.GetFullyQualifiedView()}");
            sb.AL("AS");
            sb.I(sb =>
            {
                sb.AL("SELECT");
                sb.I(sb =>
                {
                    var allFields = entity.GetAllFields();
                    var alignedSources = allFields.Select(f => $"[t].[{f.Name}]").AlignLeft().ToList();
                    allFields.ForEach((field, index, last) =>
                    {
                        var source = alignedSources[index];
                        var comma = last ? string.Empty : ",";
                        sb.AL($"{source}[{field.Name}]{comma}");
                    });
                });
                sb.AL("FROM");
                sb.I(sb =>
                {
                    sb.AL($"{entity.GetFullyQualifiedTable()} AS [t]");
                });
            });
            sb.AL("WITH CHECK OPTION;");
            sb.AL("GO");
            return sb.ToString();
        }

        public static string GetCreateTableStatement(this Analyzer.Models.Entity entity)
        {
            var sb = new SourceBuilder();
            sb.AL($"CREATE TABLE {entity.GetFullyQualifiedTable()}");
            sb.AL("(");
            sb.I(sb =>
            {
                var allFields = entity.GetAllFields();
                var width = allFields.Select(f => $"[{f.Name}]").GetAlignPadWidth();
                foreach (var field in allFields)
                {
                    sb.AL($"{entity.GetSqlServerSpecification(field, width)},");
                }
                sb.A(entity.GetPrimaryKeySpecification(), indent: true);
                entity.UniqueKeys.ForEach((key, index, last) =>
                {
                    sb.A(",");
                    sb.NL();
                    sb.A(GetUniqueKeySpecification(key, index + 1), indent: true);
                });
                sb.NL();
            });
            sb.AL(");");
            return sb.ToString();
        }

        public static string GetPrimaryKeySpecification(this Analyzer.Models.Entity entity)
        {
            var columns = string.Join(", ", entity.GetFullPrimaryKey().Select(fr => $"[{fr.FieldName}] ASC"));
            return $"CONSTRAINT PK_{entity.GetTableName()} PRIMARY KEY CLUSTERED({columns})";
        }

        public static string GetUniqueKeySpecification(this Analyzer.Models.UniqueKey key, int idx)
        {
            var columns = string.Join(", ", key.FieldReferences.Select(fr => $"[{fr.FieldName}] ASC"));
            return $"CONSTRAINT {key.GetUniqueKeySpecificationName(idx)} UNIQUE NONCLUSTERED({columns})";
        }

        public static string GetUniqueKeySpecificationName(this Analyzer.Models.UniqueKey key, int idx)
            => $"UK_{key.Entity.GetTableName()}_{idx.ToString().PadLeft(2, '0')}";

        public static string GetParentForeignKeySpecification(this Analyzer.Models.Entity entity, int idx)
        {
            var columns = string.Join(", ", entity.GetParentKey().Select(fr => $"[{fr.Name}]"));
            var targetTable = entity.Parent.GetFullyQualifiedTable();
            return $"CONSTRAINT {entity.GetParentForeignKeySpecificationName(idx)} FOREIGN KEY({columns}) REFERENCES {targetTable}({columns}) ON DELETE CASCADE";
        }

        public static string GetParentForeignKeySpecificationName(this Analyzer.Models.Entity entity, int idx)
            => $"FK_{entity.GetTableName()}_{idx.ToString().PadLeft(2, '0')}";

        public static string GetForeignKeySpecification(this Analyzer.Models.Reference reference, int idx)
        {
            var sourceColumns = string.Join(", ", reference.Source.Select(fr => $"[{fr.FieldName}]"));
            var targetColumns = string.Join(", ", reference.Target.Select(fr => $"[{fr.FieldName}]"));
            var targetTable = reference.Target.Last().ResolvedField.Entity.GetFullyQualifiedTable();
            return $"CONSTRAINT {reference.GetForeignKeySpecificationName(idx)} FOREIGN KEY({sourceColumns}) REFERENCES {targetTable}({targetColumns})";
        }

        public static string GetForeignKeySpecificationName(this Analyzer.Models.Reference reference, int idx)
            => $"FK_{reference.Source.Last().ResolvedField.Entity.GetTableName()}_{idx.ToString().PadLeft(2, '0')}";

        public static string GetSqlServerSpecification(this Analyzer.Models.Entity entity, Analyzer.Models.Field field, int nameWidth = 0)
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
            if (field.Computed)
            {
                sb.Append("/* Insert the computation expression here */");
            }
            else
            {
                sb.Append(entity.GetSqlServerTypeWithExtras(field));
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
            }
            return sb.ToString();
        }

        public static string GetSqlServerTypeWithExtras(this Analyzer.Models.Entity entity, Analyzer.Models.Field field)
            => field.Type switch
            {
                FieldType.BIT => "BIT",
                FieldType.TINYINT => "TINYINT",
                FieldType.INT => $"INT{(field.AutoIncrement && !entity.IsPartOfParentKey(field) ? " IDENTITY(1, 1)" : string.Empty)}",
                FieldType.BIGINT => $"BIGINT{(field.AutoIncrement && !entity.IsPartOfParentKey(field) ? " IDENTITY(1, 1)" : string.Empty)}",
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

        public static bool IsPartOfParentKey(this Analyzer.Models.Entity entity, Analyzer.Models.Field field)
            => entity.GetParentKey().Contains(field);

        public static IEnumerable<Analyzer.Models.Field> GetParentKey(this Analyzer.Models.Entity entity)
            => entity.GetFullPrimaryKey().Select(fr => fr.ResolvedField).Except(entity.PrimaryKey.Select(fr => fr.ResolvedField));

        public static IEnumerable<Analyzer.Models.Field> GetAllFields(this Analyzer.Models.Entity entity)
            => entity.GetFullPrimaryKey().Select(fr => fr.ResolvedField).Concat(entity.Fields).Distinct();
    }
}
