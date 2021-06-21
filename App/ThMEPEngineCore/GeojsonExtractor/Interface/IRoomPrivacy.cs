using ThMEPEngineCore.Model;
using ThMEPEngineCore.GeojsonExtractor;

namespace ThMEPEngineCore.GeojsonExtractor.Interface
{
    public interface IRoomPrivacy
    {
        Privacy Judge(ThIfcRoom room);
    }
}
