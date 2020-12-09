using System.Collections.Generic;
using System.Threading.Tasks;
using Allvis.Kaylee.Validator.SqlServer.Models.DB;
using SqlKata.Execution;

namespace Allvis.Kaylee.Validator.SqlServer.Extensions
{
    public static class QueryFactoryExtensions
    {
        public static Task<IEnumerable<Schema>> GetSchemata(this QueryFactory db)
            => db.Query("INFORMATION_SCHEMA.SCHEMATA")
                .Select("SCHEMA_NAME as Name")
                .OrderBy("SCHEMA_NAME")
                .GetAsync<Schema>();

        public static Task<IEnumerable<Table>> GetTables(this QueryFactory db)
            => db.Query("INFORMATION_SCHEMA.TABLES")
                .Select("TABLE_SCHEMA as Schema")
                .Select("TABLE_NAME as Name")
                .Select("TABLE_TYPE as Type")
                .OrderBy("TABLE_SCHEMA", "TABLE_NAME")
                .GetAsync<Table>();

        public static Task<IEnumerable<Column>> GetColumns(this QueryFactory db)
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

        public static Task<IEnumerable<TableConstraint>> GetTableConstraints(this QueryFactory db)
            => db.Query("INFORMATION_SCHEMA.TABLE_CONSTRAINTS")
                .Select("CONSTRAINT_SCHEMA as Schema")
                .Select("CONSTRAINT_NAME as Name")
                .Select("TABLE_SCHEMA as TableSchema")
                .Select("TABLE_NAME as TableName")
                .Select("CONSTRAINT_TYPE as Type")
                .Where("CONSTRAINT_TYPE", "<>", "CHECK")
                .OrderBy("CONSTRAINT_TYPE", "CONSTRAINT_SCHEMA", "CONSTRAINT_NAME")
                .GetAsync<TableConstraint>();

        public static Task<IEnumerable<ReferentialConstraint>> GetReferentialConstraints(this QueryFactory db)
            => db.Query("INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS")
                .Select("CONSTRAINT_SCHEMA as Schema")
                .Select("CONSTRAINT_NAME as Name")
                .Select("UNIQUE_CONSTRAINT_SCHEMA as TargetSchema")
                .Select("UNIQUE_CONSTRAINT_NAME as TargetName")
                .SelectRaw("CASE UPDATE_RULE WHEN 'CASCADE' THEN 1 ELSE 0 END as CascadingUpdates")
                .SelectRaw("CASE DELETE_RULE WHEN 'CASCADE' THEN 1 ELSE 0 END as CascadingDeletes")
                .OrderBy("CONSTRAINT_SCHEMA", "CONSTRAINT_NAME")
                .GetAsync<ReferentialConstraint>();

        public static Task<IEnumerable<ConstraintColumn>> GetConstraintColumns(this QueryFactory db)
            => db.Query("INFORMATION_SCHEMA.KEY_COLUMN_USAGE")
                .Select("CONSTRAINT_SCHEMA as ConstraintSchema")
                .Select("CONSTRAINT_NAME as ConstraintName")
                .Select("TABLE_SCHEMA as TableSchema")
                .Select("TABLE_NAME as TableName")
                .Select("ORDINAL_POSITION as Position")
                .Select("COLUMN_NAME as Name")
                .OrderBy("CONSTRAINT_SCHEMA", "CONSTRAINT_NAME", "ORDINAL_POSITION")
                .GetAsync<ConstraintColumn>();
    }
}
