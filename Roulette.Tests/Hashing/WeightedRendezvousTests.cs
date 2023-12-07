using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Roulette.Hashing;

public class WeightedRendezvousTests
{
    private readonly ITestOutputHelper output;

    public WeightedRendezvousTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void RemoveKeepsNodesSorted()
    {
        WeightedRendezvous wr = new(XxHashProvider.Instance);
        wr.Add("a");
        wr.Add("b");
        wr.Add("c");

        wr.Remove("b");

        wr.NodeNames.Should().BeEquivalentTo(new[] { "a", "c" });

        wr.Remove("d");

        wr.NodeNames.Should().BeEquivalentTo(new[] { "a", "c" });
    }

    [Fact]
    public void AddKeepsNodesSorted()
    {
        WeightedRendezvous wr = new(XxHashProvider.Instance);
        wr.Add("d");
        wr.Add("c");
        wr.Add("b");
        wr.Add("e");
        wr.Add("a");

        wr.NodeNames.Should().BeEquivalentTo(new[] { "a", "b", "c", "d", "e" });
    }

    [Fact]
    public void AddDoesNotResultInDuplicates()
    {
        WeightedRendezvous wr = new(XxHashProvider.Instance);
        wr.Add("d");
        wr.Add("c");
        wr.Add("b");
        wr.Add("e");
        wr.Add("a");

        wr.Add("a");
        wr.Add("a");

        wr.NodeNames.Should().BeEquivalentTo(new[] { "a", "b", "c", "d", "e" });
    }

    [Fact]
    public void AddUpdatesWeights()
    {
        WeightedRendezvous wr = new(XxHashProvider.Instance);
        wr.Add("a", 2.0);
        wr.Nodes.Select(n => n.Weight).Should().BeEquivalentTo(new[] { 2.0 });

        wr.Add("a", 4.0);
        wr.Nodes.Select(n => n.Weight).Should().BeEquivalentTo(new[] { 4.0 });
    }

    [Fact]
    public void LookupIsBasicallyAccurate()
    {
        WeightedRendezvous wr = new(XxHashProvider.Instance);
        wr.Add("x", 1.0);
        wr.Add("y", 0.5);
        wr.Add("z", 0.5);

        Dictionary<string, int> allocs = new()
        {
            { "x", 0 },
            { "y", 0 },
            { "z", 0 }
        };

        for (int i = 0; i < 10_000; i++)
        {
            var node = $"n{i}";
            allocs[wr.Lookup(node)]++;
        }

        var pctX = allocs["x"] / 10_000.0;
        pctX.Should().BeApproximately(0.5, precision: 0.01);
    }

    [Fact]
    public void LookupIsConsistent()
    {
        // This test asserts that, for some keys K mapped to known
        // nodes N (subset of N+), when we remove nodes _not_ in N, then all keys K
        // remain mapped to N.
        //
        // In other words, keys mapped to nodes should remain mapped to those nodes
        // when unrelated nodes are removed.
        WeightedRendezvous wr = new(Fnv1aHashProvider.Instance);

        // N+ = { "n" + d | 0 <= d <= 10000 }
        for (int i = 0; i <= 10_000; i++)
        {
            wr.Add($"n{i}");
        }

        // Here we build our mapping of K -> N
        Dictionary<string, string> mappings = new();
        for (int i = 0; i < 10_000; i += 29)
        {
            var key = $"k{i}";
            mappings[key] = wr.Lookup(key);
        }

        HashSet<string> mappedNodes = mappings.Values.ToHashSet();

        // Here, we remove some set of nodes N-, taking care
        // not to remove anything in N.
        for (int i = 0; i < 10_000; i += 33)
        {
            var node = $"n{i}";
            if (!mappedNodes.Contains(node))
            {
                wr.Remove(node);
            }
        }

        // Now we re-examine our mapping K->N, checking that for each k in K
        // that it remains mapped to its corresponding n in N.
        int numFailed = mappings.Count(kvp => wr.Lookup(kvp.Key) != kvp.Value);

        numFailed.Should().Be(0, "{0}% failed", numFailed / ((double) mappings.Count));
    }
}
