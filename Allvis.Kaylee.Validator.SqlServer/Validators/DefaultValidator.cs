using System;
using System.Collections.Generic;
using System.Linq;
using Allvis.Kaylee.Validator.SqlServer.Extensions;
using Allvis.Kaylee.Validator.SqlServer.Models.DB;
using Allvis.Kaylee.Validator.SqlServer.Reporters;

namespace Allvis.Kaylee.Validator.SqlServer.Validators
{
    public class DefaultValidator : IValidator
    {
        private readonly IReporter reporter;

        public int Issues { get; private set; }

        public DefaultValidator(IReporter reporter)
        {
            this.reporter = reporter;
        }

        public void Validate(Analyzer.Models.Ast expected, IEnumerable<Schema> actual)
        {
            foreach (var expectedSchema in expected.Schemata)
            {
                Schema actualSchema;
                try
                {
                    actualSchema = actual.Single(s => s.Name == expectedSchema.Name);
                }
                catch (InvalidOperationException)
                {
                    Issues++;
                    reporter.ReportMissingSchema(expectedSchema);
                    continue;
                }

                foreach (var expectedEntity in expectedSchema.Entities)
                {
                    ValidateEntity(expectedEntity, actualSchema);
                }
            }
        }

        private void ValidateEntity(Analyzer.Models.Entity expected, Schema actualSchema)
        {
            if (!expected.IsQuery)
            {
                ValidateTable(expected, actualSchema);
            }
            ValidateView(expected, actualSchema);
            foreach (var child in expected.Children)
            {
                ValidateEntity(child, actualSchema);
            }
        }

        private void ValidateTable(Analyzer.Models.Entity expectedEntity, Schema actualSchema)
        {
            Table actualTable;
            try
            {
                actualTable = actualSchema.Tables.Single(t => t.Name == expectedEntity.GetTableName());
            }
            catch (InvalidOperationException)
            {
                Issues++;
                reporter.ReportMissingTable(expectedEntity);
                return;
            }
            foreach (var expectedField in expectedEntity.Fields)
            {
                ValidateColumn(expectedField, actualTable);
            }
            var ukIdx = 1;
            foreach (var key in expectedEntity.UniqueKeys)
            {
                ValidateUniqueKey(key, actualTable, ref ukIdx);
            }
            // TODO: Continue here, check referential constraints
        }

        private void ValidateUniqueKey(Analyzer.Models.UniqueKey expectedUniqueKey, Table actualTable, ref int index)
        {
            TableConstraint actualConstraint;
            try
            {
                actualConstraint = actualTable.Constraints.Where(c => c.IsUnique).Single(constraint =>
                {
                    if (constraint.Columns.Count != expectedUniqueKey.FieldReferences.Count)
                    {
                        return false;
                    }
                    foreach (var expectedField in expectedUniqueKey.FieldReferences.Select(fr => fr.ResolvedField))
                    {
                        if (!constraint.Columns.Any(c => c.Name == expectedField.Name))
                        {
                            return false;
                        }
                    }
                    return true;
                });
            }
            catch (InvalidOperationException)
            {
                var i = index;
                while (actualTable.Constraints.Any(c => c.Name == expectedUniqueKey.GetUniqueKeySpecificationName(i)))
                {
                    i++;
                }
                reporter.ReportMissingUniqueKey(expectedUniqueKey, i);
                index = i + 1;
                return;
            }
        }

        private void ValidateView(Analyzer.Models.Entity expectedEntity, Schema actualSchema)
        {
            Table actualTable;
            try
            {
                actualTable = actualSchema.Tables.Single(t => t.Name == expectedEntity.GetViewName());
            }
            catch (InvalidOperationException)
            {
                Issues++;
                reporter.ReportMissingView(expectedEntity);
                return;
            }
            foreach (var expectedField in expectedEntity.Fields)
            {
                ValidateColumn(expectedField, actualTable, isView: true);
            }
        }

        private void ValidateColumn(Analyzer.Models.Field expectedField, Table actualTable, bool isView = false)
        {
            Column actualColumn;
            try
            {
                actualColumn = actualTable.Columns.Single(c => c.Name == expectedField.Name);
            }
            catch (InvalidOperationException)
            {
                Issues++;
                if (isView)
                {
                    reporter.ReportMissingViewColumn(expectedField);
                }
                else
                {
                    reporter.ReportMissingTableColumn(expectedField);
                }
                return;
            }
            if (!isView)
            {
                var sameNullability = expectedField.Computed || actualColumn.Nullable == expectedField.Nullable;
                var sameLength = !actualColumn.HasLength || (actualColumn.Length == -1 && expectedField.Size.IsMax) || actualColumn.Length == expectedField.Size.Size || (expectedField.Type == Analyzer.Enums.FieldType.CHAR && actualColumn.Length == 1);
                var samePrecision = !actualColumn.HasPrecision || (actualColumn.Precision == expectedField.Size.Size && actualColumn.Scale == expectedField.Size.Precision);
                var sameDefault = (actualColumn.Default?.ToUpperInvariant() ?? string.Empty) == (expectedField.DefaultExpression?.ToUpperInvariant() ?? string.Empty);
                var sameType = actualColumn.Type == expectedField.Type.GetRawSqlServerType();
                if (!sameNullability || !sameLength || !samePrecision || !sameDefault || !sameType)
                {
                    var hint = "";
                    if (!sameNullability) {
                        hint += " nullability";
                    }
                    if (!sameLength) {
                        hint += " length";
                    }
                    if (!samePrecision) {
                        hint += " precision/scale";
                    }
                    if (!sameDefault) {
                        hint += " default";
                    }
                    if (!sameType) {
                        hint += " type";
                    }
                    reporter.ReportIncorrectTableColumn(expectedField, hint.Trim());
                }
            }
        }
    }
}