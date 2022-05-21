using System;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.Model;
using ThMEPTCH.TCHDrawServices;

namespace ThMEPWSS.Service
{
    public static class InsertSprinklerService
    {
        /// <summary>
        /// 插入喷淋图块
        /// </summary>
        /// <param name="insertPts"></param>
        public static void Insert(List<Point3d> positions)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                string name = BlockName();
                acadDatabase.Database.ImportBlock(name, ThWSSCommon.SprayLayerName);
                positions.ForEach(o =>
                {
                    acadDatabase.Database.InsertBlock(ThWSSCommon.SprayLayerName, name, o, new Scale3d(1), 0);
                });
            }
        }
        /// <summary>
        /// 插入天正喷淋（在事务外调用）
        /// </summary>
        /// <param name="layoutPts"></param>
        /// <param name="hidePipe"></param>
        public static void InsertTCHSprinkler(List<Point3d> layoutPts,int hidePipe) 
        {
            if (layoutPts ==null || layoutPts.Count < 1)
                return;
            var tchSprinklers = new List<ThTCHSprinkler>();
            foreach (var pt in layoutPts)
            {
                var newTCHModel = new ThTCHSprinkler();
                newTCHModel.Location = pt;
                newTCHModel.Angle = 0;
                newTCHModel.Type = 1;
                newTCHModel.LinkMode = 1;
                newTCHModel.Radius = 2.12;
                newTCHModel.SizeX = 300.0;
                newTCHModel.SizeY = 0;
                newTCHModel.DocScale = 100.0;
                newTCHModel.HidePipe = hidePipe;
                newTCHModel.MirrorByX = 0;
                newTCHModel.MirrorByY = 0;
                tchSprinklers.Add(newTCHModel);
            }
            var tchDrawSprinkler = new TCHDrawSprinklerService();
            tchDrawSprinkler.InitData(tchSprinklers);
            tchDrawSprinkler.DrawExecute();
        }
        /// <summary>
        /// 喷淋图块名
        /// </summary>
        /// <returns></returns>
        private static string BlockName()
        {
            switch(ThWSSUIService.Instance.Parameter.layoutType)
            {
                case ThMEPWSS.Model.LayoutType.UpSpray:
                    return ThWSSCommon.SprayUpBlockName;
                case ThMEPWSS.Model.LayoutType.DownSpray:
                    return ThWSSCommon.SprayDownBlockName;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 插入图块
        /// </summary>
        /// <param name="database"></param>
        /// <param name="layer"></param>
        /// <param name="name"></param>
        /// <param name="position"></param>
        /// <param name="scale"></param>
        /// <param name="angle"></param>
        private static void InsertBlock(this Database database, string layer, string name, Point3d position, Scale3d scale, double angle)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference(layer, name, position, scale, angle);
            }
        }

        /// <summary>
        /// 导入图块
        /// </summary>
        /// <param name="database"></param>
        /// <param name="name"></param>
        /// <param name="layer"></param>
        private static void ImportBlock(this Database database, string name, string layer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.SprinklerDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(name), false);
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(layer), false);
            }
        }
    }
}
