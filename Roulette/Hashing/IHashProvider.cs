namespace Roulette.Hashing;

public interface IHashProvider
{
    ulong Hash64(ReadOnlySpan<byte> bytes);

    ulong Hash64(string text)
    {
        return Hash64(System.Text.Encoding.UTF8.GetBytes(text));
    }
}
