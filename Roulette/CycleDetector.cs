namespace Roulette;

internal class CycleDetector
{
    private enum State
    {
        Unvisited,
        Visiting,
        Visited
    }

    private readonly Dictionary<Guid, State> visitingStateByFeatureGuid = new();
    private readonly List<string> path = new();
    private readonly IRouletteClient client;

    internal string CyclePath => string.Join(" -> ", path);

    internal CycleDetector(IRouletteClient client)
    {
        this.client = client;
    }

    internal bool AreCyclesPresent(IFeature feature)
    {
        StartVisiting(feature);

        foreach (var pc in feature.Preconditions)
        {
            var target = client.GetFeatureByGuid(pc.TargetFeatureGuid);
            if (target == null)
            {
                throw new InvalidOperationException("No such feature");
            }

            switch (StateFor(target))
            {
                case State.Visiting:
                    path.Add(target.Name);
                    return true;

                case State.Visited:
                    // Already seen this one - skip ahead
                    continue;

                default:
                    break;
            }

            if (AreCyclesPresent(target))
            {
                return true;
            }
        }

        StopVisiting(feature);
        return false;
    }

    private State StateFor(IFeature feature)
    {
        return visitingStateByFeatureGuid.TryGetValue(feature.Guid, out var state)
            ? state
            : State.Unvisited;
    }

    private void StartVisiting(IFeature feature)
    {
        visitingStateByFeatureGuid[feature.Guid] = State.Visiting;
        path.Add(feature.Name);
    }

    private void StopVisiting(IFeature feature)
    {
        if (path.Count > 0)
        {
            path.RemoveAt(path.Count - 1);
        }
        visitingStateByFeatureGuid[feature.Guid] = State.Visited;
    }
}
