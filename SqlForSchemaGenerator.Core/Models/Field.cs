using System.Runtime.Serialization;

namespace SqlForSchemaGenerator.Core.Models;

[DataContract]
public class Field
{
    [DataMember]
    public string? Name { get; set; }
    [DataMember]
    public SystemTypesEnum Type { get; set; }
    [DataMember]
    public int? Size{ get; set; }
    [DataMember]
    public bool IsPrimaryKey { get; set; }

}
