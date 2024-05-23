using SqlForSchemaGenerator.Core.Interfaces;
using SqlForSchemaGenerator.Core.Models;
using System.Data;

namespace SqlForSchemaGenrator.Postgres;

public class PostgresDbStructureBuilder : IDbStructureBuilder
{

    //private readonly string _connectionString;
    private DbStructure _dbStructure;
    private readonly IDbConnection _connnection;
    private readonly ISqlTypesConverter _sqlTypesConverter;

    public PostgresDbStructureBuilder(IDbConnection connection, ISqlTypesConverter sqlTypesConverter)
    {
        _connnection = connection;
        _sqlTypesConverter = sqlTypesConverter;
    }
    public DbStructure Build()
    {
        _dbStructure = new DbStructure();

        BuildTables();

        BuildFields();

        BuildRelationship();

        return _dbStructure;
    }
    private void BuildTables()
    {
        var query = TABLES_QUERY;
        var tables = new List<Table>();

        using (var command = _connnection.CreateCommand())
        {
            command.CommandText = query;
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var table = new Table();
                    table.Name = reader.GetString(0);
                    tables.Add(table);
                }
            }
        }

        _dbStructure.Tables = tables.ToArray();
    }
    private void BuildFields()
    {
        foreach (var table in _dbStructure.Tables)
        {
            var fields = new List<Field>();
            var query = GetQueryToListFields(table.Name);
            using (var command = _connnection.CreateCommand())
            {
                command.CommandText = query;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var field = new Field();
                        field.Name = reader["column_name"] as string;
                        field.Type = _sqlTypesConverter.GetSystemType(reader["udt_name"] as string);
                        field.Size = reader["character_maximum_length"] as int?;
                        fields.Add(field);
                    }
                }
            }
            table.Fields = fields.ToArray();
            query = GetPrimaryKeyForTableQuery(table.Name);
            using (var command = _connnection.CreateCommand())
            {
                command.CommandText = query;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string columnName = reader["key_column"] as string;
                        var field = fields.First(x => x.Name == columnName);
                        field.IsPrimaryKey = true;
                    }
                }
            }

        }

    }
    private void BuildRelationship()
    {
        var tablesDictionary = _dbStructure.Tables.ToDictionary(x => x.Name, y => y);
        foreach (var table in _dbStructure.Tables)
        {
            var fieldsDictionary = table.Fields.ToDictionary(x => x.Name, y => y);
            var relationships = new List<Relationship>();
            var query = GetQueryToListConstraints(table.Name);
            using (var command = _connnection.CreateCommand())
            {
                command.CommandText = query;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var type = reader["constraint_type"] as string;
                        if (type != "FOREIGN KEY")
                        {
                            continue;
                        }
                        var relationship = new Relationship();
                        relationship.ConstraintName = reader["constraint_name"] as string;
                        relationships.Add(relationship);
                    }
                }
            }
            table.Relationships = relationships.ToArray();
            foreach (var relationship in table.Relationships)
            {

                var queryToGetConstraintDetail = GetQueryToConstraintsDeteails(table.Name, relationship.ConstraintName);
                using (var command = _connnection.CreateCommand())
                {
                    command.CommandText = queryToGetConstraintDetail;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var columnName = reader["column_name"] as string ?? throw new ArgumentException();
                            var referencesTableName = reader["references_table"] as string ?? throw new ArgumentException();
                            var referencedField = reader["references_field"] as string ?? throw new ArgumentException();

                            var referencedTable = tablesDictionary[referencesTableName];
                            var column = fieldsDictionary[columnName];
                            relationship.Table = table;
                            relationship.Field = column;
                            relationship.ReferencedTable = referencedTable;
                            relationship.ReferencedField = referencedTable.Fields.First(x => x.Name == referencedField);
                        }
                    }
                }
            }
        }

    }

    private const string TABLES_QUERY = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_type = 'BASE TABLE'
              AND table_schema NOT IN
                  ('pg_catalog', 'information_schema');
        """;

    private const string VIEWS_QUERY = """
            SELECT table_name
            FROM information_schema.views
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
            AND table_name !~ '^pg_';
        """;

    private string GetQueryToListFields(string table)
    {
        return $"""
            SELECT 
                ordinal_position,
                column_name,
                data_type,
                udt_name,
                column_default,
                is_nullable,
                character_maximum_length,
                numeric_precision,
                is_identity
            FROM information_schema.columns
            WHERE table_name = '{table}'
            ORDER BY ordinal_position;
        """;
    }
    private string GetQueryToListConstraints(string table)
    {
        return $"""
            SELECT constraint_name, constraint_type
                FROM information_schema.table_constraints
                WHERE table_name = '{table}';

            """;
    }
    private string GetQueryToConstraintsDeteails(string table, string constraintName)
    {
        return $"""
            SELECT tc.constraint_name,
                          tc.constraint_type,
                          tc.table_name,
                          kcu.column_name,
                      tc.is_deferrable,
                          tc.initially_deferred,
                          rc.match_option AS match_type,
                          rc.update_rule AS on_update,
                          rc.delete_rule AS on_delete,
                          ccu.table_name AS references_table,
                          ccu.column_name AS references_field
                     FROM information_schema.table_constraints tc
                LEFT JOIN information_schema.key_column_usage kcu
                       ON tc.constraint_catalog = kcu.constraint_catalog
                      AND tc.constraint_schema = kcu.constraint_schema
                      AND tc.constraint_name = kcu.constraint_name
                LEFT JOIN information_schema.referential_constraints rc
                       ON tc.constraint_catalog = rc.constraint_catalog
                      AND tc.constraint_schema = rc.constraint_schema
                      AND tc.constraint_name = rc.constraint_name
                LEFT JOIN information_schema.constraint_column_usage ccu
                       ON rc.unique_constraint_catalog = ccu.constraint_catalog
                      AND rc.unique_constraint_schema = ccu.constraint_schema
                      AND rc.unique_constraint_name = ccu.constraint_name
                    WHERE tc.table_name = '{table}'
                      AND tc.constraint_name = '{constraintName}';
            """;
    }

    private string GetPrimaryKeyForTableQuery(string tableName) => $"""
        select kcu.table_schema,
               kcu.table_name,
               tco.constraint_name,
               kcu.ordinal_position as position,
               kcu.column_name as key_column
        from information_schema.table_constraints tco
        join information_schema.key_column_usage kcu 
             on kcu.constraint_name = tco.constraint_name
             and kcu.constraint_schema = tco.constraint_schema
             and kcu.constraint_name = tco.constraint_name
        where tco.constraint_type = 'PRIMARY KEY' and kcu.table_name = '{tableName}'
        order by kcu.table_schema,
                 kcu.table_name,
                 position;
        """;


}
