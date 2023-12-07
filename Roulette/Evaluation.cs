namespace Roulette;

internal class Evaluation : IEvaluation
{
    private readonly Feature feature;

    public string FeatureName => feature.Name;

    public ulong FeatureVersion => feature.Version;

    public IReadOnlyDictionary<string, object> Input { get; }

    public string PartitionValue { get; set; } = "";

    public MatchReason Reason { get; private set; } = MatchReason.None;

    public string Variant { get; private set; }

    public string? MatchedRuleset { get; private set; }

    internal Evaluation(Feature feature, IReadOnlyDictionary<string, object> input)
    {
        this.feature = feature;
        this.Input = input;
        this.Variant = feature.DefaultVariant.Name;
        this.MatchedRuleset = null;
    }

    public void Match(MatchReason reason, string variant, string? matchedRuleset = null)
    {
        Reason = reason;
        Variant = variant;
        MatchedRuleset = matchedRuleset;
    }

    public void Unmatch(MatchReason reason)
    {
        Reason = reason;
        Variant = feature.DefaultVariant.Name;
        MatchedRuleset = null;
    }
}
