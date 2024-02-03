using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlForSchemaGenerator.Core.Models
{
    //their values will be same as their priorities when sorting out actions
    public enum DBActionType
    {
        CreateTable = 0,
        CreateField = 1,
        UpdateType = 2,
        CreateRelationship = 3,
        DeleteRelationship = 4,
        DeleteField = 5,
        DeleteTable = 6
    }

    public class DBAction
    {
        public DBActionType Type { get; set; }
        public string ObjectName { get; set; }
        public ActionProps Props { get; set; }

    }
    public abstract class ActionProps 
    {
        
    }
    public class ActionFieldProps : ActionProps 
    {
        public int? FieldInitialSize { get; set; }
        public int? FieldTargetSize { get; set; }
        public string FieldInitialType { get; set; }
        public string FieldTargetType { get; set; }
        public string TableName { get; set; }
        public bool WasPrimaryKey { get; set; }
        public bool IsPrimaryKey { get; set; }
    }
    public class ActionRelationshipProps : ActionProps
    {
        public string TableName { get; set; }
        public string TableFieldName { get; set; }
        public string ReferencedTableName { get; set; }
        public string ReferencedTableFieldName { get; set; }
    }
}

