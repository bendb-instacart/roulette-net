using System.Linq;
using System.Text.RegularExpressions;

namespace Roulette;

internal static class Criterion
{
    internal static ICriterion? FromProto(Protos.Criterion criterionProto, IReadOnlyDictionary<string, Group> groupsByName)
    {
        switch (criterionProto.CriteriaCase)
        {
            case Protos.Criterion.CriteriaOneofCase.BoolMatch:
                return new BoolCriterion(criterionProto.BoolMatch.Expected);

            case Protos.Criterion.CriteriaOneofCase.NumericMatch:
                return new NumberCriterion(criterionProto.NumericMatch.Expected.ToHashSet());

            case Protos.Criterion.CriteriaOneofCase.StringMatch:
                var strings = criterionProto.StringMatch.Expected.ToList().AsReadOnly();
                List<Regex> expressions = new();
                foreach (var regex in criterionProto.StringMatch.Regex)
                {
                    try
                    {
                        expressions.Add(new Regex(regex, RegexOptions.ECMAScript));
                    }
                    catch (ArgumentException)
                    {
                        return null;
                    }
                }

                return new StringCriterion(
                    ExpectedStrings: strings,
                    ExpectedExpressions: expressions.AsReadOnly(),
                    Comparison: criterionProto.StringMatch.CaseSensitive
                        ? StringComparison.Ordinal
                        : StringComparison.OrdinalIgnoreCase
                );

            case Protos.Criterion.CriteriaOneofCase.NumberRange:
                return new NumberRangeCriterion(
                    criterionProto.NumberRange.MinInclusive,
                    criterionProto.NumberRange.MaxExclusive);
            
            case Protos.Criterion.CriteriaOneofCase.DayOfWeek:
                throw new NotImplementedException("DayOfWeek is unsupported");
            
            case Protos.Criterion.CriteriaOneofCase.Semver:
                return new SemverCriterion(criterionProto.Semver.Expression);

            case Protos.Criterion.CriteriaOneofCase.Group:
                if (groupsByName.TryGetValue(criterionProto.Group.GroupId, out var group))
                {
                    return new GroupCriterion(group);
                }
                else
                {
                    return null;
                }

            default:
                return null;
        }
    }
}

public record BoolCriterion(bool Expected) : ICriterion
{
    public bool IsSatisfiedBy(object input) => input is bool b && b == Expected;
}

public record NumberCriterion(IReadOnlySet<long> Expected) : ICriterion
{
    public bool IsSatisfiedBy(object input)
    {
        var n = Input.CoerceToLong(input);
        return n.HasValue && Expected.Contains(n.Value);
    }
}

public record StringCriterion(
    IReadOnlyList<string> ExpectedStrings,
    IReadOnlyList<Regex> ExpectedExpressions,
    StringComparison Comparison
) : ICriterion
{
    public bool IsSatisfiedBy(object input)
    {
        var s = Input.CoerceToString(input);
        if (s == null)
        {
            return false;
        }

        foreach (var expected in ExpectedStrings)
        {
            if (string.Equals(s, expected, Comparison))
            {
                return true;
            }
        }

        foreach (var expr in ExpectedExpressions)
        {
            if (expr.IsMatch(s))
            {
                return true;
            }
        }
        
        return false;
    }
}

public record NumberRangeCriterion(long MinInclusive, long MaxExclusive) : ICriterion
{
    public bool IsSatisfiedBy(object input)
    {
        var n = Input.CoerceToLong(input);
        return n.HasValue && n.Value >= MinInclusive && n.Value < MaxExclusive;
    }
}

public record SemverCriterion(string Expression) : ICriterion
{
    public bool IsSatisfiedBy(object input)
    {
        var s = Input.CoerceToLong(input);
        if (s == null)
        {
            return false;
        }

        throw new NotImplementedException("TODO");
    }
}

public record GroupCriterion(Group Group) : ICriterion
{
    public bool IsSatisfiedBy(object input)
    {
        var s = Input.CoerceToString(input);
        if (s == null)
        {
            return false;
        }

        return Group.Values.Contains(s);
    }
}
