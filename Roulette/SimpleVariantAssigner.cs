using Roulette.Hashing;

namespace Roulette;

public class SimpleVariantAssigner : IVariantAssigner
{
    private readonly IHashProvider hp;

    public SimpleVariantAssigner(IHashProvider hashProvider)
    {
        this.hp = hashProvider;
    }

    public void AddVariant(VariantWeight variantWeight)
    {
        throw new NotImplementedException();
    }

    public string Assign(string partitionValue)
    {
        throw new NotImplementedException();
    }
}