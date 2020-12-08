using Allvis.Kaylee.Analyzer.Extensions;
using Allvis.Kaylee.Validator.SqlServer.Builders;
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
                    var width = 6 + allFields.Max(f => f.Name.Length);
                    var remainder = width % 4;
                    width += 4 - remainder;
                    allFields.ForEach((field, last) =>
                    {
                        var source = $"[t].[{field.Name}]".PadRight(width);
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
                var width = 2 + allFields.Max(f => f.Name.Length);
                var remainder = width % 4;
                width += 4 - remainder;
                foreach (var field in allFields)
                {
                    sb.AL($"{field.GetSqlServerSpecification(width)},");
                }
                var pk = string.Join(", ", entity.GetFullPrimaryKey().Select(fr => $"[{fr.FieldName}] ASC"));
                sb.A($"CONSTRAINT PK_{entity.GetTableName()} PRIMARY KEY CLUSTERED({pk})", indent: true);

                var ukIdx = 1;
                foreach (var key in entity.UniqueKeys)
                {
                    var ukName = $"UK_{entity.GetTableName()}_{ukIdx.ToString().PadLeft(2, '0')}";
                    var uk = string.Join(", ", key.FieldReferences.Select(fr => $"[{fr.FieldName}] ASC"));
                    sb.A(",");
                    sb.NL();
                    sb.A($"CONSTRAINT {ukName} UNIQUE NONCLUSTERED({uk})", indent: true);
                    ukIdx++;
                }
                sb.NL();
            });
            sb.AL(");");
            sb.AL("GO");
            return sb.ToString();
        }

        private static IEnumerable<Analyzer.Models.Field> GetAllFields(this Analyzer.Models.Entity entity)
            => entity.GetFullPrimaryKey().Select(fr => fr.ResolvedField).Concat(entity.Fields).Distinct();
    }
}
