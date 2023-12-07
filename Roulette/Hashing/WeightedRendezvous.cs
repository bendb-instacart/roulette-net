namespace Roulette.Hashing;

public class WeightedRendezvous
{
    private readonly List<Node> nodes = new();
    private readonly IHashProvider hashProvider;

    public IReadOnlyList<INode> Nodes => nodes;
    public IEnumerable<string> NodeNames => nodes.Select(n => n.Name);

    public WeightedRendezvous(IHashProvider hashProvider)
    {
        this.hashProvider = hashProvider;
    }

    public void Add(string name, double weight = 1.0F)
    {
        int ix = nodes.BinarySearch(new Node { Name = name });
        if (ix >= 0)
        {
            nodes[ix].Weight = weight;
        }
        else
        {
            nodes.Insert(~ix, new Node
            {
                Name = name,
                Hash = hashProvider.Hash64(name),
                Weight = weight
            });
        }
    }

    public void Remove(string name)
    {
        int ix = nodes.BinarySearch(new Node { Name = name });
        if (ix >= 0)
        {
            nodes.RemoveAt(ix);
        }
    }

    public string Lookup(string key)
    {
        var keyHash = hashProvider.Hash64(key);
        var maxScore = double.MinValue;

        Node? assignedNode = null;
        foreach (var node in nodes)
        {
            var score = ComputeScore(keyHash, node.Hash, node.Weight);
            if (score > maxScore)
            {
                maxScore = score;
                assignedNode = node;
            }
        }

        if (assignedNode == null)
        {
            throw new InvalidOperationException("wtf");
        }

        return assignedNode.Name;
    }

    private static double ComputeScore(ulong keyHash, ulong nodeHash, double weight)
    {
        var h = CombineHashes(keyHash, nodeHash);
        var doubleH = (double) h;
        var doubleMax = ((double) ulong.MaxValue);
        var quotient = doubleH / doubleMax;
        var log = Math.Log(quotient);
        var score = -weight / log;
        return score;
    }

    private static ulong CombineHashes(ulong a, ulong b)
    {
        // uses the "xorshift*" mix function which is simple and effective
	    // see: https://en.wikipedia.org/wiki/Xorshift#xorshift*
        unchecked
        {
            ulong x = a ^ b;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            return x * 0x2545F4914F6CDD1D;
        }
    }

    public interface INode
    {
        string Name { get; }
        ulong Hash { get; }
        double Weight { get; }
    }

    private class Node : IComparable<Node>, INode
    {
        public string Name { get; init; } = "";
        public ulong Hash { get; init; }
        public double Weight { get; set; }

        public int CompareTo(Node? other)
        {
            return Name.CompareTo(other!.Name);
        }
    }
}
