using System;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Runtime;
using ThMEPLighting.Garage.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.Garage.Service
{
    public class ThEliminateService
    {        
        private ObjectIdList CollectIds { get; set; }
        private Polyline FireRegion { get; set; }
        private ThRacewayParameter RacewayParameter { get; set; }
        private double Width { get; set; }
        private ThEliminateService(ThRacewayParameter racewayParameter,
            Polyline fireRegion, ObjectIdList collectIds,double width)
        {
            CollectIds = collectIds;
            RacewayParameter = racewayParameter;
            FireRegion = fireRegion;
            Width = width;
        }
        public static void Eliminate(ThRacewayParameter racewayParameter,
            Polyline fireRegion,ObjectIdList collectIds,double width)
        {
            var instance = new ThEliminateService(racewayParameter, fireRegion, collectIds, width);
            instance.Eliminate();
        }
        private void Eliminate()
        {
            //删除线槽、文字编号、块
            using (var acdb = AcadDatabase.Active())
            {
                var texts = GetRegionCodeText();
                var lightBlks = GetRegionLightBlock();
                var eraseTexts = GetLightNearbyTexts(lightBlks, texts);
                var eraseEnts = new List<Entity>();
                eraseEnts.AddRange(eraseTexts);
                eraseEnts.AddRange(lightBlks);
                eraseEnts.AddRange(GetRegionLightLines());
                //删除不在CollectIds集合里的对象
                //CollectIds表示当前命令生成的对象
                eraseEnts.Where(o => !IsContains(CollectIds,o.Id)).ForEach(o =>
                 {
                     o.UpgradeOpen();
                     o.Erase();
                     o.DowngradeOpen();
                 });
            }
        }
        private bool IsContains(ObjectIdList objIds,ObjectId objId)
        {
            return objIds.Where(o => o == objId).Any();
        }
        private List<DBText> GetRegionCodeText()
        {
            using (var acdb = AcadDatabase.Active())
            {
                var dbTexts = new List<DBText>();
                List<TypedValue> tvs = new List<TypedValue>();
                tvs.Add(new TypedValue((int)DxfCode.Start, RXClass.GetClass(typeof(DBText)).DxfName));
                tvs.Add(new TypedValue((int)DxfCode.LayerName, RacewayParameter.NumberTextParameter.Layer));
                //tvs.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, ThGarageLightCommon.ThGarageLightAppName));
                var sf = new SelectionFilter(tvs.ToArray());
                var psr = Active.Editor.SelectAll(sf);
                if (psr.Status == PromptStatus.OK)
                {                    
                    psr.Value.GetObjectIds().ForEach(o => dbTexts.Add(acdb.Element<DBText>(o)));
                    dbTexts=dbTexts.Where(o => FireRegion.Contains(o.Position)).ToList();
                }
                return dbTexts.Where(o => !IsContains(CollectIds, o.ObjectId)).ToList();
            }
        }
        private List<BlockReference> GetRegionLightBlock()
        {
            using (var acdb = AcadDatabase.Active())
            {
                var blocks = new List<BlockReference>();
                List<TypedValue> tvs = new List<TypedValue>();
                tvs.Add(new TypedValue((int)DxfCode.Start, RXClass.GetClass(typeof(BlockReference)).DxfName));
                tvs.Add(new TypedValue((int)DxfCode.LayerName, RacewayParameter.LaneLineBlockParameter.Layer));
                tvs.Add(new TypedValue((int)DxfCode.BlockName, ThGarageLightCommon.LaneLineLightBlockName));
                tvs.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, ThGarageLightCommon.ThGarageLightAppName));
                var pts = FireRegion.Vertices();
                var sf = new SelectionFilter(tvs.ToArray());
                var psr = Active.Editor.SelectAll(sf);
                if (psr.Status == PromptStatus.OK)
                {                    
                    psr.Value.GetObjectIds().ForEach(o => blocks.Add(acdb.Element<BlockReference>(o)));
                    blocks = blocks.Where(o => FireRegion.Contains(o.Position)).ToList();
                }
                return blocks;
            }
        }
        private List<Line> GetRegionLightLines()
        {
            using (var acdb = AcadDatabase.Active())
            {
                var lightLines = new List<Line>();
                var layers = new List<string>();
                layers.Add(RacewayParameter.SideLineParameter.Layer);
                layers.Add(RacewayParameter.PortLineParameter.Layer);
                layers.Add(RacewayParameter.CenterLineParameter.Layer);
                layers=layers.Distinct().ToList();
                List<TypedValue> tvs = new List<TypedValue>();
                tvs.Add(new TypedValue((int)DxfCode.Start, RXClass.GetClass(typeof(Line)).DxfName));
                tvs.Add(new TypedValue((int)DxfCode.LayerName, string.Join(",",layers)));
                tvs.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, ThGarageLightCommon.ThGarageLightAppName));
                var pts = FireRegion.Vertices();
                var sf = new SelectionFilter(tvs.ToArray());
                var psr = Active.Editor.SelectAll(sf);
                if (psr.Status == PromptStatus.OK)
                {                   
                    psr.Value.GetObjectIds().ForEach(o => lightLines.Add(acdb.Element<Line>(o)));
                    lightLines = lightLines.Where(o => FireRegion.Contains(o)).ToList();
                }
                return lightLines;
            }
        }
        private List<DBText> GetLightNearbyTexts(List<BlockReference> brs, List<DBText> dbTexts)
        {
            var texts = new List<DBText>();
            var textObjs = new DBObjectCollection();
            dbTexts.ForEach(o => textObjs.Add(o));
            double textHeight = 0.0;
            if (dbTexts.Count > 0)
            {
                textHeight = dbTexts.GroupBy(o => o.Height).OrderByDescending(o => o.Count()).First().Key;
            }
            var textSpatialIndex = new ThCADCoreNTSSpatialIndex(textObjs);
            brs.ForEach(o =>
            {
                var tvs = XDataTools.GetXData(o.ObjectId, ThGarageLightCommon.ThGarageLightAppName);
                double blkAng = (double)tvs[3].Value;
                blkAng = ThGarageLightUtils.LightNumberAngle(blkAng / Math.PI * 180.0);
                blkAng = blkAng / 180.0 * Math.PI;
                var mt = Matrix3d.Rotation(blkAng + Math.PI / 2.0, Vector3d.ZAxis, o.Position);
                var dir = Vector3d.XAxis.TransformBy(mt);
                var firstPt = o.Position + dir.MultiplyBy(Width / 2.0 + 100.0+ textHeight/2.0);
                var secondPt = o.Position - dir.MultiplyBy(Width / 2.0 + 100.0 + textHeight / 2.0);
                var outline = ThDrawTool.ToOutline(firstPt, secondPt, 1.0);
                var closeTexts = textSpatialIndex
                .SelectCrossingPolygon(outline)
                .Cast<DBText>()
                .Where(d=>!IsContains(CollectIds,d.ObjectId))
                .Where(d => !texts.Contains(d))
                .OrderBy(d => d.Position.DistanceTo(o.Position));
                if (closeTexts.Count()>0)
                {
                   texts.Add(closeTexts.First());
                }                
            });
            return texts;
        }
        private Line GetBlockCenter(BlockReference blockObj, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var lines = new List<Line>();
                // 获取车位块的OBB
                // 用OBB创建一个矩形多段线来“代替”车位块
                var btr = acadDatabase.Blocks.Element(blockObj.BlockTableRecord);
                var polyline=btr.GeometricExtents().ToRectangle().GetTransformedCopy(matrix) as Polyline;
                var line1MidPt = ThGeometryTool.GetMidPt(polyline.GetPoint3dAt(0), polyline.GetPoint3dAt(1));
                var line2MidPt = ThGeometryTool.GetMidPt(polyline.GetPoint3dAt(1), polyline.GetPoint3dAt(2));
                var line3MidPt = ThGeometryTool.GetMidPt(polyline.GetPoint3dAt(2), polyline.GetPoint3dAt(3));
                var line4MidPt = ThGeometryTool.GetMidPt(polyline.GetPoint3dAt(0), polyline.GetPoint3dAt(3));
                if(line1MidPt.DistanceTo(line3MidPt)> line2MidPt.DistanceTo(line4MidPt))
                {
                    return new Line(line1MidPt, line3MidPt);
                }
                else
                {
                    return new Line(line2MidPt, line4MidPt);
                }
            }
        }
    }
}
