using System.Text;
using Roulette.Hashing;

namespace Roulette;

public class Feature : IFeature
{
    private readonly IRouletteClient client;
    private readonly IReadOnlyList<string> partitionKeySegments;
    private readonly IHashProvider hashProvider;

    public Guid Guid { get; }
    public string Name { get; }
    public string Domain { get; }
    public ulong Version { get; }
    public ulong Salt { get; }
    public string PartitionKey { get; }
    public bool IsEnabled { get; }
    public bool TrackAllExposures { get; }
    public IReadOnlyList<Variant> Variants { get; }
    public IReadOnlyDictionary<string, Variant> StaticAssignments { get; }
    public IReadOnlyList<Ruleset> Rulesets { get; }
    public IReadOnlyList<IPrecondition> Preconditions { get; }

    public Variant DefaultVariant { get; }

    public static Feature? FromProto(Roulette.Protos.Feature proto, IRouletteClient client, IReadOnlyDictionary<string, Group> groupsByName)
    {
        Guid guid;
        if (!Guid.TryParse(proto.Uuid, out guid))
        {
            return null;
        }

        var variants = proto.Variants.Select(Variant.FromProto).ToList().AsReadOnly();
        var variantsByName = variants.ToDictionary(variant => variant.Name);
        var assignments = proto.Assignments.ToDictionary(
            a => a.RequestId,
            a => variantsByName[a.VariantName]);

        var hashProvider = HashProviderFromProto(proto.HashSpec);
        var variantAssignerFactory = new VariantAssignerFactory(hashProvider, proto.HashSpec.MethodCase);

        List<Ruleset> rulesets = new(proto.Rulesets.Count);
        foreach (var rulesetProto in proto.Rulesets)
        {
            var ruleset = Ruleset.FromProto(rulesetProto, variantAssignerFactory, groupsByName);
            if (ruleset == null)
            {
                return null;
            }
            rulesets.Add(ruleset);
        }

        List<IPrecondition> preconditions = new(proto.Preconditions.Count);
        foreach (var pcProto in proto.Preconditions)
        {
            var pc = Precondition.FromProto(pcProto);
            if (pc == null)
            {
                return null;
            }
            preconditions.Add(pc);
        }

        return new Feature(
            client,
            guid,
            proto.Name,
            proto.Domain,
            proto.Version,
            proto.Salt,
            proto.PartitionKind.IdKind.Selector,
            proto.Enabled,
            proto.TrackAllExposures,
            variants,
            assignments,
            rulesets.AsReadOnly(),
            preconditions.AsReadOnly(),
            hashProvider
        );
    }

    private static IHashProvider HashProviderFromProto(Protos.HashSpec proto)
    {
        switch (proto.MethodCase)
        {
            case Protos.HashSpec.MethodOneofCase.Simple:
                return proto.Simple.HashPrimitive.ToHashPrimitive().GetHashProvider();

            case Protos.HashSpec.MethodOneofCase.WeightedRendezvous:
                return proto.WeightedRendezvous.HashPrimitive.ToHashPrimitive().GetHashProvider();

            default:
                throw new ArgumentException($"Unrecognized hashspec method '{proto.MethodCase}'");
        }
    }

    public Feature(
        IRouletteClient client,
        Guid guid,
        string name,
        string domain,
        ulong version,
        ulong salt,
        string partitionKey,
        bool enabled,
        bool TrackAllExposures,
        IReadOnlyList<Variant> variants,
        IReadOnlyDictionary<string, Variant> staticAssignments,
        IReadOnlyList<Ruleset> rulesets,
        IReadOnlyList<IPrecondition> preconditions,
        IHashProvider hashProvider
    )
    {
        this.client = client;
        this.Guid = guid;
        this.Name = name;
        this.Domain = domain;
        this.Version = version;
        this.Salt = salt;
        this.PartitionKey = partitionKey;
        this.IsEnabled = enabled;
        this.TrackAllExposures = TrackAllExposures;
        this.Variants = variants;
        this.StaticAssignments = staticAssignments;
        this.Rulesets = rulesets;
        this.Preconditions = preconditions;
        
        this.DefaultVariant = variants.Single(v => v.IsDefault);
        this.partitionKeySegments = Input.PathToSegments(partitionKey);
        this.hashProvider = hashProvider;
    }

    public IEvaluation Evaluate(IReadOnlyDictionary<string, object> input)
    {
        Dictionary<Guid, IEvaluation> evaluationsByFeatureGuid = new();
        return EvaluateFeatureAndPreconditions(input, evaluationsByFeatureGuid);
    }

    private IEvaluation EvaluateFeatureAndPreconditions(IReadOnlyDictionary<string, object> input, Dictionary<Guid, IEvaluation> evaluationsByFeatureGuid)
    {
        if (evaluationsByFeatureGuid.TryGetValue(this.Guid, out IEvaluation? cachedEval))
        {
            return cachedEval;
        }

        IEvaluation eval = EvaluateWithoutPreconditions(input);
        evaluationsByFeatureGuid[this.Guid] = eval;

        if (!eval.IsMatched || eval.Reason == MatchReason.StaticAssignent)
        {
            return eval;
        }

        bool failedPrecondition = false;
        foreach (var pc in Preconditions)
        {
            var target = client.GetFeatureByGuid(pc.TargetFeatureGuid) as Feature;
            if (target == null)
            {
                // TODO: report error
                failedPrecondition = true;
                eval.Unmatch(MatchReason.PreconditionFailed);
                continue;
            }

            var targetEval = target.EvaluateFeatureAndPreconditions(input, evaluationsByFeatureGuid);

            if (!pc.IsSatisfiedBy(targetEval))
            {
                failedPrecondition = true;
            }
        }

        if (failedPrecondition)
        {
            MatchReason newReason = eval.IsMatched
                ? MatchReason.PreconditionFailedWithMatch
                : MatchReason.PreconditionFailed;

            eval.Unmatch(newReason);
        }

        return eval;
    }

    private IEvaluation EvaluateWithoutPreconditions(IReadOnlyDictionary<string, object> input)
    {
        Evaluation eval = new(this, input);

        if (!IsEnabled)
        {
            return eval;
        }

        object? partitionValueDatum = input.Dig(partitionKeySegments);
        if (partitionValueDatum == null)
        {
            // TODO: report error
            return eval;
        }

        string? partitionValue = Input.CoerceToString(partitionValueDatum);
        if (partitionValue == null)
        {
            // TODO: report error
            return eval;
        }

        eval.PartitionValue = partitionValue;

        if (StaticAssignments.TryGetValue(partitionValue, out Variant? variant))
        {
            eval.Match(MatchReason.StaticAssignent, variant!.Name);
            return eval;
        }

        string saltedPartitionValue = $"{Salt}{partitionValue}";
        ulong partitionValueHash = hashProvider.Hash64(saltedPartitionValue);

        foreach (var ruleset in Rulesets)
        {
            string? assignedVariant = ruleset.Evaluate(input, saltedPartitionValue, partitionValueHash);
            if (assignedVariant != null)
            {
                eval.Match(MatchReason.Ruleset, assignedVariant, ruleset.Name);
                break;
            }
        }

        return eval;
    }
}