﻿using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using ThMEPStructure.Reinforcement.Model;
using ThMEPStructure.Reinforcement.Service;
using ThMEPEngineCore.Algorithm;

namespace ThMEPStructure.Reinforcement.Data.YJK
{
    public class ThYJKDataSetFactory : ThMEPDataSetFactory
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
        protected override ThMEPDataSet BuildDataSet()
        {
            return new ThMEPDataSet()
            {
                Container = new List<ThGeometry>(),
            };
        }
        protected override void GetElements(Database database, Point3dCollection collection)
        {
            // 获取数据
            var wallColumns = GetWallColumns(database, collection); // 墙柱
            var walls = GetWalls(database, collection); // 墙
            var leaderMarks = GetLeaderMarks(database, collection); // 引线标注<MarkLines,MarkTexts>

            // 移动到近似原点            
            var center = collection.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            transformer.Transform(wallColumns);
            transformer.Transform(walls);
            leaderMarks.Item1.ForEach(o => transformer.Transform(o.Value));
            leaderMarks.Item2.ForEach(o => transformer.Transform(o.Value));

            // 用墙柱对墙线进行裁剪
            var bufferWallColumns = Buffer(wallColumns, ThReinforcementUtils.WallColumnBufferLength);
            var clipWalls = Clip(bufferWallColumns, walls);
            var wallLines = clipWalls.ExplodeLines();
            
            // 识别墙柱轮廓、规格
            var markFindService = new ThEdgeComponentMarkFindService(leaderMarks.Item1, leaderMarks.Item2);
            foreach(var edgeComponent in wallColumns.OfType<Polyline>())
            {
                var shapeCode = Analysis(edgeComponent);
                var leaderMarkInfs = markFindService.Find(edgeComponent);
                if (leaderMarkInfs.Count != 1)
                {
                    continue;
                }
                var edgeComponentInf = leaderMarkInfs.First();
                edgeComponentInf.ShapeCode = shapeCode;
                // 解析的结果存在于edgeComponentInf中
                GetOutlineSpecAndLinkWallPos(edgeComponentInf, wallLines);
                Results.Add(edgeComponentInf);
            };

            // 释放资源
            walls.DisposeEx();
            clipWalls.DisposeEx();
            wallLines.DisposeEx();
            bufferWallColumns.DisposeEx();
            var leaderObjs = new DBObjectCollection();
            leaderMarks.Item1.ForEach(o => leaderObjs = leaderObjs.Union(o.Value));
            leaderMarks.Item2.ForEach(o => leaderObjs = leaderObjs.Union(o.Value));
            leaderObjs.DisposeEx();
            var restWallColumns = wallColumns.Difference(Results.Select(o=>o.EdgeComponent).ToCollection());            
            // 还原到近似原点
            transformer.Reset(wallColumns);
            restWallColumns.DisposeEx();
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
            var specService = new ThHuaRunRectSecAnalysisService(walls,AntiSeismicGrade);
            specService.Analysis(componentInf);
        }

        private void GetLTypeSpecAndLinkWallPos(EdgeComponentExtractInfo componentInf, DBObjectCollection walls)
        {
            var specService = new ThHuaRunLTypeSecAnalysisService(walls, AntiSeismicGrade);
            specService.Analysis(componentInf);
        }

        private void GetTTypeSpecAndLinkWallPos(EdgeComponentExtractInfo componentInf, DBObjectCollection walls)
        {
            var specService = new ThHuaRunTTypeSecAnalysisService(walls, AntiSeismicGrade);
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
        private DBObjectCollection Clip(DBObjectCollection wallColumns,
            DBObjectCollection walls)
        {
            var results = walls.CloneEx();
            var garabages = new DBObjectCollection();
            wallColumns.OfType<Entity>().ForEach(e =>
            {
                garabages = garabages.Union(results);
                results = e.Clip(results, true);
            });
            garabages = garabages.Difference(results);
            garabages.MDispose();
            return results.OfType<Curve>().ToCollection();
        }
        private DBObjectCollection Buffer(DBObjectCollection polygons,double length)
        {
            var results = new DBObjectCollection();
            polygons.OfType<Entity>().ForEach(e =>
            {
                if(e is Polyline polyline)
                {
                    results = results.Union(polyline.Buffer(length));
                }
                else if(e is MPolygon polygon)
                {
                    results = results.Union(polygon.Buffer(length));
                }
            });
            return results;
        }
    }
}
