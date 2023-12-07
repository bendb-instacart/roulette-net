namespace Roulette;

public record Variant(string Name, bool IsDefault)
{
    public static Variant FromProto(Roulette.Protos.Feature.Types.Variant proto)
    {
        return new Variant(proto.Name, proto.IsDefault);
    }
}
