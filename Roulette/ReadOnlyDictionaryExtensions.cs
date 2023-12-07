namespace Roulette;

internal static class ReadOnlyDictionaryExtensions
{
    internal static object? Dig(this IReadOnlyDictionary<string, object> input, string path)
    {
        return input.Dig(Input.PathToSegments(path));
    }

    internal static object? Dig(this IReadOnlyDictionary<string, object> input, IReadOnlyList<string> pathSegments)
    {
        object? result = input;

        foreach (var segment in pathSegments)
        {
            if (result == null)
            {
                return null;
            }

            if (result is IReadOnlyDictionary<string, object> dict)
            {
                result = null;
                dict.TryGetValue(segment, out result);
                continue;
            }

            // If we're here, then there's a value but it's either scalar (and we expect more data),
            // or it is a collection type we don't understand.  Fail.
            return null;
        }

        return result;
    }
}
