﻿using Linq2Acad;
using DotNetARX;
using ThMEPEngineCore;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Service
{
    public static class ThSprinklerLayerService
    {
        /// <summary>
        /// 1.盲区检测
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAISprinklerBlindZoneCheckerLayer(this Database database)
        {
            return database.CreateAILayer(ThWSSCommon.Blind_Zone_LayerName, 1);
        }

        /// <summary>
        /// 2.喷头距边是否过大
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAISprinklerDistanceFormBoundarySoFarCheckerLayer(this Database database)
        {
            return database.CreateAILayer(ThWSSCommon.From_Boundary_So_Far_LayerName, 6);
        }

        /// <summary>
        /// 4.车位上方喷头
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAISprinklerParkingStallCheckerLayer(this Database database)
        {
            return database.CreateAILayer(ThWSSCommon.Parking_Stall_Checker_LayerName, 1);
        }

        /// <summary>
        /// 5.机械车位侧喷
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAISprinklerMechanicalParkingStallCheckerLayer(this Database database)
        {
            return database.CreateAILayer(ThWSSCommon.Mechanical_Parking_Stall_Checker_LayerName, 6);
        }

        /// <summary>
        /// 6.喷头间距是否过小
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAISprinklerDistanceCheckerLayer(this Database database)
        {
            return database.CreateAILayer(ThWSSCommon.Sprinkler_Distance_LayerName, 4);
        }

        /// <summary>
        /// 7.喷头距边是否过小
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAISprinklerDistanceFormBoundarySoCloseCheckerLayer(this Database database)
        {
            return database.CreateAILayer(ThWSSCommon.From_Boundary_So_Close_LayerName, 4);
        }

        /// <summary>
        /// 8.可布置区域
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAISprinklerLayoutAreaLayer(this Database database)
        {
            return database.CreateAILayer(ThWSSCommon.Layout_Area_LayerName, 3);
        }

        /// <summary>
        /// 8.喷头距梁是否过小
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAISprinklerDistanceFormBeamCheckerLayer(this Database database)
        {
            return database.CreateAILayer(ThWSSCommon.Distance_Form_Beam_LayerName, 4);
        }

        /// <summary>
        /// 9.较高的梁
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAIBeamCheckerLayer(this Database database)
        {
            return database.CreateAILayer(ThWSSCommon.Beam_Checker_LayerName, 1);
        }

        /// <summary>
        /// 10.喷头是否连管
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAIPipeCheckerLayer(this Database database)
        {
            return database.CreateAILayer(ThWSSCommon.Pipe_Checker_LayerName, 1);
        }

        /// <summary>
        /// 11.宽度大于1200的风管
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAIDuctCheckerLayer(this Database database)
        {
            return database.CreateAILayer(ThWSSCommon.Duct_Checker_LayerName, 40);
        }

        /// <summary>
        /// 12.区域喷头过密
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ObjectId CreateAISprinklerSoDenseLayer(this Database database)
        {
            return database.CreateAILayer(ThWSSCommon.Sprinkler_So_Dense_LayerName, 6);
        }
    }
}
