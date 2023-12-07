namespace Roulette.Hashing;

public enum HashPrimitive
{
    XxHash,
    Fnv1a
}

public static class HashPrimitiveExtensions
{
    public static IHashProvider GetHashProvider(this HashPrimitive primitive)
    {
        switch (primitive)
        {
            case HashPrimitive.XxHash: return XxHashProvider.Instance;
            case HashPrimitive.Fnv1a: return Fnv1aHashProvider.Instance;
            default:
                throw new ArgumentException($"Unsupported hash primitive value '{primitive}'", nameof(primitive));
        }
    }

    public static HashPrimitive ToHashPrimitive(this Roulette.Protos.HashSpec.Types.HashPrimitive proto)
    {
        switch (proto)
        {
            case Roulette.Protos.HashSpec.Types.HashPrimitive.Xxhash: return HashPrimitive.XxHash;
            default:
                // we don't have any other prod primitive types, just default to xxhash
                return HashPrimitive.XxHash;
        }
    }
}
