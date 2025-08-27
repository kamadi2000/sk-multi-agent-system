using Microsoft.Extensions.VectorData;

public class TriageAgentModel
{
    [VectorStoreKey]
    public Guid Key { get; set; } = Guid.NewGuid();

    [VectorStoreData]
    public string UserID { get; set; } = null!;

    [VectorStoreData]
    public string ChatID { get; set; } = null!;

    [VectorStoreData]
    public string Bug_Description { get; set; } = null!;

    [VectorStoreVector(3072, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }
}

