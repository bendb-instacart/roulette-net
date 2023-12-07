namespace Roulette;

using System.Collections.Generic;

public interface IFeature
{
    string Name { get; }
    Guid Guid { get; }
    IReadOnlyList<IPrecondition> Preconditions { get; }

    IEvaluation Evaluate(IReadOnlyDictionary<string, object> input);
}
