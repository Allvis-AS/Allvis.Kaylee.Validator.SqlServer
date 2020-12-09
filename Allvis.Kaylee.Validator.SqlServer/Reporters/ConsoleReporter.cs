using System;
using Allvis.Kaylee.Analyzer.Models;
using Allvis.Kaylee.Validator.SqlServer.Extensions;

namespace Allvis.Kaylee.Validator.SqlServer.Reporters
{
    public class ConsoleReporter : IReporter
    {
        public void ReportMissingSchema(Schema schema)
        {
            SetColorDescription();
            Console.WriteLine($"The schema [{schema.Name}] is missing:");
            SetColorResolution();
            Console.WriteLine($"CREATE SCHEMA [{schema.Name}];");
            Console.WriteLine();
            Console.ResetColor();
        }

        public void ReportMissingTable(Entity entity)
        {
            SetColorDescription();
            Console.WriteLine($"The table {entity.GetFullyQualifiedTable()} is missing:");
            SetColorResolution();
            Console.WriteLine(entity.GetCreateTableStatement());
            Console.WriteLine();
            Console.ResetColor();
        }

        public void ReportMissingView(Entity entity)
        {
            SetColorDescription();
            Console.WriteLine($"The view {entity.GetFullyQualifiedView()} is missing:");
            SetColorResolution();
            Console.WriteLine(entity.GetCreateOrAlterViewStatement());
            Console.WriteLine();
            Console.ResetColor();
        }

        public void ReportMissingTableColumn(Field field)
        {
            if (field.Computed)
            {
                SetColorDescription();
                Console.WriteLine($"The computed column {field.Name} is missing from table {field.Entity.GetFullyQualifiedTable()}:");
                SetColorResolution();
                Console.WriteLine($"ALTER TABLE {field.Entity.GetFullyQualifiedTable()} ADD [{field.Name}] AS /* insert the computed expression here */;");
                Console.WriteLine();
                Console.ResetColor();
            }
            else
            {
                SetColorDescription();
                Console.WriteLine($"The column {field.Name} is missing from table {field.Entity.GetFullyQualifiedTable()}:");
                SetColorResolution();
                Console.WriteLine($"ALTER TABLE {field.Entity.GetFullyQualifiedTable()} ADD {field.GetSqlServerSpecification()};");
                Console.WriteLine();
                Console.ResetColor();
            }
        }

        public void ReportMissingViewColumn(Field field)
        {
            SetColorDescription();
            Console.WriteLine($"The column {field.Name} is missing from view {field.Entity.GetFullyQualifiedView()}:");
            SetColorResolution();
            Console.WriteLine(field.Entity.GetCreateOrAlterViewStatement());
            Console.WriteLine();
            Console.ResetColor();
        }

        private static void SetColorDescription()
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        private static void SetColorResolution()
        {
            Console.ResetColor();
        }
    }
}