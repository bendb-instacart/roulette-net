namespace Roulette;

public interface IVariantAssigner
{
    void AddVariant(VariantWeight variantWeight);
    string Assign(string partitionValue);
}

public interface IVariantAssignerFactory
{
    IVariantAssigner Create();
}
