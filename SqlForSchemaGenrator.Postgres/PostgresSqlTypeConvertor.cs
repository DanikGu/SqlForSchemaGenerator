using SqlForSchemaGenerator.Core.Interfaces;
using SqlForSchemaGenerator.Core.Models;

namespace SqlForSchemaGenrator.Postgres
{
    public class PostgresSqlTypeConvertor : ISqlTypesConverter
    {
        private readonly Dictionary<SystemTypesEnum, string> _typeMapping = new()
        {
            { SystemTypesEnum.Integer,"INT4"},
            { SystemTypesEnum.SmallInteger,"INT2"},
            { SystemTypesEnum.BigInteger,"INT8"},
            { SystemTypesEnum.Decimal,"DECIMAL"},
            { SystemTypesEnum.Real,"REAL"},
            { SystemTypesEnum.Double,"DOUBLE PRECISION"},
            { SystemTypesEnum.UnlimitedText,"TEXT"},
            { SystemTypesEnum.LimitedText,"VARCHAR"},
            { SystemTypesEnum.Date,"DATE"},
            { SystemTypesEnum.Time,"TIME"},
            { SystemTypesEnum.Timestamp,"TIMESTAMP"},
            { SystemTypesEnum.Boolean,"BOOLEAN"},
            { SystemTypesEnum.Binary,"BYTEA"},
            { SystemTypesEnum.UUID,"UUID" }
        };
        private readonly Dictionary<string, SystemTypesEnum> _reversedTypeMapping;
        public PostgresSqlTypeConvertor()
        {
            _reversedTypeMapping = _typeMapping.ToDictionary(x => x.Value, x => x.Key);
        }
        public string GetSqlType(SystemTypesEnum systemType)
        {
            if (_typeMapping.TryGetValue(systemType, out var sqlType))
            {
                return sqlType;
            }
            throw new ArgumentException("Unsupported SystemTypeEnum value");
        }

        public SystemTypesEnum GetSystemType(string sqlType)
        {
            if (sqlType.ToUpper() == "NUMERIC") 
            {
                sqlType = "DECIMAL";
            }
            if (sqlType.ToUpper() == "TIMESTAMPTZ")
            {
                sqlType = "TIMESTAMP";
            }
            if (_reversedTypeMapping.TryGetValue(sqlType.ToUpper(), out var systemType))
            {
                return systemType;
            }
            throw new ArgumentException("Unsupported SQL type");
        }


    }
}
