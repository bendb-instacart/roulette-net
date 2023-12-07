using Roulette.Hashing;

namespace Roulette;

public class WeightedRendezvousVariantAssigner : IVariantAssigner
{
    private readonly WeightedRendezvous wr;

    public WeightedRendezvousVariantAssigner(IHashProvider hashProvider)
    {
        wr = new WeightedRendezvous(hashProvider);
    }

    public void AddVariant(VariantWeight variantWeight)
    {
        wr.Add(variantWeight.Name, (double) variantWeight.Weight);
    }

    public string Assign(string partitionValue)
    {
        return wr.Lookup(partitionValue);
    }
}
