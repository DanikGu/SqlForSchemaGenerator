using System.Runtime.Serialization;

namespace SqlForSchemaGenerator.Core.Models;

[DataContract]
public class Relationship
{
    public Table Table { get; set; }
    public Field Field { get; set; }
    public Table ReferencedTable { get; set; }
    public Field ReferencedField { get; set; }

    [DataMember]
    public string FieldName
    {
        get => Field.Name; set { }
    }
    [DataMember]
    public string ReferncedTableName
    {
        get => ReferencedTable.Name; set { }
    }
    [DataMember]
    public string ReferencedFieldName
    {
        get => ReferencedField.Name; set { }
    }
    [DataMember]
    public string ConstraintName { get; set; }
}
