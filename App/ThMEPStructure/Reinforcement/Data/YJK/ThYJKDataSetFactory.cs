using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPStructure.Reinforcement.Model;
using ThMEPStructure.Reinforcement.Service;

namespace ThMEPStructure.Reinforcement.Data.YJK
{
    public class ThYJKDataSetFactory
    {
        #region ---------- 外部传入 -----------
        /// <summary>
        /// 抗震等级
        /// </summary>
        public string AntiSeismicGrade { get; set; }
        /// <summary>
        /// 标注文字图层
        /// </summary>
        public List<string> TextLayers { get; set; }
        /// <summary>
        /// 墙图层
        /// </summary>
        public List<string> WallLayers { get; set; }
        /// <summary>
        /// 墙柱图层
        /// </summary>
        public List<string> WallColumnLayers { get; set; }
        #endregion
        public List<EdgeComponentExtractInfo> Results { get; set; }
        private ThShapeAnalysisService AnalysisService { get; set; }
        public ThYJKDataSetFactory()
        {
            TextLayers = new List<string>();
            WallLayers = new List<string>();
            WallColumnLayers = new List<string>();
            Results= new List<EdgeComponentExtractInfo>();
            AnalysisService = new ThShapeAnalysisService();
        }
        public void GetElements(Database database, Point3dCollection collection)
        {
            // 获取数据
            var wallColumns = GetWallColumns(database, collection); // 墙柱
            var walls = GetWalls(database, collection); // 墙
            var leaderMarks = GetLeaderMarks(database, collection); // 引线标注<MarkLines,MarkTexts>

            // 识别墙柱轮廓、规格
            var markFindService = new ThEdgeComponentMarkFindService(leaderMarks.Item1, leaderMarks.Item2);
            foreach(var edgeComponent in wallColumns.OfType<Polyline>())
            {
                var shapeCode = Analysis(edgeComponent);
                if (shapeCode == ShapeCode.Unknown)
                {
                    continue;
                }
                var leaderMarkInfs = markFindService.Find(edgeComponent);
                if (leaderMarkInfs.Count != 1)
                {
                    continue;
                }
                var edgeComponentInf = leaderMarkInfs.First();
                edgeComponentInf.ShapeCode = shapeCode;


            };
        }

        private string GetPolylineSpec(Polyline polyline, ShapeCode shapeCode,
            string antiSeismicGrade,string code,DBObjectCollection walls)
        {
            var spec = "";
            switch (shapeCode)
            {
                case ShapeCode.Rect:
                    spec = GetRectSpec(polyline, antiSeismicGrade, code, walls);
                    break;
                case ShapeCode.L:
                    spec = GetLTypeSpec(polyline, antiSeismicGrade, code, walls);
                    break;
                case ShapeCode.T:
                    spec = GetTTypeSpec(polyline, antiSeismicGrade, code, walls);
                    break;
                default:
                    spec = "";
                    break;
            }
            return spec;
        }

        private string GetRectSpec(Polyline polyline, string antiSeismicGrade, string code, DBObjectCollection walls)
        {
            var specService = new ThHuaRunRectSecAnalysisService(walls, code, antiSeismicGrade);
            specService.Analysis(polyline);
            return specService.Spec;
        }

        private string GetLTypeSpec(Polyline polyline,string antiSeismicGrade, string code, DBObjectCollection walls)
        {
            var specService = new ThHuaRunLTypeSecAnalysisService(walls, code, antiSeismicGrade);
            specService.Analysis(polyline);
            return specService.Spec;
        }

        private string GetTTypeSpec(Polyline polyline, string antiSeismicGrade, string code, DBObjectCollection walls)
        {
            var specService = new ThHuaRunTTypeSecAnalysisService(walls, code, antiSeismicGrade);
            specService.Analysis(polyline);
            return specService.Spec;
        }

        private EdgeComponentExtractInfo CreateEdgeComponentExtractInfo(
            Polyline outline,ShapeCode shapeCode)
        {
            return new EdgeComponentExtractInfo
            {
                EdgeComponent = outline,
                ShapeCode = shapeCode,
            };
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

        private DBObjectCollection GetWallColumns(Database database, Point3dCollection collection)
        {
            var extractService = new ThExtractWallColumnService(WallColumnLayers);
            extractService.Extract(database, collection);
            return extractService.Elements;
        }
        private DBObjectCollection GetWalls(Database database, Point3dCollection collection)
        {
            var extractService = new ThExtractWallService(WallLayers);
            extractService.Extract(database, collection);
            return extractService.Elements;
        }
        private Tuple<Dictionary<string,DBObjectCollection>, Dictionary<string, DBObjectCollection>>
            GetLeaderMarks(Database database, Point3dCollection collection)
        {
            var extractService = new ThExtractLeaderMarkService(TextLayers);
            extractService.Extract(database, collection);
            return Tuple.Create(extractService.MarkLines, extractService.MarkTexts);
        }
    }
}
