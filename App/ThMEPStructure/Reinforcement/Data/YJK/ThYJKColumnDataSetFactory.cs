using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using ThMEPStructure.Reinforcement.Model;
using ThMEPStructure.Reinforcement.Service;

namespace ThMEPStructure.Reinforcement.Data.YJK
{
    /// <summary>
    /// 提取柱配筋信息
    /// </summary>
    public class ThYJKColumnDataSetFactory : ThMEPDataSetFactory
    {
        public List<ColumnExtractInfo> Results { get; set; }
        private ThShapeAnalysisService AnalysisService { get; set; }
        public ThYJKColumnDataSetFactory()
        {
            Results= new List<ColumnExtractInfo>();
            AnalysisService = new ThShapeAnalysisService();
        }
        protected override ThMEPDataSet BuildDataSet()
        {
            return new ThMEPDataSet()
            {
                Container = new List<ThGeometry>(),
            };
        }
        protected override void GetElements(Database database, Point3dCollection collection)
        {
            throw new NotImplementedException();
            //// 获取数据
            //var columns = GetColumns(database, collection); // 墙柱
            //var leaderMarks = GetLeaderMarks(database, collection); // 引线标注<MarkLines,MarkTexts>

            //// 识别墙柱轮廓、规格
            //var markFindService = new ThEdgeComponentMarkFindService(leaderMarks.Item1, leaderMarks.Item2);
            //foreach(var edgeComponent in columns.OfType<Polyline>())
            //{
            //    var shapeCode = Analysis(edgeComponent);
            //    if (shapeCode == ShapeCode.Unknown)
            //    {
            //        continue;
            //    }
            //    var leaderMarkInfs = markFindService.Find(edgeComponent);
            //    if (leaderMarkInfs.Count != 1)
            //    {
            //        continue;
            //    }
            //    var edgeComponentInf = leaderMarkInfs.First();
            //    edgeComponentInf.ShapeCode = shapeCode;
            //    // 解析的结果存在于edgeComponentInf中
            //    GetOutlineSpecAndLinkWallPos(edgeComponentInf, walls);
            //    Results.Add(edgeComponentInf);
            //};

            //// 释放资源
            //walls.DisposeEx();
            //var leaderObjs = new DBObjectCollection();
            //leaderMarks.Item1.ForEach(o => leaderObjs = leaderObjs.Union(o.Value));
            //leaderMarks.Item2.ForEach(o => leaderObjs = leaderObjs.Union(o.Value));
            //leaderObjs.DisposeEx();
            //var restWallColumns = wallColumns.Difference(Results.Select(o=>o.EdgeComponent).ToCollection());
            //restWallColumns.DisposeEx();
        }

        private void GetOutlineSpecAndLinkWallPos(EdgeComponentExtractInfo componentInf,DBObjectCollection walls)
        {
            // 解析的结果会放在componentInf对应的属性上
            switch (componentInf.ShapeCode)
            {
                case ShapeCode.Rect:
                    GetRectSpecAndLinkWallPos(componentInf, walls);
                    break;
                case ShapeCode.L:
                    GetLTypeSpecAndLinkWallPos(componentInf, walls);
                    break;
                case ShapeCode.T:
                    GetTTypeSpecAndLinkWallPos(componentInf, walls);
                    break;
                default:
                    break;
            }
        }

        private void GetRectSpecAndLinkWallPos(EdgeComponentExtractInfo componentInf, DBObjectCollection walls)
        {
            var specService = new ThHuaRunRectSecAnalysisService(walls, ThColumnReinforceConfig.Instance.AntiSeismicGrade);
            specService.Analysis(componentInf);
        }

        private void GetLTypeSpecAndLinkWallPos(EdgeComponentExtractInfo componentInf, DBObjectCollection walls)
        {
            var specService = new ThHuaRunLTypeSecAnalysisService(walls, ThColumnReinforceConfig.Instance.AntiSeismicGrade);
            specService.Analysis(componentInf);
        }

        private void GetTTypeSpecAndLinkWallPos(EdgeComponentExtractInfo componentInf, DBObjectCollection walls)
        {
            var specService = new ThHuaRunTTypeSecAnalysisService(walls, ThColumnReinforceConfig.Instance.AntiSeismicGrade);
            specService.Analysis(componentInf);
        }

        private ShapeCode Analysis(Entity wallColumn)
        {
            if(wallColumn is Polyline polyline)
            {
                AnalysisService.Analysis(polyline);
                return AnalysisService.ShapeCode;
            }
            else
            {
                return ShapeCode.Unknown;
            }
        }

        private DBObjectCollection GetColumns(Database database, Point3dCollection collection)
        {
            var extractService = new ThExtractColumnService(ThColumnReinforceDrawConfig.Instance.ColumnLayers);
            extractService.Extract(database, collection);
            return extractService.Elements;
        }
        private Tuple<Dictionary<string,DBObjectCollection>, Dictionary<string, DBObjectCollection>>
            GetLeaderMarks(Database database, Point3dCollection collection)
        {
            var extractService = new ThExtractLeaderMarkService(ThColumnReinforceDrawConfig.Instance.TextLayers);
            extractService.Extract(database, collection);
            return Tuple.Create(extractService.MarkLines, extractService.MarkTexts);
        }
    }
}
