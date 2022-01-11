using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.LaneLine;

namespace ThMEPLighting.FEI
{
    public class GetPrimitivesService
    {
        ThMEPOriginTransformer originTransformer;
        public GetPrimitivesService(ThMEPOriginTransformer originTransformer)
        {
            this.originTransformer = originTransformer;
        }

        /// <summary>
        /// 获取车道线
        /// </summary>
        /// <returns></returns>
        public List<List<Line>> GetLanes(Polyline polyline, out List<List<Line>> otherLanes)
        {
            otherLanes = new List<List<Line>>();
            var objs = new DBObjectCollection();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var laneLines = acdb.ModelSpace
                .OfType<Curve>()
                .Where(o => o.Layer == ThMEPEngineCoreCommon.LANELINE_LAYER_NAME || o.Layer == ThMEPEngineCoreLayerUtils.CENTERLINE);
                laneLines.ForEach(x =>
                {
                    var transCurve = x.Clone() as Curve;
                    originTransformer.Transform(transCurve);
                    objs.Add(transCurve);
                });
            }
            
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();
            if (sprayLines.Count <= 0)
            {
                return new List<List<Line>>();
            }
            sprayLines = sprayLines.SelectMany(x => polyline.Trim(x).Cast<Entity>().Where(y => y is Curve).Cast<Curve>().ToList()).ToList();

            //处理车道线
            var handleLines = ThMEPLineExtension.LineSimplifier(sprayLines.ToCollection(), 500, 100.0, 2.0, Math.PI / 180.0);
            var parkingLinesService = new ParkingLinesService();
            var parkingLines = parkingLinesService.CreateNodedParkingLines(polyline, handleLines, out List<List<Line>> otherPLines);
            otherLanes = otherPLines;

            return parkingLines;
        }

        /// <summary>
        /// 获取构建
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        public void GetStructureInfo(Polyline polyline, out List<Polyline> columns, out List<Polyline> walls)
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var ColumnExtractEngine = new ThColumnExtractionEngine();
                ColumnExtractEngine.Extract(acdb.Database);
                ColumnExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
                var ColumnEngine = new ThColumnRecognitionEngine();
                ColumnEngine.Recognize(ColumnExtractEngine.Results, polyline.Vertices());

                // 启动墙识别引擎
                var ShearWallExtractEngine = new ThShearWallExtractionEngine();
                ShearWallExtractEngine.Extract(acdb.Database);
                ShearWallExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
                var ShearWallEngine = new ThShearWallRecognitionEngine();
                ShearWallEngine.Recognize(ShearWallExtractEngine.Results, polyline.Vertices());

                var archWallExtractEngine = new ThDB3ArchWallExtractionEngine();
                archWallExtractEngine.Extract(acdb.Database);
                archWallExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
                var archWallEngine = new ThDB3ArchWallRecognitionEngine();
                archWallEngine.Recognize(archWallExtractEngine.Results, polyline.Vertices());

                ////获取柱
                columns = new List<Polyline>();
                columns = ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
                var objs = new DBObjectCollection();
                columns.ForEach(x => objs.Add(x));
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

                //获取剪力墙
                walls = new List<Polyline>();
                walls = ShearWallEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
                objs = new DBObjectCollection();
                walls.ForEach(x => objs.Add(x));
                thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

                //获取建筑墙
                foreach (var o in archWallEngine.Elements)
                {
                    if (o.Outline is Polyline wall)
                    {
                        walls.Add(wall);
                    }
                }
            }
        }

        /// <summary>
        /// 获取区域内的主要疏散路径或辅助
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public List<Curve> GetMainEvacuate(Polyline polyline, string name) 
        {
            var objs = new DBObjectCollection();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var exitLines = acdb.ModelSpace
                .OfType<Curve>()
                .Where(x => x.Layer == name);
                exitLines.ForEach(x =>
                {
                    var transCurve = x.Clone() as Curve;
                    originTransformer.Transform(transCurve);
                    objs.Add(transCurve);
                });
            }
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();
            return sprayLines;
        }

        /// <summary>
        /// 获取出入口图块
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public List<BlockReference> GetEvacuationExitBlock(Polyline polyline)
        {
            var objs = new DBObjectCollection();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var exitBlock = acdb.ModelSpace
                .OfType<BlockReference>()
                .Where(x => !x.BlockTableRecord.IsNull)
                .Where(x =>
                {
                    var name = x.GetEffectiveName();
                    return name == ThMEPLightingCommon.FEI_EXIT_NAME100 ||
                     name == ThMEPLightingCommon.FEI_EXIT_NAME101 ||
                     name == ThMEPLightingCommon.FEI_EXIT_NAME102 ||
                     name == ThMEPLightingCommon.FEI_EXIT_NAME103 ||
                     name == ThMEPLightingCommon.FEI_EXIT_NAME140 ||
                     name == ThMEPLightingCommon.FEI_EXIT_NAME141;

                });
                exitBlock.ForEach(x =>
                {
                    var transBlock = x.Clone() as BlockReference;
                    originTransformer.Transform(transBlock);
                    objs.Add(transBlock);
                });
            }

            List<BlockReference> blocks = new List<BlockReference>();
            foreach (BlockReference obj in objs)
            {
                if (polyline.Contains(obj.Position) && !blocks.Any(x=>x.Position.DistanceTo(obj.Position) < 50))
                {
                    blocks.Add(obj);
                }
            }
            
            return blocks;
        }
    }
}
