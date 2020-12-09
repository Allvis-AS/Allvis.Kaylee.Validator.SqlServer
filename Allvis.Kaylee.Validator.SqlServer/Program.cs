using Allvis.Kaylee.Analyzer;
using Allvis.Kaylee.Validator.SqlServer.Extensions;
using Allvis.Kaylee.Validator.SqlServer.Models.DB;
using Allvis.Kaylee.Validator.SqlServer.Options;
using Allvis.Kaylee.Validator.SqlServer.Validators;
using CommandLine;
using Microsoft.Data.SqlClient;
using SqlKata.Compilers;
using SqlKata.Execution;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using Allvis.Kaylee.Validator.SqlServer.Reporters;

namespace Allvis.Kaylee.Validator.SqlServer
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var exitCode = 0;
            try
            {
                await Parser.Default
                    .ParseArguments<CommandLineOptions>(args)
                    .WithParsedAsync(async options =>
                    {
                        var expected = GetExpected(options.Directory);
                        var actual = await GetActual(options.ConnectionString, options.Timeout).ConfigureAwait(false);
                        using var reporter = GetReporter(options.OutFile);
                        var validator = new DefaultValidator(reporter);
                        validator.Validate(expected, actual);
                        exitCode = validator.Issues;
                    }).ConfigureAwait(false);
            }
            catch (Exception e) when (e is IOException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ResetColor();
                return -1;
            }
            return exitCode;
        }

        private static Analyzer.Models.Ast GetExpected(string schemaDirectory)
        {
            var model = GetExpectedModel(schemaDirectory);
            return KayleeHelper.Parse(model);
        }

        private static string GetExpectedModel(string directory)
        {
            var searchDirectory = string.IsNullOrWhiteSpace(directory) ? Directory.GetCurrentDirectory() : directory;
            var sb = new StringBuilder();
            var files = Directory.GetFiles(searchDirectory, "*.kay");
            if (files.Length == 0)
            {
                throw new IOException($"The directory {searchDirectory} contains no *.kay files.");
            }
            foreach (var file in files)
            {
                sb.AppendLine(File.ReadAllText(file, Encoding.UTF8));
            }
            return sb.ToString();
        }

        private static async Task<IEnumerable<Schema>> GetActual(string connectionString, int timeout)
        {
            using var conn = new SqlConnection(connectionString);
            using var db = new QueryFactory(conn, new SqlServerCompiler(), timeout);
            var schemata = await db.GetSchemata().ConfigureAwait(false);
            var tables = await db.GetTables().ConfigureAwait(false);
            var columns = await db.GetColumns().ConfigureAwait(false);
            var tableConstraints = await db.GetTableConstraints().ConfigureAwait(false);
            var referentialConstraints = await db.GetReferentialConstraints().ConfigureAwait(false);
            var constraintColumns = await db.GetConstraintColumns().ConfigureAwait(false);
            ResolveReferences(schemata, tables, columns, tableConstraints, referentialConstraints, constraintColumns);
            return schemata;
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

        private static IReporter GetReporter(string outFile)
        {
            if (string.IsNullOrWhiteSpace(outFile))
            {
                return new ConsoleReporter();
            }
            return new FileReporter(outFile);
        }
    }
}
