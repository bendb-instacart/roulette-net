namespace Roulette;

public interface IPrecondition
{
    Guid TargetFeatureGuid { get; }

    bool IsSatisfiedBy(IEvaluation evaluation);
}
