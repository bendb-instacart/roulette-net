namespace Roulette;

public record VariantWeight(String Name, int Weight)
{
    public static VariantWeight FromProto(Roulette.Protos.Ruleset.Types.VariantWeight proto)
    {
        return new VariantWeight(proto.Variant, proto.Weight);
    }
}
