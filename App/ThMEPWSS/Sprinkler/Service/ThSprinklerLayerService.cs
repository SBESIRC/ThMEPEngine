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

        public static ObjectId CreateAISprinklerDistanceCheckerLayer(this Database database)
        {
            return database.CreateAILayer("AI-喷头校核-喷头间距是否过小", 6);
        }

        public static ObjectId CreateAISprinklerDistanceFormBoundaryCheckerLayer(this Database database)
        {
            return database.CreateAILayer("AI-喷头校核-喷头距边是否过小", 30);
        }

        public static ObjectId CreateAISprinklerLayoutAreaLayer(this Database database)
        {
            return database.CreateAILayer("AI-喷头校核-可布置区域", 3);
        }

        public static ObjectId CreateAISprinklerDistanceFormBeamCheckerLayer(this Database database)
        {
            return database.CreateAILayer("AI-喷头校核-喷头距梁是否过小", 4);
        }
    }
}
