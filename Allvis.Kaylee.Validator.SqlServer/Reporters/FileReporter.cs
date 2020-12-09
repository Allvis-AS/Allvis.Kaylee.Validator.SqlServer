using System;
using System.IO;
using System.Text;
using Allvis.Kaylee.Analyzer.Models;
using Allvis.Kaylee.Validator.SqlServer.Extensions;

namespace Allvis.Kaylee.Validator.SqlServer.Reporters
{
    public class FileReporter : IReporter
    {
        private readonly TextWriter writer;

        public FileReporter(string file)
        {
            writer = new StreamWriter(File.Open(file, FileMode.CreateNew), Encoding.UTF8, leaveOpen: false);
        }

        public void ReportMissingSchema(Schema schema)
        {
            WriteComment($"The schema [{schema.Name}] is missing:");
            WriteLine($"CREATE SCHEMA [{schema.Name}];");
            WriteLine("GO");
            WriteLine();
        }

        public void ReportMissingTable(Entity entity)
        {
            WriteComment($"The table {entity.GetFullyQualifiedTable()} is missing:");
            WriteLine(entity.GetCreateTableStatement());
            WriteLine();
        }

        public void ReportMissingView(Entity entity)
        {
            WriteComment($"The view {entity.GetFullyQualifiedView()} is missing:");
            WriteLine(entity.GetCreateOrAlterViewStatement());
            WriteLine();
        }

        public void ReportMissingTableColumn(Field field)
        {
            if (field.Computed)
            {
                WriteComment($"The computed column [{field.Name}] is missing from table {field.Entity.GetFullyQualifiedTable()}:");
                WriteLine($"ALTER TABLE {field.Entity.GetFullyQualifiedTable()} ADD [{field.Name}] AS /* insert the computed expression here */;");
                WriteLine();
            }
            else
            {
                WriteComment($"The column [{field.Name}] is missing from table {field.Entity.GetFullyQualifiedTable()}:");
                WriteLine($"ALTER TABLE {field.Entity.GetFullyQualifiedTable()} ADD {field.GetSqlServerSpecification()};");
                WriteLine();
            }
        }

        public void ReportMissingViewColumn(Field field)
        {
            WriteComment($"The column [{field.Name}] is missing from view {field.Entity.GetFullyQualifiedView()}:");
            WriteLine(field.Entity.GetCreateOrAlterViewStatement());
            WriteLine();
        }

        public void ReportMissingUniqueKey(UniqueKey uniqueKey, int index)
        {
            WriteComment($"The table {uniqueKey.Entity.GetFullyQualifiedTable()} is missing the unique key [{uniqueKey.GetUniqueKeySpecificationName(index)}]:");
            WriteLine($"ALTER TABLE {uniqueKey.Entity.GetFullyQualifiedTable()} ADD {uniqueKey.GetUniqueKeySpecification(index)};");
            WriteLine();
        }

        public void ReportIncorrectTableColumn(Field field, string hint)
        {
            WriteComment($"The column [{field.Name}] in table {field.Entity.GetFullyQualifiedTable()} does not match its specification:");
            WriteComment($"*HINT* {hint}");
            WriteComment($"TODO: ALTER TABLE {field.Entity.GetFullyQualifiedTable()} ALTER COLUMN {field.GetSqlServerSpecification()};");
            WriteLine();
        }

        private void WriteComment(string comment)
        {
            writer.WriteLine($"-- {comment}");
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