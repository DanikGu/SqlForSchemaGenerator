using System.Runtime.Serialization;

namespace SqlForSchemaGenerator.Core.Models;

[DataContract]
public class Table
{
    [DataMember]
    public string Name { get; set; }
    [DataMember]
    public Field[] Fields { get; set; } = new Field[0];
    [DataMember]
    public Relationship[] Relationships { get; set; } = new Relationship[0];
}
