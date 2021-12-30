using System.Linq;
using System.Collections.Generic;

using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore;

namespace ThMEPWSS.SprinklerConnect.Service
{
    public class ThSprinklerConnectUtils
    {
        /// <summary>
        /// 获取框线
        /// </summary>
        /// <returns></returns>
        public static List<Polyline> GetFrames()
        {
            var resPolys = new List<Polyline>();
            var options = new PromptKeywordOptions("\n选择处理方式");
            options.Keywords.Add("框选范围", "K", "框选范围(K)");
            options.Keywords.Add("选择防火分区", "P", "选择防火分区(P)");
            options.Keywords.Default = "框选范围";
            var result = Active.Editor.GetKeywords(options);
            if (result.Status != PromptStatus.OK)
            {
                return resPolys;
            }

            if (result.StringResult == "框选范围")
            {
                resPolys = GetFrameByCrossing();
            }
            else if (result.StringResult == "选择防火分区")
            {
                resPolys = GetFrameBySelectPolyline();
            }

            var clonePoly = resPolys.Select(x => x.Clone() as Polyline).ToList();
            return clonePoly;
        }

        /// <summary>
        /// 通过框选获取框线
        /// </summary>
        /// <returns></returns>
        private static List<Polyline> GetFrameByCrossing()
        {
            using (var pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                var resPolys = new List<Polyline>();
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return resPolys;
                }
                var winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());
                var frames = new List<Polyline>() { frame };

                resPolys.AddRange(GetAllFramePolys(frames));
                resPolys.Remove(frame);

                return resPolys;
            }
        }

        /// <summary>
        /// 通过点选获取框线
        /// </summary>
        /// <returns></returns>
        private static List<Polyline> GetFrameBySelectPolyline()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var resPolys = new List<Polyline>();
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var layerNames = new string[]
                {
                    ThMEPEngineCoreLayerUtils.FIRECOMPARTMENT,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames, layerNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return resPolys;
                }

                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var plBack = acdb.Element<Polyline>(frame);
                    var plFrame = ThMEPFrameService.Normalize(plBack);
                    resPolys.Add(plFrame);
                }

                return resPolys;
            }
        }

        /// <summary>
        /// 获取所有框线包括洞口
        /// </summary>
        /// <param name="frames"></param>
        /// <returns></returns>
        private static List<Polyline> GetAllFramePolys(List<Polyline> frames)
        {
            var resPolys = new List<Polyline>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var layerNames = new string[]
                {
                    ThMEPEngineCoreLayerUtils.FIRECOMPARTMENT,
                };
                var filterlist = OpFilter.Bulid(o => o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames) &
                    o.Dxf((int)DxfCode.LayerName) == string.Join(",", layerNames));
                var polys = new List<Polyline>();
                var status = Active.Editor.SelectAll(filterlist);
                if (status.Status == PromptStatus.OK)
                {
                    foreach (ObjectId obj in status.Value.GetObjectIds())
                    {
                        var plBack = acadDatabase.Element<Polyline>(obj);
                        var plFrame = ThMEPFrameService.Normalize(plBack);
                        polys.Add(plFrame);
                    }
                }

                foreach (var frame in frames)
                {
                    var checkFrame = frame.Buffer(5)[0] as Polyline;
                    polys.Where(o =>
                    {
                        return o.Area > 0 && checkFrame.Contains(o) && (frame.Area - o.Area) > 50;
                    })
                   .OfType<Polyline>()
                   .ForEachDbObject(o => resPolys.Add(o));
                    resPolys.Add(frame);
                }
            }

            return resPolys;
        }
    }
}
