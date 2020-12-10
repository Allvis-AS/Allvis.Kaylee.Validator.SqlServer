using System;
using System.Collections.Generic;
using System.Linq;
using Allvis.Kaylee.Analyzer.Extensions;
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
                Schema? actualSchema = null;
                try
                {
                    actualSchema = actual.Single(s => s.Name == expectedSchema.Name);
                }
                catch (InvalidOperationException)
                {
                    Issues++;
                    reporter.ReportMissingSchema(expectedSchema);
                }

                foreach (var expectedEntity in expectedSchema.Entities)
                {
                    ValidateEntity(expectedEntity, actualSchema);
                }
            }
        }

        private void ValidateEntity(Analyzer.Models.Entity expected, Schema? actualSchema)
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

        private void ValidateTable(Analyzer.Models.Entity expectedEntity, Schema? actualSchema)
        {
            Table actualTable;
            try
            {
                actualTable = (actualSchema?.Tables ?? Enumerable.Empty<Table>()).Single(t => t.Name == expectedEntity.GetTableName());
            }
            catch (InvalidOperationException)
            {
                Issues++;
                reporter.ReportMissingTable(expectedEntity);
                return;
            }
            foreach (var expectedField in expectedEntity.GetAllFields())
            {
                ValidateColumn(expectedEntity, expectedField, actualTable);
            }
            var ukIdx = 1;
            foreach (var key in expectedEntity.UniqueKeys)
            {
                ValidateUniqueKey(key, actualTable, ref ukIdx);
            }
            var refIdx = 1;
            if (expectedEntity.Parent != null)
            {
                ValidateParentReference(expectedEntity, actualTable, ref refIdx);
            }
            foreach (var reference in expectedEntity.References)
            {
                ValidateReference(reference, actualTable, ref refIdx);
            }
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
        private void ValidateParentReference(Analyzer.Models.Entity expectedEntity, Table actualTable, ref int index)
        {
            var parentKey = expectedEntity.GetParentKey().ToList();
            TableConstraint actualConstraint;
            try
            {
                actualConstraint = actualTable.Constraints.Where(c => c.IsForeign).Single(c =>
                {
                    var constraint = c.ReferentialConstraint!;
                    // Same counts
                    if (constraint.Source!.Columns.Count != parentKey.Count)
                    {
                        return false;
                    }
                    // Same schemata
                    if (constraint.Source!.TableSchema != expectedEntity.Schema.Name
                        || constraint.Target!.TableSchema != expectedEntity.Parent.Schema.Name)
                    {
                        return false;
                    }
                    // Same tables
                    if (constraint.Source!.TableName != expectedEntity.GetTableName()
                        || constraint.Target!.TableName != expectedEntity.Parent.GetTableName())
                    {
                        return false;
                    }
                    // Same columns and ordering
                    var sourceColumns = constraint.Source!.Columns.OrderBy(c => c.Position).Select(c => c.Name).ToList();
                    var targetColumns = constraint.Target!.Columns.OrderBy(c => c.Position).Select(c => c.Name).ToList();
                    for (var i = 0; i < sourceColumns.Count; i++)
                    {
                        var source = sourceColumns[i];
                        var target = targetColumns[i];
                        var expectedSource = parentKey[i];
                        var expectedTarget = parentKey[i];
                        if (source != expectedSource.Name || target != expectedTarget.Name)
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
                while (actualTable.Constraints.Any(c => c.Name == expectedEntity.GetParentForeignKeySpecificationName(i)))
                {
                    i++;
                }
                reporter.ReportMissingParentForeignKey(expectedEntity, i);
                index = i + 1;
                return;
            }
        }

        private void ValidateReference(Analyzer.Models.Reference expectedReference, Table actualTable, ref int index)
        {
            TableConstraint actualConstraint;
            try
            {
                actualConstraint = actualTable.Constraints.Where(c => c.IsForeign).Single(c =>
                {
                    var constraint = c.ReferentialConstraint!;
                    // Same counts
                    if (constraint.Source!.Columns.Count != expectedReference.Source.Count)
                    {
                        return false;
                    }
                    // Same schemata
                    if (constraint.Source!.TableSchema != expectedReference.Source.Last().SchemaName
                        || constraint.Target!.TableSchema != expectedReference.Target.Last().SchemaName)
                    {
                        return false;
                    }
                    // Same tables
                    if (constraint.Source!.TableName != expectedReference.Source.Last().ResolvedField.Entity.GetTableName()
                        || constraint.Target!.TableName != expectedReference.Target.Last().ResolvedField.Entity.GetTableName())
                    {
                        return false;
                    }
                    // Same columns and ordering
                    var sourceColumns = constraint.Source!.Columns.OrderBy(c => c.Position).Select(c => c.Name).ToList();
                    var targetColumns = constraint.Target!.Columns.OrderBy(c => c.Position).Select(c => c.Name).ToList();
                    for (var i = 0; i < sourceColumns.Count; i++)
                    {
                        var source = sourceColumns[i];
                        var target = targetColumns[i];
                        var expectedSource = expectedReference.Source[i];
                        var expectedTarget = expectedReference.Target[i];
                        if (source != expectedSource.FieldName || target != expectedTarget.FieldName)
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
                while (actualTable.Constraints.Any(c => c.Name == expectedReference.GetForeignKeySpecificationName(i)))
                {
                    i++;
                }
                reporter.ReportMissingForeignKey(expectedReference, i);
                index = i + 1;
                return;
            }
        }

        private void ValidateView(Analyzer.Models.Entity expectedEntity, Schema? actualSchema)
        {
            Table actualTable;
            try
            {
                actualTable = (actualSchema?.Tables ?? Enumerable.Empty<Table>()).Single(t => t.Name == expectedEntity.GetViewName());
            }
            catch (InvalidOperationException)
            {
                Issues++;
                reporter.ReportMissingView(expectedEntity);
                return;
            }
            foreach (var expectedField in expectedEntity.GetAllFields())
            {
                ValidateColumn(expectedEntity, expectedField, actualTable, isView: true);
            }
        }

        private void ValidateColumn(Analyzer.Models.Entity expectedEntity, Analyzer.Models.Field expectedField, Table actualTable, bool isView = false)
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
                    reporter.ReportMissingTableColumn(expectedEntity, expectedField);
                }
                return;
            }
            if (!isView)
            {
                var sameNullability = expectedField.Computed || actualColumn.Nullable == expectedField.Nullable;
                var sameLength = !actualColumn.HasLength || (actualColumn.Length == -1 && expectedField.Size.IsMax) || actualColumn.Length == expectedField.Size.Size || (expectedField.Type == Analyzer.Enums.FieldType.CHAR && actualColumn.Length == 1);
                var samePrecision = !actualColumn.HasPrecision || (actualColumn.Precision == expectedField.Size.Size && actualColumn.Scale == expectedField.Size.Precision);
                var sameDefault = (actualColumn.Default?.ToUpperInvariant() ?? string.Empty) == (expectedField.DefaultExpression?.ToUpperInvariant() ?? string.Empty);
                var sameAutoIncrement = expectedEntity.IsPartOfParentKey(expectedField)
                    ? !actualColumn.IsIdentity
                    : actualColumn.IsIdentity == expectedField.AutoIncrement; // WHY ISNT THIS REPORTING CORRECTLY
                if (actualTable.Name == "tbl_TenantProcedureRevisionExecutionCommentReaction" && actualColumn.Name == "CommentId")
                {
                    //Console.WriteLine(expectedField.Entity.DisplayName);
                    //Console.WriteLine(actualColumn.Table);
                    //Console.WriteLine(actualColumn.Name);
                    //Console.WriteLine(expectedField.AutoIncrement);
                    //Console.WriteLine(expectedField.IsPartOfParentKey());
                    //Console.WriteLine(string.Join(", ", expectedField.Entity.GetFullPrimaryKey().Select(fr => fr.FieldName)));
                    //Console.WriteLine(string.Join(", ", expectedField.Entity.GetParentKey().Select(f => f.Name)));
                    //Console.WriteLine(string.Join(", ", expectedField.Entity.PrimaryKey.Select(fr => fr.FieldName)));
                    //Console.WriteLine(expectedField.AutoIncrement && !expectedField.IsPartOfParentKey());
                }
                var sameType = actualColumn.Type == expectedField.Type.GetRawSqlServerType();
                if (!sameNullability || !sameLength || !samePrecision || !sameDefault || !sameAutoIncrement || !sameType)
                {
                    var hint = "";
                    if (!sameNullability)
                    {
                        hint += " nullability";
                    }
                    if (!sameLength)
                    {
                        hint += " length";
                    }
                    if (!samePrecision)
                    {
                        hint += " precision/scale";
                    }
                    if (!sameDefault)
                    {
                        hint += " default";
                    }
                    if (!sameAutoIncrement)
                    {
                        hint += " identity";
                    }
                    if (!sameType)
                    {
                        hint += " type";
                    }
                    reporter.ReportIncorrectTableColumn(expectedEntity, expectedField, hint.Trim());
                }
            }
        }
    }
}