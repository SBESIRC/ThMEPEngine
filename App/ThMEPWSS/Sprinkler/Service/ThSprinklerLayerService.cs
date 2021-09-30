using Linq2Acad;
using DotNetARX;
using ThMEPEngineCore;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Service
{
    public static class ThSprinklerLayerService
    {
        /// <summary>
        /// 较高的梁
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAIBeamCheckerLayer(this Database database)
        {
            return database.CreateAILayer(ThSprinklerCheckerLayer.Beam_Checker_LayerName, 1);
        }

        /// <summary>
        /// 喷头间距是否过小
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAISprinklerDistanceCheckerLayer(this Database database)
        {
            return database.CreateAILayer(ThSprinklerCheckerLayer.Sprinkler_Distance_LayerName, 4);
        }

        /// <summary>
        /// 喷头距边是否过小
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAISprinklerDistanceFormBoundarySoCloseCheckerLayer(this Database database)
        {
            return database.CreateAILayer(ThSprinklerCheckerLayer.From_Boundary_So_Close_LayerName, 4);
        }

        /// <summary>
        /// 可布置区域
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAISprinklerLayoutAreaLayer(this Database database)
        {
            return database.CreateAILayer(ThSprinklerCheckerLayer.Layout_Area_LayerName, 3);
        }

        /// <summary>
        /// 喷头距梁是否过小
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAISprinklerDistanceFormBeamCheckerLayer(this Database database)
        {
            return database.CreateAILayer(ThSprinklerCheckerLayer.Distance_Form_Beam_LayerName, 4);
        }

        /// <summary>
        /// 盲区检测
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAISprinklerBlindZoneCheckerLayer(this Database database)
        {
            return database.CreateAILayer(ThSprinklerCheckerLayer.Blind_Zone_LayerName, 1);
        }

        /// <summary>
        /// 喷头距边是否过大
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAISprinklerDistanceFormBoundarySoFarCheckerLayer(this Database database)
        {
            return database.CreateAILayer(ThSprinklerCheckerLayer.From_Boundary_So_Far_LayerName, 6);
        }
    }
}
