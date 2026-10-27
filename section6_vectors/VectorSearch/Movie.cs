using Microsoft.Extensions.VectorData;

public class Movie
{
    [VectorStoreKey]
    public required int Key { get; set; }

    [VectorStoreData]
    public required string Title { get; set; }

    [VectorStoreData]
    public required string Description { get; set; }

    [VectorStoreVector(384, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }
}
