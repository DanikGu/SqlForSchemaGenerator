using System.Runtime.Serialization;

namespace SqlForSchemaGenerator.Core.Models;

[DataContract]
public class DbStructure
{
    [DataMember]
    public Table[] Tables { get; set; }

    public List<Relationship> GetAllRelationships() 
    {
        return Tables.SelectMany(x => x.Relationships).ToList();
    }
}
