using SqlForSchemaGenerator.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SqlForSchemaGenerator.Core
{
    public class DiffChecker
    {
        private DbStructure _currentStructure { get; set; }
        private DbStructure _targetStructure { get; set; }
        public DiffChecker(DbStructure currentStructure, DbStructure targetStructure)
        {
            _currentStructure = currentStructure;
            _targetStructure = targetStructure;
        }
        //changing of relationship is not considered and if it happened such case will not have any affect on actions 
        //supposed that old constraint deleted and new created
        //will delete relationship that was pointed to deleted tables or deleted fields
        public List<DBAction> GetActionsToAchiveTargetStructure() 
        {
            var result = new List<DBAction>();
            var currTables = _currentStructure.Tables.ToDictionary(x => x.Name, y => y);
            var targetTables = _targetStructure.Tables.ToDictionary(x => x.Name, y => y);

            var tablesToCreate = targetTables.Where(x => !currTables.ContainsKey(x.Key)).
                Select(x => new DBAction() {
                    ObjectName = x.Key,
                    Type = DBActionType.CreateTable
                }
            );
            var tablesToDelete = currTables.Where(x => !targetTables.ContainsKey(x.Key)).
                Select(x => new DBAction()
                {
                    ObjectName = x.Key,
                    Type = DBActionType.DeleteTable
                }
            );
            var toCreateTables = new HashSet<string>(tablesToCreate.Select(x => x.ObjectName));
            
            var fieldsToCreate = new List<DBAction>();
            var fieldsToUpdate = new List<DBAction>();
            var fieldsToDelete = new List<DBAction>();
            var relationshipToDelete = new List<DBAction>();
            var relationshipToCreate = new List<DBAction>();

            //populate this set to later delete all references that pointing to this field
            var sentencedFields = new HashSet<(string Tabel, string Field)>();
            var sentencedTables = new HashSet<string>(tablesToDelete.Select(x => x.ObjectName));

            foreach (var table in targetTables) 
            {
                if (toCreateTables.Contains(table.Key)) 
                {
                    fieldsToCreate.AddRange(table.Value.Fields.Select(x => 
                        new DBAction() 
                        {
                            Type = DBActionType.CreateField,
                            ObjectName = x.Name,
                            Props = new ActionFieldProps() 
                            {
                                FieldInitialSize = x.Size,
                                FieldTargetSize = x.Size,
                                FieldInitialType = x.Type,
                                FieldTargetType = x.Type,
                                TableName = table.Key,
                                IsPrimaryKey = x.IsPrimaryKey
                            }
                        }
                    ));
                    continue;
                }
                var currentTable = currTables[table.Key];
                var currentFields = currentTable.Fields;
                var currentRelationships = currentTable.Relationships;

                fieldsToCreate.AddRange(
                    table.Value.Fields.Where(x => !currentFields.Any(y => y.Name == x.Name)).
                        Select(x => new DBAction() 
                        {
                            Type= DBActionType.CreateField,
                            ObjectName = x.Name,
                            Props = new ActionFieldProps() {
                                FieldInitialSize = x.Size,
                                FieldTargetSize = x.Size,
                                FieldInitialType = x.Type,
                                FieldTargetType  = x.Type,
                                TableName = table.Key,
                                IsPrimaryKey = x.IsPrimaryKey
                            }
                        }
                ));

                var toDeleteFields = currentFields.Where(x => !table.Value.Fields.Any(y => y.Name == x.Name));
                toDeleteFields.ToList().ForEach(x => sentencedFields.Add((table.Key, x.Name)));
                fieldsToDelete.AddRange(
                    toDeleteFields.
                        Select(x => new DBAction()
                        {
                            Type = DBActionType.DeleteField,
                            ObjectName = x.Name,
                            Props = new ActionFieldProps()
                            {
                                FieldInitialType = x.Type,
                                FieldTargetType = x.Type,
                                TableName = table.Key,
                                IsPrimaryKey = x.IsPrimaryKey
                            }
                        }
                ));

                foreach (var field in currentFields) 
                {
                    var targetField = table.Value.Fields.FirstOrDefault(x => x.Name == field.Name);
                    if (targetField is null) { continue; }

                    if (field.Type == targetField.Type && field.IsPrimaryKey == targetField.IsPrimaryKey && field.Size == targetField.Size) { continue; }


                    fieldsToUpdate.Add(new DBAction()
                    {
                        Type = DBActionType.UpdateType,
                        ObjectName = field.Name,
                        Props = new ActionFieldProps()
                        {
                            FieldInitialSize = field.Size,
                            FieldTargetSize = targetField.Size,
                            FieldInitialType = field.Type,
                            FieldTargetType = targetField.Type,
                            TableName = table.Key,
                            IsPrimaryKey = targetField.IsPrimaryKey,
                            WasPrimaryKey = field.IsPrimaryKey
                        }
                    });
                }

            }

            var oldRealationships = _currentStructure.GetAllRelationships().ToDictionary(x => x.ConstraintName, y => y);
            var targetRealationships = _targetStructure.GetAllRelationships().ToDictionary(x => x.ConstraintName, y => y);
            relationshipToCreate.AddRange(
                targetRealationships.
                    Where(x => !oldRealationships.ContainsKey(x.Key)).
                    Select(x => new DBAction() 
                    {
                        Type = DBActionType.CreateRelationship,
                        ObjectName = x.Key,
                        Props = new ActionRelationshipProps() 
                        {
                            TableFieldName = x.Value.FieldName,
                            TableName = x.Value.Table.Name,
                            ReferencedTableFieldName = x.Value.ReferencedFieldName,
                            ReferencedTableName = x.Value.ReferncedTableName
                        }
                    })
            );
            relationshipToDelete.AddRange(
               oldRealationships.
                   Where(x => !targetRealationships.ContainsKey(x.Key)).
                   Select(x => new DBAction()
                   {
                       Type = DBActionType.DeleteRelationship,
                       ObjectName = x.Key,
                       Props = new ActionRelationshipProps()
                       {
                           TableFieldName = x.Value.FieldName,
                           TableName = x.Value.Table.Name,
                           ReferencedTableFieldName = x.Value.ReferencedFieldName,
                           ReferencedTableName = x.Value.ReferncedTableName
                       }
                   })
            );
            //questionable part do I need a part where I will delete relationships
            //that was pointed to deleted fields or tables or consumer of this class have
            //to fix such issues, are this is expected behavior ?

            var relatedToDeletedParts = oldRealationships.Where(x =>
                sentencedTables.Contains(x.Value.ReferncedTableName) ||
                sentencedTables.Contains(x.Value.Table.Name) ||
                sentencedFields.Contains((x.Value.Table.Name, x.Value.FieldName)) ||
                sentencedFields.Contains((x.Value.ReferncedTableName, x.Value.ReferencedFieldName))
            );
            relationshipToDelete.AddRange(relatedToDeletedParts.
                Select(x => new DBAction()
                {
                    Type = DBActionType.DeleteRelationship,
                    ObjectName = x.Key,
                    Props = new ActionRelationshipProps()
                    {
                        TableFieldName = x.Value.FieldName,
                        TableName = x.Value.Table.Name,
                        ReferencedTableFieldName = x.Value.ReferencedFieldName,
                        ReferencedTableName = x.Value.ReferncedTableName
                    }
                }
            ));
            relationshipToDelete = relationshipToDelete.DistinctBy(x => x.ObjectName).ToList();
            result.AddRange(tablesToCreate);
            result.AddRange(tablesToDelete);
            result.AddRange(fieldsToCreate);
            result.AddRange(fieldsToUpdate);
            result.AddRange(fieldsToDelete);
            result.AddRange(relationshipToDelete);
            result.AddRange(relationshipToCreate);
            
            return result.OrderBy(x => x.Type).ToList();
        }
    }
    
    
    
}