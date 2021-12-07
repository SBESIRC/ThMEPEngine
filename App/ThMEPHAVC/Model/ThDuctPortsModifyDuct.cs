using System;
using System.Linq;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using NFox.Cad;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPHVAC.Model
{
    public class CurDuctInfo
    {
        public string geoLayer;
        public string modifySize;
        public double textHeight;
        public Polyline curDuctGeo;
        public DuctModifyParam curDuctParam;
    }
    public class ThDuctPortsModifyDuct
    {
        private Point3d moveSrtP;
        private Matrix3d toOrgMat;
        private Matrix3d toZeroMat;
        private Tolerance tor;
        private ThDuctPortsReDrawFactory service;
        private Dictionary<Polyline, FanModifyParam> fansDic;     // 风机外包框到文字参数的映射
        private Dictionary<Polyline, TextModifyParam> textsDic;   // 文字外包框到文字参数的映射
        private Dictionary<Polyline, DuctModifyParam> ductsDic;   // 管段外包框到管段参数的映射
        private Dictionary<Polyline, EntityModifyParam> shapesDic;// 连接件外包框到连接件参数的映射
        private ThCADCoreNTSSpatialIndex fansIndex;
        private ThCADCoreNTSSpatialIndex textsIndex;
        private ThCADCoreNTSSpatialIndex ductsIndex;
        private ThCADCoreNTSSpatialIndex shapesIndex;
        private CurDuctInfo curDuct;
        private ThModifyFanConnComponent fanConnService;
        public ThDuctPortsModifyDuct(string modifySize, ObjectId[] ductCompIds, DuctModifyParam curDuctParam)
        {
            using (var db = AcadDatabase.Active())
            {
                InitCurDuct(ductCompIds, curDuctParam, modifySize);// 得到图层信息
                ImportTextInfo();
                Init();
                UpDateCurDuctParam();
                DetectNeighbors(curDuct.curDuctParam.sp);
            }
        }
        private void DetectNeighbors(Point3d detectP)
        {
            var detectPl = new Polyline();
            detectPl.CreatePolygon(detectP.ToPoint2D(), 4, 10);
            var connConnector = shapesIndex.SelectCrossingPolygon(detectPl);
            foreach (Polyline pl in connConnector)
            {
                var connectorParam = shapesDic[pl];
                if (connectorParam.type == "Reducing" || connectorParam.type == "AxisReducing")
                    DoProcReducing(connectorParam, detectP);
                else if (connectorParam.type == "Elbow")
                    DoProcElbow(connectorParam, detectP);
                else if (connectorParam.type == "Tee")
                    DoProcTee(connectorParam, detectP);
                else if (connectorParam.type == "Cross")
                    DoProcCross(connectorParam, detectP);
                else
                    throw new NotImplementedException();
            }

        }

        private void UpDateCurDuctParam()
        {
            // 从字典中取出当前管段的参数后再构造管段的空间索引
            foreach (var ductParam in ductsDic)
            {
                if (ductParam.Value.handle == curDuct.curDuctParam.handle)
                {
                    curDuct.curDuctParam = ductParam.Value;
                    ductsDic.Remove(ductParam.Key);
                    return;
                }
            }
            var toOrgMat = Matrix3d.Displacement(-moveSrtP.GetAsVector());
            foreach (Polyline b in ductsDic.Keys.ToCollection())
            {
                b.TransformBy(toOrgMat);
                var ductParam = ductsDic[b];
                ductParam.sp = ductParam.sp.TransformBy(toOrgMat);
                ductParam.ep = ductParam.ep.TransformBy(toOrgMat);
            }
            ductsIndex = new ThCADCoreNTSSpatialIndex(ductsDic.Keys.ToCollection());
            throw new NotImplementedException("[CheckError]: Can not find cur duct param in dictionary!");
        }

        private void ImportTextInfo()
        {
            ThDuctPortsInterpreter.GetTextsDic(out textsDic);
            foreach (var param in textsDic.Values)
            {
                curDuct.textHeight = param.height;
                return;
            }
            throw new NotImplementedException("[CheckError]: None of DBText in this DWG!");
        }

        private void InitCurDuct(ObjectId[] ductCompIds, DuctModifyParam curDuctParam, string modifySize)
        {
            using (var db = AcadDatabase.Active())
            {
                var geoLine = db.Element<Line>(ductCompIds[0]);
                var l = new Line(curDuctParam.sp, curDuctParam.ep);
                var w = ThMEPHVACService.GetWidth(curDuctParam.ductSize);
                var dirVec = ThMEPHVACService.GetEdgeDirection(l);
                var extL = new Line(l.StartPoint - dirVec, l.EndPoint + dirVec);
                var pl = extL.Buffer(0.5 * w);// 做等宽的外包框是为了与侧回风口求交
                curDuct = new CurDuctInfo() { geoLayer = geoLine.Layer, modifySize = modifySize, curDuctGeo = pl, curDuctParam = curDuctParam };
            }
        }

        private void Init()
        {
            tor = new Tolerance(1e-3, 1e-3);
            var scale = ThMEPHVACService.GetScaleByHeight(curDuct.textHeight);
            var scenario = ThMEPHVACService.GetScenarioByGeoLayer(curDuct.geoLayer);
            moveSrtP = curDuct.curDuctParam.sp;
            service = new ThDuctPortsReDrawFactory (scenario, scale, moveSrtP);
            toOrgMat = Matrix3d.Displacement(moveSrtP.GetAsVector());
            toZeroMat = Matrix3d.Displacement(-moveSrtP.GetAsVector());
            fanConnService = new ThModifyFanConnComponent(moveSrtP);
            ReadComponent();
        }
        private void ReadComponent()
        {
            // DBText已经读进来了
            ThDuctPortsInterpreter.GetFanDic(out fansDic);
            ThDuctPortsInterpreter.GetDuctsDic(out ductsDic);
            ThDuctPortsInterpreter.GetShapesDic(out shapesDic);
            var toZeroMat = Matrix3d.Displacement(-moveSrtP.GetAsVector());
            foreach (Polyline b in fansDic.Keys.ToCollection())
                b.TransformBy(toZeroMat);
            foreach (Polyline b in textsDic.Keys.ToCollection())
            {
                b.TransformBy(toZeroMat);
                textsDic[b].pos = textsDic[b].pos.TransformBy(toZeroMat);
            }
            foreach (Polyline b in shapesDic.Keys.ToCollection())
            {
                b.TransformBy(toZeroMat);
                foreach (var port in shapesDic[b].portWidths)
                {
                    var record = shapesDic[b];
                    var p = ThMEPHVACService.RoundPoint(port.Key.TransformBy(toZeroMat), 6);
                    record.portWidths.Add(p, record.portWidths[port.Key]);
                    record.portWidths.Remove(port.Key);
                }
            }

            fansIndex = new ThCADCoreNTSSpatialIndex(fansDic.Keys.ToCollection());
            textsIndex = new ThCADCoreNTSSpatialIndex(textsDic.Keys.ToCollection());
            shapesIndex = new ThCADCoreNTSSpatialIndex(shapesDic.Keys.ToCollection());
        }
        private void DoProcReducing(EntityModifyParam reducing, Point3d detectP)
        {
            var otherP = GetReducingOtherP(reducing, detectP);
            var pl = CreateDetectPl(otherP);
            var res = ductsIndex.SelectCrossingPolygon(pl);
            var connDuctBounds = res[0] as Polyline;
            var connDuctParam = ductsDic[connDuctBounds];
            if (connDuctParam.ductSize == curDuct.modifySize)
            {
                // 取消变径变成直管
                var sp = otherP.IsEqualTo(connDuctParam.sp, tor) ? connDuctParam.ep : connDuctParam.sp;
                var airVolume = connDuctParam.airVolume + curDuct.curDuctParam.airVolume;
                var elevation = Math.Max(curDuct.curDuctParam.elevation, connDuctParam.elevation);
                var newDuctParam = new DuctModifyParam() { sp = sp, ep = detectP, airVolume = airVolume, ductSize = curDuct.modifySize, elevation = elevation, type = "Duct" };
                service.DrawDuctByDuct(newDuctParam);
                ThDuctPortsDrawService.ClearGraph(reducing.handle);
                ThDuctPortsDrawService.ClearGraph(connDuctParam.handle);
                ThDuctPortsDrawService.ClearGraph(curDuct.curDuctParam.handle);
            }
            else
            {
                // 修改变径相连端的大小
                var w = ThMEPHVACService.GetWidth(curDuct.modifySize);
                reducing.portWidths[detectP] = w;
                service.DrawReducingByReducing(reducing);
                curDuct.curDuctParam.ductSize = curDuct.modifySize;
                service.DrawDuctByDuct(curDuct.curDuctParam);
                ThDuctPortsDrawService.ClearGraph(reducing.handle);
                ThDuctPortsDrawService.ClearGraph(curDuct.curDuctParam.handle);

            }
        }
        private double GetMainHeight(double curElevation, double height, double uiElevation)
        {
            return curElevation * 1000 + height - uiElevation * 1000;
        }
        private void CreateDuctAndDraw(DuctModifyParam connDuctParam, Point3d sp, Point3d ep, double airVolume)
        {
            var curH = ThMEPHVACService.GetHeight(connDuctParam.ductSize);
            var modifyH = ThMEPHVACService.GetHeight(curDuct.modifySize);
            var diff = -(modifyH - curH) / 1000;
            var elevation = connDuctParam.elevation + diff;
            var duct = new DuctModifyParam() { type = "Duct", 
                                               airVolume = connDuctParam.airVolume,
                                               ductSize = curDuct.modifySize,
                                               elevation = elevation,
                                               sp = sp,
                                               ep = ep };
            service.DrawDuctByDuct(duct);
        }
        private void CreateReducingAndDraw(Point3d sp, Point3d ep, double srtW, double endW)
        {
            var dis = sp.DistanceTo(ep);
            if (dis > 200)
            {
                var portWidths = new Dictionary<Point3d, double>
                {
                    { sp, srtW },
                    { ep, endW }
                };
                var reducing = new EntityModifyParam() { type = "Reducing", portWidths = portWidths };
                service.DrawReducingByReducing(reducing);
            }
        }
        private void DoProcCross(EntityModifyParam connectorParam, Point3d detectP)
        {
            throw new NotImplementedException();
        }

        private void DoProcTee(EntityModifyParam connectorParam, Point3d detectP)
        {
            // 将三通对应处的端口变粗并更新管段

        }
        private void DoProcElbow(EntityModifyParam elbow, Point3d detectP)
        {
            var modifyWidth = ThMEPHVACService.GetWidth(curDuct.modifySize);
            if (elbow.portWidths[detectP] > modifyWidth)
            {
                var otherP = GetElbowOtherP(elbow, detectP);
                var pl = CreateDetectPl(otherP);
                // 弯头缩小
                var dicUpdatePoint = UpdateElbow(elbow, modifyWidth);
                service.DrawElbowByElbow(elbow);
                // 处理弯头的另一端|
                //                  --- 另一端是变径，直接修改变径
                //                  --- 另一端是管段，缩短管段并添加一个变径
                var flag = UpdateReducingWithElbow(otherP, pl, dicUpdatePoint);
                if (!flag)
                    UpdateDuctAndInsertReducing(otherP, pl, dicUpdatePoint[detectP]);
                // 处理弯头的detect端
                // Line: dicUpdatePoint[detectP]  curDuctOtherP // 直接画修改管径后的管段
                var curDuctOtherP = GetDuctOtherP(curDuct.curDuctParam, detectP);
                CreateDuctAndDraw(curDuct.curDuctParam, dicUpdatePoint[detectP], curDuctOtherP, curDuct.curDuctParam.airVolume);
            }
            else
            {
                // 与弯头相连的管径变大，弯头不变, 弯头本端加变径
                var pl = CreateDetectPl(detectP);
                UpdateDuctAndInsertReducing(detectP, pl, detectP);
            }
        }
        private void UpdateDuctAndInsertReducing(Point3d detectP, Polyline pl, Point3d srtP)
        {
            var res = ductsIndex.SelectCrossingPolygon(pl);
            if (res.Count > 0)
            {
                // Line: srtP  endP // 插点加变径和直管
                var connDuctBounds = res[0] as Polyline;
                var connDuctParam = ductsDic[connDuctBounds];
                var connDuctOtherP = GetDuctOtherP(connDuctParam, detectP);
                var orgW = ThMEPHVACService.GetWidth(connDuctParam.ductSize);
                var modifyW = ThMEPHVACService.GetWidth(curDuct.curDuctParam.ductSize);
                var reducingLen = ThMEPHVACService.CalcReducingLen(orgW, modifyW);

                var dirVec = (connDuctOtherP - srtP).GetNormal();
                var insertP = srtP + dirVec * reducingLen;
                CreateReducingAndDraw(insertP, srtP, orgW, modifyW);
                CreateDuctAndDraw(connDuctParam, connDuctOtherP, insertP, connDuctParam.airVolume);
            }
        }
        private bool UpdateReducingWithElbow(Point3d detectP,
                                             Polyline pl,
                                             Dictionary<Point3d, Point3d> dicUpdatePoint)
        {
            var res = shapesIndex.SelectCrossingPolygon(pl);
            if (res.Count > 0)
            {
                foreach (Polyline polyline in res)
                {
                    // 与弯头相连的不可能是轴流的变径
                    if (shapesDic[polyline].type == "Reducing")
                    {
                        // 改变相连端变径的宽度
                        var reducing = shapesDic[polyline];
                        var otherP = GetReducingOtherP(reducing, detectP);
                        var modifyW = ThMEPHVACService.GetWidth(curDuct.curDuctParam.ductSize);
                        CreateReducingAndDraw(otherP, dicUpdatePoint[detectP], reducing.portWidths[otherP], modifyW);
                        ThDuctPortsDrawService.ClearGraph(reducing.handle);
                        return true;
                    }
                }
            }
            return false;
        }

        private Dictionary<Point3d, Point3d> UpdateElbow(EntityModifyParam elbow, double modifyWidth)
        {
            // 缩小弯头的宽度
            var points = elbow.portWidths.Keys.ToList();
            var p1 = points.FirstOrDefault();
            var p2 = points.LastOrDefault();
            var dir1 = (p1 - elbow.centerP).GetNormal();
            var dir2 = (p2 - elbow.centerP).GetNormal();
            var angle = dir1.GetAngleTo(dir2);
            var orgElbowShrink = ThDuctPortsShapeService.GetElbowShrink(angle, elbow.portWidths[p1]);
            var modifyElbowShrink = ThDuctPortsShapeService.GetElbowShrink(angle, modifyWidth);
            var shrinkLen = orgElbowShrink - modifyElbowShrink;
            var newP1 = p1 - dir1 * shrinkLen;
            var newP2 = p2 - dir2 * shrinkLen;
            elbow.portWidths.Clear();
            elbow.portWidths.Add(newP1, modifyWidth);
            elbow.portWidths.Add(newP2, modifyWidth);
            var dic = new Dictionary<Point3d, Point3d>
            {
                { p1, newP1 },
                { p2, newP2 }
            };
            return dic;
        }

        private Point3d GetDuctOtherP(DuctModifyParam duct, Point3d detectP)
        {
            return duct.sp.IsEqualTo(detectP, tor) ? duct.ep : duct.sp;
        }
        private Point3d GetReducingOtherP(EntityModifyParam reducing, Point3d detectP)
        {
            foreach (var p in reducing.portWidths.Keys)
                if (!p.IsEqualTo(detectP))
                    return p;
            throw new NotImplementedException();
        }
        private Point3d GetElbowOtherP(EntityModifyParam elbow, Point3d detectP)
        {
            foreach (var p in elbow.portWidths.Keys)
                if (!p.IsEqualTo(detectP))
                    return p;
            throw new NotImplementedException();
        }
        private Polyline CreateDetectPl(Point3d otherP)
        {
            var pl = new Polyline();
            pl.CreatePolygon(otherP.ToPoint2D(), 4, 10);
            return pl;
        }
    }
}