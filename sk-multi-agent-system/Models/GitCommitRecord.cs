using Microsoft.Extensions.VectorData;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace sk_multi_agent_system.Models;

public class GitCommitRecord
{
    [VectorStoreKey]
    [BsonId]
    public string CommitSha { get; set; } = string.Empty;

    [BsonElement("commit_message")]
    [VectorStoreData]
    public string Message { get; set; } = string.Empty;

    [BsonElement("author_name")]
    [VectorStoreData(IsIndexed = true)]
    public string Author { get; set; } = string.Empty;

    [BsonElement("commit_date")]
    [VectorStoreData(IsIndexed = true)]
    public DateTime Date { get; set; }

    [BsonElement("commit_description")]
    [VectorStoreData(IsIndexed = true)]
    public string Description { get; set; } = string.Empty ;

    [BsonElement("commit_embedding")]
    [VectorStoreVector(3072, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float>? Embedding { get; set; }
}
