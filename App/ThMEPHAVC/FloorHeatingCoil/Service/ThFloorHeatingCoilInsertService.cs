using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using DotNetARX;
using Linq2Acad;

namespace ThMEPHVAC.FloorHeatingCoil.Service
{
    public static class ThFloorHeatingCoilInsertService
    {
        public static void InsertSuggestBlock(Point3d insertPt, int route, double suggestDist, double length, string insertBlkName, bool withColor = false)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var attNameValues = BuildRootSuggestAttr(route, suggestDist, length);

                var blkName = insertBlkName;
                var pt = insertPt;
                double rotateAngle = 0;//TransformBy(Active.Editor.UCS2WCS());
                var scale = 1;
                var layerName = ThFloorHeatingCommon.BlkLayerDict[insertBlkName];
                var objId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                                          layerName,
                                          blkName,
                                          pt,
                                          new Scale3d(scale),
                                          rotateAngle,
                                          attNameValues);

                if (withColor == true)
                {
                    var blk = acadDatabase.Element<BlockReference>(objId);
                    blk.UpgradeOpen();//切换属性对象为写的状态
                    blk.ColorIndex = route % 6;
                    blk.DowngradeOpen();//为了安全，将属性对象的状态改为读
                }
            }
        }

        public static void UpdateSuggestBlock(BlockReference blk, int route, double suggestDist, double length, bool withColor = false)
        {
            var attNameValues = BuildRootSuggestAttr(route, suggestDist, length);
            blk.ObjectId.UpdateAttributesInBlock(attNameValues);
            if (withColor == true)
            {
                blk.UpgradeOpen();//切换属性对象为写的状态
                blk.ColorIndex = route % 6;
                blk.DowngradeOpen();//为了安全，将属性对象的状态改为读
            }
        }

        public static void InsertCoil(List<Polyline> pipes, bool withColor = false)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                if (pipes != null)
                {
                    for (int i = 0; i < pipes.Count; i++)
                    {
                        var poly = pipes[i];
                        poly.Linetype = "ByLayer";
                        poly.Layer = ThFloorHeatingCommon.Layer_Coil;
                        if (withColor == true)
                        {
                            poly.ColorIndex = i % 6;
                        }
                        acadDatabase.ModelSpace.Add(poly);
                    }
                }
            }
        }

        private static Dictionary<string, string> BuildRootSuggestAttr(int route, double suggestDist, double lenth)
        {
            var attNameValues = new Dictionary<string, string>();
            attNameValues.Add(ThFloorHeatingCommon.BlkSettingAttrName_RoomSuggest_Route, string.Format("HL{0}", route));
            attNameValues.Add(ThFloorHeatingCommon.BlkSettingAttrName_RoomSuggest_Dist, string.Format("A={0}", suggestDist));
            attNameValues.Add(ThFloorHeatingCommon.BlkSettingAttrName_RoomSuggest_Length, string.Format("L≈{0}m", lenth));

            return attNameValues;
        }


        public static void ShowConnectivity(List<Polyline> roomgraph, int colorIndex)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var poly in roomgraph)
                {
                    poly.Linetype = "ByLayer";
                    poly.Layer = ThFloorHeatingCommon.Layer_RoomSetFrame;
                    poly.ColorIndex = colorIndex;
                    var objid = acadDatabase.ModelSpace.Add(poly);


                    var ids = new ObjectIdCollection();
                    ids.Add(objid);
                    var hatch = new Hatch();
                    hatch.PatternScale = 1;
                    hatch.ColorIndex = colorIndex;
                    hatch.CreateHatch(HatchPatternType.PreDefined, "SOLID", true);
                    hatch.AppendLoop(HatchLoopTypes.Outermost, ids);
                    hatch.EvaluateHatch(true);
                 
                }

            }
        }


    }
}
