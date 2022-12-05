using System;
using System.Linq;
using System.Collections.Generic;

using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.ChargerDistribution.Common;

namespace ThMEPElectrical.ChargerDistribution.Service
{
    public static class ThChargerSelector
    {
        /// <summary>
        /// 选择区域
        /// </summary>
        /// <returns></returns>
        public static List<Entity> GetFrames(AcadDatabase acad)
        {
            using (var acadDatabase = AcadDatabase.Use(acad.Database))
            {
                var resPolys = new List<Polyline>();
                var options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var result = Active.Editor.GetSelection(options);
                if (result.Status != PromptStatus.OK)
                {
                    return new List<Entity>();
                }

                foreach (var obj in result.Value.GetObjectIds()
                    .Where(o => o.ObjectClass.DxfName == RXClass.GetClass(typeof(Polyline)).DxfName))
                {
                    var frame = acadDatabase.ElementOrDefault<Polyline>(obj);
                    if (frame.IsNull())
                    {
                        continue;
                    }

                    var frameFix = frame.Clone() as Polyline;
                    if (!frameFix.Closed && frameFix.StartPoint.DistanceTo(frameFix.EndPoint) < 1000.0)
                    {
                        frameFix.Closed = true;
                    }
                    var collection = new DBObjectCollection { frameFix };
                    var polylineArea = collection.PolygonsEx().OfType<Polyline>().OrderByDescending(o => o.Area).FirstOrDefault();
                    var mPolygonArea = collection.PolygonsEx().OfType<MPolygon>().OrderByDescending(o => o.Area).FirstOrDefault();
                    if (!polylineArea.IsNull() && !mPolygonArea.IsNull())
                    {
                        if (polylineArea.Area > mPolygonArea.Area)
                        {
                            resPolys.Add(polylineArea);
                        }
                        else
                        {
                            resPolys.Add(mPolygonArea.Shell());
                        }
                    }
                    else if (!polylineArea.IsNull())
                    {
                        resPolys.Add(polylineArea);
                    }
                    else if (!mPolygonArea.IsNull())
                    {
                        resPolys.Add(mPolygonArea.Shell());
                    }
                }

                // 创建框线空间关系
                var results = new List<Entity>();
                resPolys = resPolys.OrderByDescending(o => o.Area).ToList();
                var search = new List<Polyline>();
                for (var i = 0; i < resPolys.Count; i++)
                {
                    if (search.Contains(resPolys[i]))
                    {
                        continue;
                    }
                    search.Add(resPolys[i]);
                    var list = new DBObjectCollection { resPolys[i] };
                    for (var j = i + 1; j < resPolys.Count; j++)
                    {
                        if (!search.Contains(resPolys[j]) && resPolys[i].Contains(resPolys[j]))
                        {
                            list.Add(resPolys[j]);
                            search.Add(resPolys[j]);
                        }
                    }

                    if (list.Count == 1)
                    {
                        results.Add(resPolys[i]);
                    }
                    else
                    {
                        results.Add(ThMPolygonTool.CreateMPolygon(list));
                    }
                }
                return results;
            }
        }

        public static List<Point3d> GetChargerBlock(AcadDatabase acad)
        {
            using (var acadDatabase = AcadDatabase.Use(acad.Database))
            {
                var options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择充电桩设备",
                    RejectObjectsOnLockedLayers = true,
                };
                var result = Active.Editor.GetSelection(options);
                if (result.Status != PromptStatus.OK)
                {
                    return new List<Point3d>();
                }

                return result.Value.GetObjectIds()
                    .Where(o => o.ObjectClass.DxfName == RXClass.GetClass(typeof(BlockReference)).DxfName)
                    .Select(o => acadDatabase.ElementOrDefault<BlockReference>(o))
                    .Where(o => o != null && ThChargerDistributionCommon.Block_Name_Filter.Contains(o.Name))
                    .Select(o => o.Position).ToList();
            }
        }

        public static Polyline PickPolyline(AcadDatabase acad)
        {
            using (var acadDatabase = AcadDatabase.Use(acad.Database))
            {
                var result = Active.Editor.GetEntity("请选择分组线");
                if (result.Status != PromptStatus.OK)
                {
                    return null;
                }

                if (result.ObjectId.ObjectClass.DxfName == RXClass.GetClass(typeof(Polyline)).DxfName)
                {
                    var pline = acadDatabase.ElementOrDefault<Polyline>(result.ObjectId);
                    if (!pline.IsNull() && pline.Layer == ThChargerDistributionCommon.Grouping_Layer)
                    {
                        return pline;
                    }
                }
                return null;
            }
        }
    }
}
