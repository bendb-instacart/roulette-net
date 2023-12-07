namespace Roulette;

public class Rule
{
    public IReadOnlyList<string> PathSegments { get; }
    public IReadOnlyList<ICriterion> Criteria { get; }

    public static Rule? FromProto(Protos.Ruleset.Types.Rule proto, IReadOnlyDictionary<string, Group> groupsByName)
    {
        List<ICriterion> criteria = new();
        foreach (var criterionProto in proto.Criteria)
        {
            ICriterion? criterion = Criterion.FromProto(criterionProto, groupsByName);
            if (criterion == null)
            {
                return null;
            }
            criteria.Add(criterion);
        }

        return new Rule(Input.PathToSegments(proto.Path), criteria.AsReadOnly());
    }

    public Rule(IReadOnlyList<string> pathSegments, IReadOnlyList<ICriterion> criteria)
    {
        PathSegments = pathSegments;
        Criteria = criteria;
    }

    public bool IsSatisfiedBy(IReadOnlyDictionary<string, object> input)
    {
        object? datum = input.Dig(PathSegments);

        return datum != null && Criteria.All(c => c.IsSatisfiedBy(datum));
    }
}
