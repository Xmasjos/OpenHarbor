using OpenHarbor.Models;

namespace OpenHarbor.Services;

public interface IPluginRecordNormalizer
{
    void Normalize(PluginRecord record);
}