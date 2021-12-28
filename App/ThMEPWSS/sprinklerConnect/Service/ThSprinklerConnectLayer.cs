using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore;

namespace ThMEPWSS.SprinklerConnect.Service
{
    public static class ThSprinklerConnectLayer
    {
        public static ObjectId CreateAIMainPipeLayer(this Database database)
        {
            return database.CreateAILayer(ThWSSCommon.Sprinkler_Connect_MainPipe, 1);
        }

        public static ObjectId CreateAISubMainPipeLayer(this Database database)
        {
            return database.CreateAILayer(ThWSSCommon.Sprinkler_Connect_SubMainPipe, 4);
        }
    }
}
