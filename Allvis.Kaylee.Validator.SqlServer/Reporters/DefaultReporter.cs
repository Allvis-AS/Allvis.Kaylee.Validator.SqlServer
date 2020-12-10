using System;
using System.Collections.Generic;
using System.Linq;
using Allvis.Kaylee.Analyzer.Models;
using Allvis.Kaylee.Validator.SqlServer.Extensions;
using Allvis.Kaylee.Validator.SqlServer.Writers;

namespace Allvis.Kaylee.Validator.SqlServer.Reporters
{
    public class DefaultReporter : IReporter
    {
        private readonly IWriter writer;

        private readonly List<Schema> missingSchemata = new();
        private readonly List<Entity> missingTables = new();
        private readonly List<Entity> missingViews = new();
        private readonly List<(Entity Entity, Field Field)> missingTableColumns = new();
        private readonly List<Field> missingViewColumns = new();
        private readonly List<(UniqueKey UniqueKey, int Index)> missingUniqueKeys = new();
        private readonly List<(Entity Entity, int Index)> missingParentForeignKeys = new();
        private readonly List<(Reference Reference, int Index)> missingForeignKeys = new();
        private readonly List<(Entity Entity, Field Field, string Hint)> incorrectTableColumns = new();

        public DefaultReporter(IWriter writer)
        {
            this.writer = writer;
        }

        public void ReportMissingSchema(Schema schema)
        {
            missingSchemata.Add(schema);
        }

        public void ReportMissingTable(Entity entity)
        {
            missingTables.Add(entity);
        }

        public void ReportMissingView(Entity entity)
        {
            missingViews.Add(entity);
        }

        public void ReportMissingTableColumn(Entity entity, Field field)
        {
            missingTableColumns.Add((entity, field));
        }

        public void ReportMissingViewColumn(Field field)
        {
            missingViewColumns.Add(field);
        }

        public void ReportIncorrectTableColumn(Entity entity, Field field, string hint)
        {
            incorrectTableColumns.Add((entity, field, hint));
        }

        public void ReportMissingUniqueKey(UniqueKey uniqueKey, int index)
        {
            missingUniqueKeys.Add((uniqueKey, index));
        }

        public void ReportMissingParentForeignKey(Entity entity, int index)
        {
            missingParentForeignKeys.Add((entity, index));
        }

        public void ReportMissingForeignKey(Reference reference, int index)
        {
            missingForeignKeys.Add((reference, index));
        }

        private void Write()
        {
            WriteMissingSchemata();
            WriteMissingTables();
            WriteMissingViews();
            WriteMissingTableColumns();
            WriteMissingViewColumns();
            WriteIncorrectTableColumns();
            WriteMissingUniqueKeys();
            WriteMissingParentForeignKeys();
            WriteMissingForeignKeys();
        }

        private void WriteMissingSchemata()
        {
            WriteComment($"The following schemata are missing:");
            foreach (var schema in missingSchemata)
            {
                WriteLine($"CREATE SCHEMA [{schema.Name}];");
                WriteLine("GO");
            }
            WriteLine();
            missingSchemata.Clear();
        }

        private void WriteMissingTables()
        {
            WriteComment($"The following tables are missing:");
            foreach (var entity in missingTables)
            {
                WriteLine(entity.GetCreateTableStatement());
            }
            if (missingTables.Any())
            {
                WriteLine("GO");
            }
            WriteLine();
            missingTables.Clear();
        }

        private void WriteMissingViews()
        {
            WriteComment($"The following views are missing:");
            foreach (var entity in missingViews)
            {
                if (entity.IsQuery)
                {
                    WriteComment($"TODO: CREATE OR ALTER VIEW {entity.GetFullyQualifiedView()} AS /* Insert the view definition here */;");
                }
                else
                {
                    WriteLine(entity.GetCreateOrAlterViewStatement());
                }
            }
            WriteLine();
            missingViews.Clear();
        }

        private void WriteMissingTableColumns()
        {
            WriteMissingTableColumnsComputed();
            WriteMissingTableColumnsPersisted();
            missingTableColumns.Clear();
        }

        private void WriteMissingTableColumnsComputed()
        {
            WriteComment($"The following computed columns are missing from tables:");
            foreach (var tuple in missingTableColumns.Where(c => c.Field.Computed))
            {
                WriteLine($"ALTER TABLE {tuple.Entity.GetFullyQualifiedTable()} ADD [{tuple.Field.Name}] AS /* Insert the computation expression here */;");
            }
            WriteLine();
        }

        private void WriteMissingTableColumnsPersisted()
        {
            WriteComment($"The following persisted columns are missing from tables:");
            foreach (var tuple in missingTableColumns.Where(c => !c.Field.Computed))
            {
                WriteLine($"ALTER TABLE {tuple.Entity.GetFullyQualifiedTable()} ADD {tuple.Entity.GetSqlServerSpecification(tuple.Field)};");
            }
            WriteLine();
        }

        private void WriteMissingViewColumns()
        {
            WriteComment($"The following views have missing columns:");
            foreach (var field in missingViewColumns)
            {
                WriteComment($"*COLUMN: [{field.Name}]");
                WriteLine(field.Entity.GetCreateOrAlterViewStatement());
            }
            WriteLine();
            missingViewColumns.Clear();
        }

        private void WriteIncorrectTableColumns()
        {
            WriteComment($"The following columns do not match their specification:");
            foreach (var tuple in incorrectTableColumns)
            {
                WriteComment($"*HINT: {tuple.Hint}");
                WriteComment($"TODO: ALTER TABLE {tuple.Entity.GetFullyQualifiedTable()} ALTER COLUMN {tuple.Entity.GetSqlServerSpecification(tuple.Field)};");
            }
            WriteLine();
            incorrectTableColumns.Clear();
        }

        private void WriteMissingUniqueKeys()
        {
            WriteComment($"The following unique keys are missing from tables:");
            foreach (var tuple in missingUniqueKeys)
            {
                WriteLine($"ALTER TABLE {tuple.UniqueKey.Entity.GetFullyQualifiedTable()} ADD {tuple.UniqueKey.GetUniqueKeySpecification(tuple.Index)};");
            }
            WriteLine();
            missingUniqueKeys.Clear();
        }

        private void WriteMissingParentForeignKeys()
        {
            WriteComment($"The following parent foreign keys are missing from tables:");
            foreach (var tuple in missingParentForeignKeys)
            {
                WriteLine($"ALTER TABLE {tuple.Entity.GetFullyQualifiedTable()} ADD {tuple.Entity.GetParentForeignKeySpecification(tuple.Index)};");
            }
            WriteLine();
            missingParentForeignKeys.Clear();
        }

        private void WriteMissingForeignKeys()
        {
            WriteComment($"The following foreign keys are missing from tables:");
            foreach (var tuple in missingForeignKeys)
            {
                WriteLine($"ALTER TABLE {tuple.Reference.Source.Last().ResolvedField.Entity.GetFullyQualifiedTable()} ADD {tuple.Reference.GetForeignKeySpecification(tuple.Index)};");
            }
            WriteLine();
            missingForeignKeys.Clear();
        }

        private void WriteComment(string comment)
        {
            writer.WriteComment(comment);
        }

        private void WriteLine(string line = "")
        {
            writer.WriteLine(line);
        }

        #region IDisposable support
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Write();
                    writer?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}