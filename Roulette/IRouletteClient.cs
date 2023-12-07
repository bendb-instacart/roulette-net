namespace Roulette;

public interface IRouletteClient : IDisposable
{
    void Start();

    IFeature? GetFeatureByName(string name);
    IFeature? GetFeatureByGuid(Guid guid);

    IFeature? this[string name]
    {
        get { return GetFeatureByName(name); }
    }

    IFeature? this[Guid guid]
    {
        get { return GetFeatureByGuid(guid); }
    }
}
