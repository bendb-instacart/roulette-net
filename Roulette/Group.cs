namespace Roulette;

public class Group
{
    public string Name { get; }
    public ulong Version { get; }
    public IReadOnlySet<string> Values { get; }

    public static Group FromProto(Protos.Group proto)
    {
        return new Group(proto.Id, proto.Version, proto.Values.ToHashSet());
    }

    public Group(string name, ulong version, IReadOnlySet<string> values)
    {
        this.Name = name;
        this.Version = version;
        this.Values = values;
    }
}