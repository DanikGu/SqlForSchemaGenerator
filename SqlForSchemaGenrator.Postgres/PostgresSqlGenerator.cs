using SqlForSchemaGenerator.Core;
using SqlForSchemaGenerator.Core.Interfaces;
using SqlForSchemaGenerator.Core.Models;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace SqlForSchemaGenrator.Postgres
{
    public class PostgresSqlGenerator: ISqlGenerator
    {
        private readonly ISqlTypesConverter _sqlTypesConverter;

        public PostgresSqlGenerator(ISqlTypesConverter sqlTypesConverter)
        {
            _sqlTypesConverter = sqlTypesConverter;
        }
        public string GetSql(DiffChecker checker)
        {
            var actions = checker.GetActionsToAchiveTargetStructure();
            return GenerateSqlFromActions(actions.ToArray());
        }
        public string GenerateSqlFromActions(DBAction[] dBActions) 
        {
            return string.Join("\n", dBActions.Select(x => SqlFromAction(x)));
        }
        private string SqlFromAction(DBAction dBAction) 
        {
            var result = string.Empty;
            dBAction.ObjectName = WrapObjectName(dBAction.ObjectName);
            switch (dBAction.Type) 
            {
                case DBActionType.CreateTable:
                    result = $"CREATE TABLE {dBAction.ObjectName}();";
                    break;

                case DBActionType.CreateField:
                    if (dBAction.Props is ActionFieldProps fieldProps1)
                    {
                        var fieldSizeStr = fieldProps1.FieldInitialSize is null ? "" : $"({fieldProps1.FieldInitialSize})";
                        fieldProps1.TableName = WrapObjectName(fieldProps1.TableName);
                        result = $"ALTER TABLE {fieldProps1.TableName} ADD COLUMN {dBAction.ObjectName} {fieldProps1.FieldInitialType}{fieldSizeStr};";
                        if (fieldProps1.IsPrimaryKey)
                        {
                            result += $"\nALTER TABLE {fieldProps1.TableName} ADD PRIMARY KEY ({dBAction.ObjectName});";
                        }
                    }
                    break;

                case DBActionType.UpdateType:
                    if (dBAction.Props is ActionFieldProps fieldProps2)
                    {
                        if (fieldProps2.FieldInitialType != fieldProps2.FieldTargetType || fieldProps2.FieldInitialSize != fieldProps2.FieldTargetSize)
                        {
                            var fieldSizeStr = fieldProps2.FieldTargetSize is null ? "" : $"({fieldProps2.FieldTargetSize})";
                            fieldProps2.TableName = WrapObjectName(fieldProps2.TableName);
                            result = $"ALTER TABLE {fieldProps2.TableName} ALTER COLUMN {dBAction.ObjectName} TYPE {fieldProps2.FieldTargetType}{fieldSizeStr};";
                        }
                        if (fieldProps2.IsPrimaryKey != fieldProps2.WasPrimaryKey)
                        {
                            result += $"ALTER TABLE {fieldProps2.TableName} ADD PRIMARY KEY ({dBAction.ObjectName});";
                        }
                    }
                    break;

                case DBActionType.CreateRelationship:
                    if (dBAction.Props is ActionRelationshipProps relationshipProps)
                    {
                        relationshipProps.TableName = WrapObjectName(relationshipProps.TableName);
                        relationshipProps.TableFieldName = WrapObjectName(relationshipProps.TableFieldName);
                        relationshipProps.ReferencedTableName = WrapObjectName(relationshipProps.ReferencedTableName);
                        relationshipProps.ReferencedTableFieldName = WrapObjectName(relationshipProps.ReferencedTableFieldName);
                        result = $"ALTER TABLE {relationshipProps.TableName} ADD FOREIGN KEY ({relationshipProps.TableFieldName}) " +
                            $"REFERENCES {relationshipProps.ReferencedTableName}({relationshipProps.ReferencedTableFieldName});";
                    }
                    break;

                case DBActionType.DeleteRelationship:
                    if (dBAction.Props is ActionRelationshipProps relationshipProps2) 
                    {
                        relationshipProps2.TableName = WrapObjectName(relationshipProps2.TableName);
                        result = $"ALTER TABLE {relationshipProps2.TableName} DROP CONSTRAINT {dBAction.ObjectName};";
                    }
                    break;

                case DBActionType.DeleteField:
                    
                    result = $"ALTER TABLE {(dBAction.Props is ActionFieldProps fieldPropsDel ? WrapObjectName(fieldPropsDel.TableName) :
                        throw new ArgumentException())} " +
                        $"DROP COLUMN {dBAction.ObjectName};";
                    break;

                case DBActionType.DeleteTable:
                    result = $"DROP TABLE {dBAction.ObjectName};";
                    break;

            }
            return result;
        }
        private string WrapObjectName(string objectName) 
        {
            objectName = $"\"{objectName}\"";
            return objectName;
        }

    }
}
 
        
        
        
        
        
        
        