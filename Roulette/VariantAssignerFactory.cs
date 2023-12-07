using Roulette.Hashing;

namespace Roulette;

public class VariantAssignerFactory : IVariantAssignerFactory
{
    private readonly IHashProvider hashProvider;
    private readonly Protos.HashSpec.MethodOneofCase oneofCase;

    public VariantAssignerFactory(IHashProvider hashProvider, Protos.HashSpec.MethodOneofCase oneofCase)
    {
        this.hashProvider = hashProvider;
        this.oneofCase = oneofCase;
    }
    
    public IVariantAssigner Create()
    {
        switch (oneofCase)
        {
            case Protos.HashSpec.MethodOneofCase.Simple: return new SimpleVariantAssigner(hashProvider);
            case Protos.HashSpec.MethodOneofCase.WeightedRendezvous: return new WeightedRendezvousVariantAssigner(hashProvider);
            default:
                throw new ArgumentException($"Unsupported HashSpec method '{oneofCase}'");
        }
    }
}
