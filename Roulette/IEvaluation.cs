namespace Roulette;

public interface IEvaluation
{
    string FeatureName { get; }
    ulong FeatureVersion { get; }
    string PartitionValue { get; set; }
    IReadOnlyDictionary<string, object> Input { get; }
    MatchReason Reason { get; }
    string Variant { get; }
    string? MatchedRuleset { get; }

    bool IsMatched => Reason switch
    {
        MatchReason.Ruleset or MatchReason.StaticAssignent => true,
        _ => false
    };

    void Match(MatchReason reason, string variant, string? matchedRuleset = null);
    void Unmatch(MatchReason reason);
}
