using Allvis.Kaylee.Analyzer;
using Allvis.Kaylee.Validator.SqlServer.Extensions;
using Allvis.Kaylee.Validator.SqlServer.Models.DB;
using Allvis.Kaylee.Validator.SqlServer.Options;
using CommandLine;
using Microsoft.Data.SqlClient;
using SqlKata.Compilers;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Allvis.Kaylee.Validator.SqlServer
{
    class Program
    {
        static Task Main(string[] args)
        {
            return Parser.Default
                .ParseArguments<CommandLineOptions>(args)
                .WithParsedAsync(MainWithCommandLineOptions);
        }

        private static async Task MainWithCommandLineOptions(CommandLineOptions options)
        {
            var model = ReadModel(options.Directory);
            var ast = KayleeHelper.Parse(model);

            using var conn = new SqlConnection(options.ConnectionString);
            using var db = new QueryFactory(conn, new SqlServerCompiler(), options.Timeout);

            var schemata = await GetSchemata(db).ConfigureAwait(false);
            var tables = await GetTables(db).ConfigureAwait(false);
            var columns = await GetColumns(db).ConfigureAwait(false);
            var tableConstraints = await GetTableConstraints(db).ConfigureAwait(false);
            var referentialConstraints = await GetReferentialConstraints(db).ConfigureAwait(false);
            var constraintColumns = await GetConstraintColumns(db).ConfigureAwait(false);

            ResolveReferences(schemata, tables, columns, tableConstraints, referentialConstraints, constraintColumns);

            Validate(ast, schemata);
        }

        private static void Validate(Analyzer.Models.Ast ast, IEnumerable<Schema> schemata)
        {
            foreach (var expectedSchema in ast.Schemata)
            {
                Schema actualSchema;
                try
                {
                    actualSchema = schemata.Single(s => s.Name == expectedSchema.Name);
                }
                catch (InvalidOperationException)
                {
                    MissingSchema(expectedSchema);
                    continue;
                }

                foreach (var expectedEntity in expectedSchema.Entities)
                {
                    Validate(expectedEntity, actualSchema);
                }
            }
        }

        private static void Validate(Analyzer.Models.Entity expectedEntity, Schema actualSchema)
        {
            if (!expectedEntity.IsQuery)
            {
                ValidateTable(expectedEntity, actualSchema);
            }
            ValidateView(expectedEntity, actualSchema);
            foreach (var child in expectedEntity.Children)
            {
                Validate(child, actualSchema);
            }
        }

        private static void ValidateTable(Analyzer.Models.Entity expectedEntity, Schema actualSchema)
        {
            Table actualTable;
            try
            {
                actualTable = actualSchema.Tables.Single(t => t.Name == expectedEntity.GetTableName());
            }
            catch (InvalidOperationException)
            {
                MissingTable(expectedEntity);
                return;
            }
            ValidateTableColumns(expectedEntity, actualTable);
            // TODO: Continue here, check referential constraints
        }

        private static void ValidateTableColumns(Analyzer.Models.Entity expectedEntity, Table actualTable)
        {
            foreach (var expectedField in expectedEntity.Fields)
            {
                Column actualColumn;
                try
                {
                    actualColumn = actualTable.Columns.Single(c => c.Name == expectedField.Name);
                }
                catch (InvalidOperationException)
                {
                    MissingTableColumn(expectedField);
                    continue;
                }
            }
        }

        private static void ValidateView(Analyzer.Models.Entity expectedEntity, Schema actualSchema)
        {
            Table actualTable;
            try
            {
                actualTable = actualSchema.Tables.Single(t => t.Name == expectedEntity.GetViewName());
            }
            catch (InvalidOperationException)
            {
                MissingView(expectedEntity);
                return;
            }
            ValidateViewColumns(expectedEntity, actualTable);
        }

        private static void ValidateViewColumns(Analyzer.Models.Entity expectedEntity, Table actualTable)
        {
            foreach (var expectedField in expectedEntity.Fields)
            {
                Column actualColumn;
                try
                {
                    actualColumn = actualTable.Columns.Single(c => c.Name == expectedField.Name);
                }
                catch (InvalidOperationException)
                {
                    MissingViewColumn(expectedField);
                    continue;
                }
            }
        }

        private static void MissingViewColumn(Analyzer.Models.Field field)
        {
            Console.WriteLine($"The column {field.Name} is missing from view {field.Entity.GetFullyQualifiedView()}");
            Console.WriteLine(field.Entity.GetCreateOrAlterViewStatement());
            // TODO: Write to disk files
        }

        private static void MissingTableColumn(Analyzer.Models.Field field)
        {
            if (field.Computed)
            {
                Console.WriteLine($"The computed column {field.Name} is missing from table {field.Entity.GetFullyQualifiedTable()}");
                Console.WriteLine($"ALTER TABLE {field.Entity.GetFullyQualifiedTable()} ADD [{field.Name}] AS /* insert the computed expression here */;");
                // TODO: Write to disk files
            }
            else
            {
                Console.WriteLine($"The column {field.Name} is missing from table {field.Entity.GetFullyQualifiedTable()}");
                Console.WriteLine($"ALTER TABLE {field.Entity.GetFullyQualifiedTable()} ADD {field.GetSqlServerSpecification()};");
                // TODO: Write to disk files
            }
        }

        private static void MissingView(Analyzer.Models.Entity entity)
        {
            Console.WriteLine($"The view {entity.GetFullyQualifiedView()} is missing");
            Console.WriteLine(entity.GetCreateOrAlterViewStatement());
            // TODO: Write to disk files
        }

        private static void MissingTable(Analyzer.Models.Entity entity)
        {
            Console.WriteLine($"The table {entity.GetFullyQualifiedTable()} is missing");
            Console.WriteLine(entity.GetCreateTableStatement());
            // TODO: Write to disk files
        }

        private static void MissingSchema(Analyzer.Models.Schema schema)
        {
            Console.WriteLine($"The schema [{schema.Name}] is missing");
            Console.WriteLine($"CREATE SCHEMA [{schema.Name}];");
            // TODO: Write to disk files
        }

        private static void ResolveReferences(
            IEnumerable<Schema> schemata,
            IEnumerable<Table> tables,
            IEnumerable<Column> columns,
            IEnumerable<TableConstraint> tableConstraints,
            IEnumerable<ReferentialConstraint> referentialConstraints,
            IEnumerable<ConstraintColumn> constraintColumns)
        {
            foreach (var table in tables)
            {
                var schema = schemata.Single(s => s.Name == table.Schema);
                schema.Tables.Add(table);
            }
            foreach (var column in columns)
            {
                var table = tables.Single(t => t.Schema == column.Schema && t.Name == column.Table);
                table.Columns.Add(column);
            }
            foreach (var constraint in tableConstraints)
            {
                var table = tables.Single(t => t.Schema == constraint.TableSchema && t.Name == constraint.TableName);
                table.Constraints.Add(constraint);
            }
            foreach (var constraint in referentialConstraints)
            {
                constraint.Source = tableConstraints.Single(c => c.Schema == constraint.Schema && c.Name == constraint.Name);
                constraint.Target = tableConstraints.Single(c => c.Schema == constraint.TargetSchema && c.Name == constraint.TargetName);
                constraint.Source.ReferentialConstraint = constraint;
            }
            foreach (var column in constraintColumns)
            {
                var constraint = tableConstraints.Single(c => c.Schema == column.ConstraintSchema && c.Name == column.ConstraintName);
                constraint.Columns.Add(column);
            }
        }

        private static Task<IEnumerable<Schema>> GetSchemata(QueryFactory db)
            => db.Query("INFORMATION_SCHEMA.SCHEMATA")
                .Select("SCHEMA_NAME as Name")
                .OrderBy("SCHEMA_NAME")
                .GetAsync<Schema>();

        private static Task<IEnumerable<Table>> GetTables(QueryFactory db)
            => db.Query("INFORMATION_SCHEMA.TABLES")
                .Select("TABLE_SCHEMA as Schema")
                .Select("TABLE_NAME as Name")
                .Select("TABLE_TYPE as Type")
                .OrderBy("TABLE_SCHEMA", "TABLE_NAME")
                .GetAsync<Table>();

        private static Task<IEnumerable<Column>> GetColumns(QueryFactory db)
            => db.Query("INFORMATION_SCHEMA.COLUMNS")
                .Select("TABLE_SCHEMA as Schema")
                .Select("TABLE_NAME as Table")
                .Select("COLUMN_NAME as Name")
                .Select("COLUMN_DEFAULT as Default")
                .SelectRaw("CASE IS_NULLABLE WHEN 'NO' THEN 0 ELSE 1 END as Nullable")
                .SelectRaw("UPPER(DATA_TYPE) as Type")
                .Select("CHARACTER_MAXIMUM_LENGTH as Length")
                .Select("NUMERIC_PRECISION as Precision")
                .Select("NUMERIC_SCALE as Scale")
                .OrderBy("TABLE_SCHEMA", "TABLE_NAME", "COLUMN_NAME")
                .GetAsync<Column>();

        private static Task<IEnumerable<TableConstraint>> GetTableConstraints(QueryFactory db)
            => db.Query("INFORMATION_SCHEMA.TABLE_CONSTRAINTS")
                .Select("CONSTRAINT_SCHEMA as Schema")
                .Select("CONSTRAINT_NAME as Name")
                .Select("TABLE_SCHEMA as TableSchema")
                .Select("TABLE_NAME as TableName")
                .Select("CONSTRAINT_TYPE as Type")
                .Where("CONSTRAINT_TYPE", "<>", "CHECK")
                .OrderBy("CONSTRAINT_TYPE", "CONSTRAINT_SCHEMA", "CONSTRAINT_NAME")
                .GetAsync<TableConstraint>();

        private static Task<IEnumerable<ReferentialConstraint>> GetReferentialConstraints(QueryFactory db)
            => db.Query("INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS")
                .Select("CONSTRAINT_SCHEMA as Schema")
                .Select("CONSTRAINT_NAME as Name")
                .Select("UNIQUE_CONSTRAINT_SCHEMA as TargetSchema")
                .Select("UNIQUE_CONSTRAINT_NAME as TargetName")
                .SelectRaw("CASE UPDATE_RULE WHEN 'CASCADE' THEN 1 ELSE 0 END as CascadingUpdates")
                .SelectRaw("CASE DELETE_RULE WHEN 'CASCADE' THEN 1 ELSE 0 END as CascadingDeletes")
                .OrderBy("CONSTRAINT_SCHEMA", "CONSTRAINT_NAME")
                .GetAsync<ReferentialConstraint>();

        private static Task<IEnumerable<ConstraintColumn>> GetConstraintColumns(QueryFactory db)
            => db.Query("INFORMATION_SCHEMA.KEY_COLUMN_USAGE")
                .Select("CONSTRAINT_SCHEMA as ConstraintSchema")
                .Select("CONSTRAINT_NAME as ConstraintName")
                .Select("TABLE_SCHEMA as TableSchema")
                .Select("TABLE_NAME as TableName")
                .Select("ORDINAL_POSITION as Position")
                .Select("COLUMN_NAME as Name")
                .OrderBy("CONSTRAINT_SCHEMA", "CONSTRAINT_NAME", "ORDINAL_POSITION")
                .GetAsync<ConstraintColumn>();

        private static string ReadModel(string directory)
        {
            var searchDirectory = string.IsNullOrWhiteSpace(directory) ? Directory.GetCurrentDirectory() : directory;
            var sb = new StringBuilder();
            var files = Directory.GetFiles(searchDirectory, "*.kay");
            if (files.Length == 0)
            {
                throw new InvalidOperationException($"The directory {searchDirectory} contains no *.kay files.");
            }
            foreach (var file in files)
            {
                sb.AppendLine(File.ReadAllText(file, Encoding.UTF8));
            }
            return sb.ToString();
        }

        private static void PrintDebugInfo(
            IEnumerable<Schema> schemata,
            IEnumerable<Table> tables,
            IEnumerable<Column> columns,
            IEnumerable<TableConstraint> tableConstraints,
            IEnumerable<ReferentialConstraint> referentialConstraints,
            IEnumerable<ConstraintColumn> constraintColumns)
        {
            Console.WriteLine("The following schemata exist in the database:");
            foreach (var schema in schemata)
            {
                Console.WriteLine(schema.Name);
            }
            Console.WriteLine();
            Console.WriteLine("The following tables exist in the database:");
            foreach (var table in tables.Where(t => t.IsTable))
            {
                Console.WriteLine($"{table.Schema}.{table.Name}");
            }
            Console.WriteLine();
            Console.WriteLine("The following views exist in the database:");
            foreach (var view in tables.Where(t => t.IsView))
            {
                Console.WriteLine($"{view.Schema}.{view.Name}");
            }
            Console.WriteLine();
            Console.WriteLine("The following columns exist in the database:");
            foreach (var column in columns)
            {
                Console.WriteLine($"{column.Schema}.{column.Table}.{column.Name}{(column.Nullable ? "?" : "")} {column.Type.ToUpperInvariant()}{column.TypeArgument}");
            }
            Console.WriteLine();
            Console.WriteLine("The following table constraints exist in the database:");
            foreach (var constraint in tableConstraints)
            {
                Console.WriteLine($"{constraint.Type}: {constraint.Schema}.{constraint.Name}");
            }
            Console.WriteLine();
            Console.WriteLine("The following referential constraints exist in the database:");
            foreach (var constraint in referentialConstraints)
            {
                Console.WriteLine($"{constraint.Schema}.{constraint.Name} => {constraint.TargetSchema}.{constraint.TargetName}");
                if (constraint.CascadingUpdates)
                {
                    Console.WriteLine($"    ON UPDATE CASCADE");
                }
                if (constraint.CascadingDeletes)
                {
                    Console.WriteLine($"    ON DELETE CASCADE");
                }
            }
            Console.WriteLine();
            Console.WriteLine("The following constraint columns exist in the database:");
            foreach (var column in constraintColumns)
            {
                Console.WriteLine($"{column.ConstraintSchema}.{column.ConstraintName}: {column.Name}");
            }
            Console.WriteLine();
        }
    }
}
