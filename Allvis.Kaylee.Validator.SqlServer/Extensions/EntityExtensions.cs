using Allvis.Kaylee.Analyzer.Extensions;
using Allvis.Kaylee.Validator.SqlServer.Builders;
using System.Collections.Generic;
using System.Linq;

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
                    sb.AL($"{field.GetSqlServerSpecification(width)},");
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
            sb.AL("GO");
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

        private static IEnumerable<Analyzer.Models.Field> GetAllFields(this Analyzer.Models.Entity entity)
            => entity.GetFullPrimaryKey().Select(fr => fr.ResolvedField).Concat(entity.Fields).Distinct();
    }
}
