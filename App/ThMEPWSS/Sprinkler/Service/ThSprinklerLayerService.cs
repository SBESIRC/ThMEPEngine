using Linq2Acad;
using DotNetARX;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore;

namespace ThMEPWSS.Sprinkler.Service
{
    public static class ThSprinklerLayerService
    {
        public static ObjectId CreateAIBeamsCheckerLayer(this Database database)
        {
            return database.CreateAILayer("AI-喷头校核-较高的梁", 60);
        }
    }
}
