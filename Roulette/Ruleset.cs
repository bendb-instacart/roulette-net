namespace Roulette;

public class Ruleset
{
    public string Name { get; }
    public bool IsEnabled { get; }
    public double ExposurePercentage { get; }
    public IReadOnlyList<Rule> Rules { get; }
    public IVariantAssigner VariantAssigner { get; }


    public static Ruleset? FromProto(Protos.Ruleset proto, IVariantAssignerFactory variantAssignerFactory, IReadOnlyDictionary<string, Group> groupsByName)
    {
        List<Rule> rules = new(proto.Rules.Count);
        foreach (var ruleProto in proto.Rules)
        {
            var rule = Rule.FromProto(ruleProto, groupsByName);
            if (rule == null)
            {
                return null;
            }
            rules.Add(rule);
        }

        var assigner = variantAssignerFactory.Create();
        foreach (var vwProto in proto.VariantWeights)
        {
            var vw = VariantWeight.FromProto(vwProto);
            assigner.AddVariant(vw);
        }

        return new Ruleset(
            name: proto.Name,
            enabled: proto.Enabled,
            exposurePercentage: proto.ExposurePercentage,
            rules: rules.AsReadOnly(),
            variantAssigner: assigner
        );
    }

    public Ruleset(string name, bool enabled, float exposurePercentage, IReadOnlyList<Rule> rules, IVariantAssigner variantAssigner)
    {
        this.Name = name;
        this.IsEnabled = enabled;
        this.ExposurePercentage = exposurePercentage;
        this.Rules = rules;
        this.VariantAssigner = variantAssigner;
    }

    public string? Evaluate(IReadOnlyDictionary<string, object> input, string saltedPartitionValue, ulong partitionValueHash)
    {
        if (!IsEnabled)
        {
            return null;
        }

        checked
        {
            double exposureCutoff = ExposurePercentage * ulong.MaxValue;
            if ((double) partitionValueHash > exposureCutoff)
            {
                return null;
            }
        }

        if (!Rules.All(r => r.IsSatisfiedBy(input)))
        {
            return null;
        }

        return VariantAssigner.Assign(saltedPartitionValue);
    }
}
