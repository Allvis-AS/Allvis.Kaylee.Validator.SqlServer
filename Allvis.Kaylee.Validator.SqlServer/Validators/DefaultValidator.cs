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
            // TODO: Continue here, check referential constraints
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
        }
    }
}