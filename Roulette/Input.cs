namespace Roulette;

public static class Input
{
    public static IReadOnlyList<string> PathToSegments(string path)
    {
        return path.Split('.').SkipWhile(it => it == "$").ToList();
    }

    public static string? CoerceToString(object input) => input switch
    {
        string str => str,
        byte or sbyte or short or int or long or ushort or uint or ulong => Convert.ToString(input),
        _ => null
    };

    public static long? CoerceToLong(object input) => input switch
    {
        byte b => (long) b,
        sbyte b => (long) b,
        short s => (long) s,
        ushort s => (long) s,
        int i => (long) i,
        uint i => (long) i,
        long l => l,
        ulong l and (< long.MaxValue) => (long) l,
        _ => null
    };
}
