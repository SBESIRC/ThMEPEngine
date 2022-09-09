using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using cadGraph = Autodesk.AutoCAD.GraphicsInterface;

using Linq2Acad;
using ThCADCore.NTS;
using AcHelper;
using Dreambuild.AutoCAD;
using ThCADExtension;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.GeojsonExtractor.Service;

using ThMEPHVAC.FloorHeatingCoil.Cmd;
using ThMEPHVAC.FloorHeatingCoil.Data;
using ThMEPHVAC.FloorHeatingCoil.Service;
using ThMEPHVAC.FloorHeatingCoil.Model;
using ThMEPHVAC.FloorHeatingCoil.Heating;

namespace ThMEPHVAC.FloorHeatingCoil.Service
{
    internal class ThFloorHeatingObstacleSettingService
    {
        public static string GetNearBlkName()
        {
            using (var docLock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                string blockName = "";

                var nestedEntOpt = new PromptNestedEntityOptions("\n点击障碍物图块：");
                var nestedEntRes = Active.Editor.GetNestedEntity(nestedEntOpt);

                if (nestedEntRes.Status == PromptStatus.OK && nestedEntRes.ObjectId != null)
                {
                    var entId = nestedEntRes.ObjectId;
                    var pickEntity = acadDatabase.Element<Entity>(entId);

                    if (pickEntity is BlockReference br)
                    {
                        blockName = ThMEPXRefService.OriginalFromXref(br.GetEffectiveName());
                    }
                    else
                    {
                        if (nestedEntRes.GetContainers().Length > 0)
                        {
                            var containerId = nestedEntRes.GetContainers().First();
                            var dbObj2 = acadDatabase.Element<Entity>(containerId);
                            if (dbObj2 is BlockReference br2)
                            {
                                blockName = ThMEPXRefService.OriginalFromXref(br2.GetEffectiveName());
                            }
                        }
                    }
                }

                return blockName;
            }
        }

        public static string GetNearLayerName()
        {
            using (var docLock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                string layerName = "";

                var nestedEntOpt = new PromptNestedEntityOptions("\n点击障碍图层：");
                var nestedEntRes = Active.Editor.GetNestedEntity(nestedEntOpt);
                if (nestedEntRes.Status == PromptStatus.OK && nestedEntRes.ObjectId != null)
                {
                    var entId = nestedEntRes.ObjectId;
                    var pickEntity = acadDatabase.Element<Entity>(entId);

                    if (pickEntity is Polyline || pickEntity is Line)
                    {
                        if (ThMEPXRefService.OriginalFromXref(pickEntity.Layer) != "0")
                        {
                            layerName = pickEntity.Layer;
                        }
                        else
                        {
                            var containers = nestedEntRes.GetContainers();
                            if (containers.Length > 0)
                            {
                                // 如果pick到的实体是0图层，就返回其父亲的图层
                                var parentEntity = acadDatabase.Element<Entity>(containers.First());
                                layerName = parentEntity.Layer;
                            }
                        }
                    }
                }
                return layerName;
            }
        }

        public static List<Polyline> ExtractBlk(string blkName)
        {
            using (var docLock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                List<Polyline> blkFrame = new List<Polyline>();

                var blkList = ExtractFurnitureObstacle(acadDatabase.Database, new List<string> { blkName });

                foreach (var blk in blkList)
                {
                    var poly = ThGeomUtil.GetAABB(blk);
                    if (poly != null)
                    {
                        blkFrame.Add(poly);
                    }
                }

                return blkFrame;
            }
        }

        public static List<Polyline> ExtractLayerLines(string layerName)
        {
            using (var docLock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                List<Polyline> layerFrame = new List<Polyline>();

                var layerList = ExtractLayerPolyline(acadDatabase.Database, layerName);
                var layerLineList = ExtractLayerLine(acadDatabase.Database, layerName);

                layerFrame.AddRange(layerList);
                layerFrame.AddRange(layerLineList);

                return layerFrame;
            }
        }

        private static List<BlockReference> ExtractFurnitureObstacle(Database database, List<string> blkName)
        {
            var obstacle = new List<BlockReference>();
            if (blkName.Count == 0)
            {
                return obstacle;
            }

            var sanitaryTerminalExtractor = new ThSanitaryTerminalRecognitionEngine()
            {
                BlockNameList = blkName,
                LayerFilter = new List<string>(),
            };

            sanitaryTerminalExtractor.Recognize(database, new Point3dCollection());
            obstacle = sanitaryTerminalExtractor.Elements.Select(x => x.Outline as BlockReference).ToList();

            return obstacle;
        }


        private static List<Polyline> ExtractLayerPolyline(Database database, string LayerName)
        {
            var polylineList = new List<Polyline>();
            var extractService = new ThExtractPolylineService()
            {
                ElementLayer = LayerName,
            };
            extractService.Extract(database, new Point3dCollection());
            var tempPoly = new List<Polyline>();
            tempPoly.AddRange(extractService.Polys);

            foreach (var pl in tempPoly)
            {
                var plTemp = ThHVACHandleNonClosedPolylineService.Handle(pl);
                plTemp.DPSimplify(1);
                if (plTemp.Closed == false)
                {
                    plTemp = plTemp.BufferPL(1).OfType<Polyline>().FirstOrDefault();
                }
                if (plTemp != null)
                {
                    polylineList.Add(plTemp);
                }
            }

            return polylineList;
        }

        private static List<Polyline> ExtractLayerLine(Database database, string LayerName)
        {
            var lineList = new List<Polyline>();
            var extractService = new ThExtractLineService()
            {
                ElementLayer = LayerName,
            };
            extractService.Extract(database, new Point3dCollection());
            var tempLine = new List<Line>();
            tempLine.AddRange(extractService.Lines);

            lineList.AddRange(tempLine.Select(x => x.BufferSquare(1)).ToList());

            return lineList;
        }

        

       

    }
}
