namespace Roulette.Hashing;

class Fnv1aHashProvider : IHashProvider
{
    public static readonly IHashProvider Instance = new Fnv1aHashProvider();

    public ulong Hash64(ReadOnlySpan<byte> data)
    {
        return HashDepot.Fnv1a.Hash64(data);
    }
}

class XxHashProvider : IHashProvider
{
    public static readonly IHashProvider Instance = new XxHashProvider();

    public ulong Hash64(ReadOnlySpan<byte> data)
    {
        return HashDepot.XXHash.Hash64(data, 0);
    }
}
