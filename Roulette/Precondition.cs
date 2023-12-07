namespace Roulette;

public abstract record Precondition(Guid TargetFeatureGuid) : IPrecondition
{
    public virtual bool IsSatisfiedBy(IEvaluation evaluation)
    {
        throw new NotImplementedException();
    }

    internal static Precondition? FromProto(Roulette.Protos.Feature.Types.Precondition proto)
    {
        Guid targetFeatureGuid;
        if (!Guid.TryParse(proto.FeatureUuid, out targetFeatureGuid))
        {
            return null;
        }

        switch (proto.PreconditionOneofCase)
        {
            case Protos.Feature.Types.Precondition.PreconditionOneofOneofCase.None:
                return null;

            case Protos.Feature.Types.Precondition.PreconditionOneofOneofCase.ShouldBeAssigned:
                return new AssignmentPrecondition(targetFeatureGuid, proto.ShouldBeAssigned);

            case Protos.Feature.Types.Precondition.PreconditionOneofOneofCase.AllowedVariants:
                return new VariantPrecondition(
                    targetFeatureGuid,
                    proto.AllowedVariants.VariantName.ToList().AsReadOnly(),
                    new List<string>()
                );

            case Protos.Feature.Types.Precondition.PreconditionOneofOneofCase.ExclusiveGroup:
                return new ExclusiveGroupPrecondition(
                    targetFeatureGuid,
                    proto.ExclusiveGroup.Percentage,
                    proto.ExclusiveGroup.AllowedVariants.VariantName.ToList().AsReadOnly()
                );

            default:
                return null;
        }
    }
}

public record AssignmentPrecondition(
    Guid TargetFeatureGuid,
    bool ShouldBeAssigned) : Precondition(TargetFeatureGuid);

public record VariantPrecondition(
    Guid TargetFeatureGuid,
    IReadOnlyList<string> AllowedVariants,
    IReadOnlyList<string> DisallowedVariants) : Precondition(TargetFeatureGuid);

public record ExclusiveGroupPrecondition(
    Guid TargetFeatureGuid,
    long Percentage,
    IReadOnlyList<string> allowedVariants) : Precondition(TargetFeatureGuid);
