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
        private Matrix3d toZeroMat;
        private Tolerance tor;
        private ThDuctPortsReDrawFactory service;
        private Dictionary<Polyline, TextModifyParam> textsDic;   // 文字外包框到文字参数的映射
        private Dictionary<Polyline, DuctModifyParam> ductsDic;   // 管段外包框到管段参数的映射
        private Dictionary<Polyline, EntityModifyParam> shapesDic;// 连接件外包框到连接件参数的映射
        private ThCADCoreNTSSpatialIndex textsIndex;
        private ThCADCoreNTSSpatialIndex ductsIndex;
        private ThCADCoreNTSSpatialIndex shapesIndex;
        private CurDuctInfo curDuct;
        private ThModifyFanConnComponent fanConnService;
        public ThDuctPortsModifyDuct(string modifySize, ObjectId[] ductCompIds, DuctModifyParam curDuctParam)
        {
            InitCurDuct(ductCompIds, curDuctParam, modifySize);// 得到图层信息
            ImportTextInfo();
            Init();
            UpDateCurDuctParam();
            // 两次检测只是更新curDuctParam的参数，最后再画
            var sp = curDuct.curDuctParam.sp;
            var ep = curDuct.curDuctParam.ep;
            DetectNeighbors(sp);
            DetectNeighbors(ep);
            UpdatePort(sp, ep);
            DoProcCurDuct();
        }
        private void GetDuctVerticalOft(out Vector3d lVec, out Vector3d rVec)
        {
            var w = ThMEPHVACService.GetWidth(curDuct.curDuctParam.ductSize);
            var modifyW = ThMEPHVACService.GetWidth(curDuct.modifySize);
            var diffW = (modifyW - w) * 0.5;
            var dirVec = (curDuct.curDuctParam.ep - curDuct.curDuctParam.sp).GetNormal();
            lVec = ThMEPHVACService.GetLeftVerticalVec(dirVec) * diffW;
            rVec = ThMEPHVACService.GetRightVerticalVec(dirVec) * diffW;
        }
        private void UpdatePort(Point3d sp, Point3d ep)
        {
            // 要用未修改前的管径宽度去搜索侧送的风口
            ThDuctPortsReadComponent.GetPortBounds(moveSrtP, out Dictionary<Polyline, PortModifyParam> dicPlToPort);
            var portIndex = new ThCADCoreNTSSpatialIndex(dicPlToPort.Keys.ToCollection());
            var l = new Line(sp, ep);
            var w = ThMEPHVACService.GetWidth(curDuct.curDuctParam.ductSize);
            var pl = l.Buffer(w);
            var res = portIndex.SelectCrossingPolygon(pl);
            var dirVec = (curDuct.curDuctParam.ep - curDuct.curDuctParam.sp).GetNormal();
            GetDuctVerticalOft(out Vector3d lVec, out Vector3d rVec);
            var mat = Matrix3d.Displacement(moveSrtP.GetAsVector());
            foreach (Polyline bound in res)
            {
                var port = dicPlToPort[bound];
                if (port.portRange.Contains("侧"))
                {
                    ThDuctPortsDrawService.ClearGraph(port.handle);
                    var vec = (port.pos - sp).GetNormal();
                    if (dirVec.CrossProduct(vec).Z > 0)
                        port.pos += lVec;
                    else
                        port.pos += rVec;
                    port.pos = port.pos.TransformBy(mat);
                    service.portService.InsertPort(port);
                }
            }
        }
        private void DetectNeighbors(Point3d detectP)
        {
            var detectPl = new Polyline();
            detectPl.CreatePolygon(detectP.ToPoint2D(), 4, 10);
            var connConnector = shapesIndex.SelectCrossingPolygon(detectPl);
            if (ductsDic.Count == 0)// 当前管段被删掉了， 所以判0
                return;
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
                    throw new NotImplementedException("[CheckError]: No such connector!");
            }
        }

        private void DoProcCurDuct()
        {
            UpdateCurDuctConnText();
            curDuct.curDuctParam.ductSize = curDuct.modifySize;
            ThDuctPortsDrawService.ClearGraph(curDuct.curDuctParam.handle);
            service.DrawDuctByDuct(curDuct.curDuctParam);
        }
        private void DeleteCurDuctConnText()
        {
            var mat = Matrix3d.Displacement(moveSrtP.GetAsVector());
            var text = textsIndex.SelectCrossingPolygon(curDuct.curDuctGeo);
            foreach (Polyline pl in text)
            {
                var textInfo = textsDic[pl];
                if (textInfo.textString.Contains("x"))
                    textInfo.textString = curDuct.modifySize;
                textInfo.pos = textInfo.pos.TransformBy(mat);
                ThDuctPortsDrawService.ClearGraph(textInfo.handle);
            }
        }
        private void UpdateCurDuctConnText()
        {
            // 一根管段上只能有在侧边或在中间的文字，不能共存！
            var mat = Matrix3d.Displacement(moveSrtP.GetAsVector());
            var text = textsIndex.SelectCrossingPolygon(curDuct.curDuctGeo);
            GetDuctVerticalOft(out Vector3d lVec, out Vector3d rVec);
            var dirVec = (curDuct.curDuctParam.ep - curDuct.curDuctParam.sp).GetNormal();
            if (text.Count == 0)
            {
                // 检测在管段中间的文字
                var l = new Line(curDuct.curDuctParam.sp, curDuct.curDuctParam.ep);
                var pl = l.Buffer(1);
                text = textsIndex.SelectCrossingPolygon(pl);
                lVec = Point3d.Origin.GetAsVector();
                rVec = Point3d.Origin.GetAsVector();
            }
            foreach (Polyline pl in text)
            {
                var textInfo = textsDic[pl];
                // 不计算修改管段的标高
                if (textInfo.textString.Contains("x"))
                    textInfo.textString = curDuct.modifySize;
                var vec = (textInfo.pos - curDuct.curDuctParam.sp).GetNormal();
                if (dirVec.CrossProduct(vec).Z > 0)
                    textInfo.pos += lVec;
                else
                    textInfo.pos += rVec;
                textInfo.pos = textInfo.pos.TransformBy(mat);
                ThDuctPortsDrawService.ClearGraph(textInfo.handle);
                service.DrawTextByText(textInfo);
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
                    break;
                }
            }
            ductsIndex = new ThCADCoreNTSSpatialIndex(ductsDic.Keys.ToCollection());
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
                moveSrtP = curDuctParam.sp;
                var geoLine = db.Element<Line>(ductCompIds[0]);
                var l = new Line(curDuctParam.sp, curDuctParam.ep);
                var w = ThMEPHVACService.GetWidth(curDuctParam.ductSize);
                var dirVec = ThMEPHVACService.GetEdgeDirection(l);
                var extL = new Line(l.StartPoint - dirVec, l.EndPoint + dirVec);
                var mat = Matrix3d.Displacement(-moveSrtP.GetAsVector());
                curDuctParam.sp = Point3d.Origin;
                curDuctParam.ep = curDuctParam.ep.TransformBy(mat);
                var pl = extL.Buffer(0.5 * w);// 做等宽的外包框是为了与侧回风口求交
                pl.TransformBy(mat);
                curDuct = new CurDuctInfo() { geoLayer = geoLine.Layer, modifySize = modifySize, curDuctGeo = pl, curDuctParam = curDuctParam };
            }
        }

        private void Init()
        {
            tor = new Tolerance(1e-3, 1e-3);
            var scale = ThMEPHVACService.GetScaleByHeight(curDuct.textHeight);
            var scenario = ThMEPHVACService.GetScenarioByGeoLayer(curDuct.geoLayer);
            service = new ThDuctPortsReDrawFactory (scenario, scale, moveSrtP);
            toZeroMat = Matrix3d.Displacement(-moveSrtP.GetAsVector());
            fanConnService = new ThModifyFanConnComponent(moveSrtP);
            ReadComponent();
        }
        private void ReadComponent()
        {
            // DBText已经读进来了
            ThDuctPortsInterpreter.GetDucts(out List<DuctModifyParam> ducts);
            ThDuctPortsInterpreter.GetShapesDic(moveSrtP, out shapesDic);
            var toZeroMat = Matrix3d.Displacement(-moveSrtP.GetAsVector());
            foreach (Polyline b in textsDic.Keys.ToCollection())
            {
                b.TransformBy(toZeroMat);
                textsDic[b].pos = ThMEPHVACService.RoundPoint(textsDic[b].pos.TransformBy(toZeroMat), 6);
            }
            GetShapeDic();
            ductsDic = new Dictionary<Polyline, DuctModifyParam>();
            foreach (var d in ducts)
            {
                d.sp = ThMEPHVACService.RoundPoint(d.sp.TransformBy(toZeroMat), 6);
                d.ep = ThMEPHVACService.RoundPoint(d.ep.TransformBy(toZeroMat), 6);
                var l = new Line(d.sp, d.ep);
                ductsDic.Add(l.Buffer(1), d);
            }
            textsIndex = new ThCADCoreNTSSpatialIndex(textsDic.Keys.ToCollection());
            shapesIndex = new ThCADCoreNTSSpatialIndex(shapesDic.Keys.ToCollection());
        }
        private void GetShapeDic()
        {
            var shapes = new Dictionary<Polyline, EntityModifyParam>();
            foreach (Polyline b in shapesDic.Keys.ToCollection())
            {
                // b.TransformBy(toZeroMat); //外包框在原点附近
                shapes.Add(b, new EntityModifyParam()
                {
                    type = shapesDic[b].type,
                    centerP = ThMEPHVACService.RoundPoint(shapesDic[b].centerP.TransformBy(toZeroMat), 6),
                    handle = shapesDic[b].handle,
                    portWidths = new Dictionary<Point3d, string>()
                });
                foreach (Point3d p in shapesDic[b].portWidths.Keys)
                    shapes[b].portWidths.Add(ThMEPHVACService.RoundPoint(p.TransformBy(toZeroMat), 6), shapesDic[b].portWidths[p]);
                shapesDic[b].portWidths.Clear();
            } 
            shapesDic.Clear();
            shapesDic = shapes;
        }
        private void DoProcReducing(EntityModifyParam reducing, Point3d detectP)
        {
            var otherP = GetReducingOtherP(reducing, detectP);
            var pl = CreateDetectPl(otherP);
            var res = ductsIndex.SelectCrossingPolygon(pl);
            if (res.Count == 1)
            {
                var connDuctBounds = res[0] as Polyline;
                var connDuctParam = ductsDic[connDuctBounds];
                if (connDuctParam.ductSize == curDuct.modifySize)
                {
                    // 取消变径变成直管
                    var ep = otherP.IsEqualTo(connDuctParam.sp, tor) ? connDuctParam.ep : connDuctParam.sp;
                    var airVolume = connDuctParam.airVolume + curDuct.curDuctParam.airVolume;
                    var elevation = Math.Max(curDuct.curDuctParam.elevation, connDuctParam.elevation);
                    var remotep = ep.DistanceTo(curDuct.curDuctParam.sp) > ep.DistanceTo(curDuct.curDuctParam.ep) ? curDuct.curDuctParam.sp : curDuct.curDuctParam.ep;
                    // 此处先不将管段变为要修改的管段
                    var newDuctParam = new DuctModifyParam() { handle = curDuct.curDuctParam.handle,  sp = remotep, ep = ep, airVolume = airVolume, ductSize = curDuct.curDuctParam.ductSize, elevation = elevation, type = "Duct" };
                    // 先删再画
                    ThDuctPortsDrawService.ClearGraph(reducing.handle);
                    ThDuctPortsDrawService.ClearGraph(connDuctParam.handle);
                    curDuct.curDuctParam = newDuctParam;// 只更新参数，不画
                    DeleteCurDuctConnText();
                    return;
                }
            }
            // 修改变径相连端的大小
            reducing.portWidths[detectP] = curDuct.modifySize;
            service.DrawReducingByReducing(reducing);
            ThDuctPortsDrawService.ClearGraph(reducing.handle);
            UpdateCurDuctConnText();
        }
        private void CreateDuctAndDraw(DuctModifyParam connDuctParam, Point3d sp, Point3d ep)
        {
            var curH = ThMEPHVACService.GetHeight(connDuctParam.ductSize);
            var modifyH = ThMEPHVACService.GetHeight(curDuct.modifySize);
            var diff = -(modifyH - curH) / 1000;
            var elevation = connDuctParam.elevation + diff;
            // 设置handle的值是为了最终删除管段
            var duct = new DuctModifyParam() { type = "Duct", 
                                               airVolume = connDuctParam.airVolume,
                                               ductSize = curDuct.modifySize,
                                               elevation = elevation,
                                               sp = sp,
                                               ep = ep,
                                               handle = curDuct.curDuctParam.handle};
            curDuct.curDuctParam = duct;// 只更新参数
        }
        private void CreateReducingAndDraw(Point3d sp, Point3d ep, string srtW, string endW)
        {
            var dis = sp.DistanceTo(ep);
            if (dis > 200)
            {
                var portWidths = new Dictionary<Point3d, string>
                {
                    { sp, srtW },
                    { ep, endW }
                };
                var reducing = new EntityModifyParam() { type = "Reducing", portWidths = portWidths };
                service.DrawReducingByReducing(reducing);
            }
        }
        private Point3d SearchNearP(Dictionary<Point3d, string> portWidths, Point3d detectP)
        {
            var tor = new Tolerance(20, 20);
            foreach (Point3d p in portWidths.Keys)
            {
                if (p.IsEqualTo(detectP, tor))
                    return p;
            }
            throw new NotImplementedException("连接件入口点未找到检测点！！！");
        }
        private void DoProcCross(EntityModifyParam cross, Point3d detectP)
        {
            // 将cross对应处的端口修改并更新管段
            var modifyWidth = ThMEPHVACService.GetWidth(curDuct.modifySize);
            detectP = SearchNearP(cross.portWidths, detectP);
            var orgDuctSize = cross.portWidths[detectP];
            ThDuctPortsDrawService.ClearGraph(cross.handle);
            cross.portWidths[detectP] = curDuct.modifySize;
            service.DrawCrossByCross(cross);
            cross.portWidths[detectP] = orgDuctSize;
            UpdateCross(cross, detectP);
        }

        private void DoProcTee(EntityModifyParam tee, Point3d detectP)
        {
            // 将cross对应处的端口修改并更新管段
            detectP = SearchNearP(tee.portWidths, detectP);
            var orgDuctSize = tee.portWidths[detectP];
            ThDuctPortsDrawService.ClearGraph(tee.handle);
            tee.portWidths[detectP] = curDuct.modifySize;
            service.DrawTeeByTee(tee);
            tee.portWidths[detectP] = orgDuctSize;
            UpdateTee(tee, detectP);
        }
        private void DoProcElbow(EntityModifyParam elbow, Point3d detectP)
        {
            var modifyWidth = ThMEPHVACService.GetWidth(curDuct.modifySize);
            var elbowWidth = ThMEPHVACService.GetWidth(elbow.portWidths[detectP]);
            detectP = SearchNearP(elbow.portWidths, detectP);
            if (elbowWidth > modifyWidth)
            {
                var otherP = GetElbowOtherP(elbow, detectP);
                var pl = CreateDetectPl(otherP);
                // 弯头缩小
                var dicUpdatePoint = UpdateElbow(elbow, curDuct.modifySize);
                // 用otherP判断弯头的旋转角
                ThDuctPortsDrawService.ClearGraph(elbow.handle);
                service.DrawElbowByElbow(elbow);
                // 处理弯头的另一端|
                //                  --- 另一端是变径，直接修改变径
                //                  --- 另一端是管段，缩短管段并添加一个变径
                var flag = UpdateReducingWithElbow(otherP, pl, dicUpdatePoint);
                if (!flag)
                    UpdateDuctAndInsertReducing(otherP, pl, dicUpdatePoint[otherP]);
                // 处理弯头的detect端
                // Line: dicUpdatePoint[detectP]  curDuctOtherP // 直接画修改管径后的管段
                var curDuctOtherP = curDuct.curDuctParam.sp.IsEqualTo(detectP, tor) ? 
                    curDuct.curDuctParam.ep : curDuct.curDuctParam.sp;
                CreateDuctAndDraw(curDuct.curDuctParam, dicUpdatePoint[detectP], curDuctOtherP);
            }
            else
            {
                // 与弯头相连的管径变大，弯头不变, 弯头本端加变径
                var orgW = ThMEPHVACService.GetWidth(curDuct.curDuctParam.ductSize);
                var modifyW = ThMEPHVACService.GetWidth(curDuct.modifySize);
                var len = ThDuctPortsShapeService.GetReducingLen(modifyW, orgW);
                if (len > 200)
                {
                    if (detectP.IsEqualTo(curDuct.curDuctParam.sp, tor))
                    {
                        var dirVec = (curDuct.curDuctParam.ep - detectP).GetNormal();
                        curDuct.curDuctParam.sp += (dirVec * len);// 更新当前管段
                        CreateReducingAndDraw(detectP, curDuct.curDuctParam.sp, curDuct.curDuctParam.ductSize, curDuct.modifySize);// 插入变径
                    }
                    else
                    {
                        var dirVec = (curDuct.curDuctParam.sp - detectP).GetNormal();
                        curDuct.curDuctParam.ep += (dirVec * len);
                        CreateReducingAndDraw(detectP, curDuct.curDuctParam.ep, curDuct.curDuctParam.ductSize, curDuct.modifySize);
                    }
                }
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
                CreateReducingAndDraw(detectP, srtP, connDuctParam.ductSize, curDuct.modifySize);
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
                        CreateReducingAndDraw(otherP, dicUpdatePoint[detectP], reducing.portWidths[otherP], curDuct.curDuctParam.ductSize);
                        ThDuctPortsDrawService.ClearGraph(reducing.handle);
                        return true;
                    }
                }
            }
            return false;
        }
        private void UpdateCross(EntityModifyParam cross, Point3d detectP)
        {
            GetOtherPortInfo(cross, detectP, out List<Line> lines, out List<DuctModifyParam> connLines);
            var curLine = new Line(cross.centerP, detectP);
            var otherParams = new List<Tuple<double, string>>
            {
                new Tuple<double, string>(connLines[0].airVolume, connLines[0].ductSize),
                new Tuple<double, string>(connLines[1].airVolume, connLines[1].ductSize),
                new Tuple<double, string>(connLines[2].airVolume, connLines[2].ductSize),
            };
            var curParam = new Tuple<double, string>(curDuct.curDuctParam.airVolume, curDuct.curDuctParam.ductSize);
            var orgCrossShrink = ThDuctPortsShapeService.GetCrossShrink(curLine, curParam, lines, otherParams);
            var modifyParam = new Tuple<double, string>(curDuct.curDuctParam.airVolume, curDuct.modifySize);
            var modifyCrossShrink = ThDuctPortsShapeService.GetCrossShrink(curLine, modifyParam, lines, otherParams);
            UpdateConnectorConnDuct(orgCrossShrink, modifyCrossShrink, curLine, cross, detectP, lines, connLines);
        }
        private void UpdateTee(EntityModifyParam tee, Point3d detectP)
        {
            GetOtherPortInfo(tee, detectP, out List<Line> lines, out List<DuctModifyParam> connLines);
            var curLine = new Line(tee.centerP, detectP);
            var otherParams = new List<Tuple<double, string>>
            {
                new Tuple<double, string>(connLines[0].airVolume, connLines[0].ductSize),
                new Tuple<double, string>(connLines[1].airVolume, connLines[1].ductSize),
            };
            var curParam = new Tuple<double, string>(curDuct.curDuctParam.airVolume, curDuct.curDuctParam.ductSize);
            var orgTeeShrink = ThDuctPortsShapeService.GetTeeShrink(curLine, curParam, lines, otherParams);
            var modifyParam = new Tuple<double, string>(curDuct.curDuctParam.airVolume, curDuct.modifySize);
            var modifyTeeShrink = ThDuctPortsShapeService.GetTeeShrink(curLine, modifyParam, lines, otherParams);
            UpdateConnectorConnDuct(orgTeeShrink, modifyTeeShrink, curLine, tee, detectP, lines, connLines);
        }
        private void UpdateConnectorConnDuct(Dictionary<int, double> orgConnectorShrink, 
                                             Dictionary<int, double> modifyConnectorShrink, 
                                             Line curLine,
                                             EntityModifyParam connector,
                                             Point3d detectP,
                                             List<Line> lines,
                                             List<DuctModifyParam> connLines)
        {
            var shrinkLen = orgConnectorShrink[curLine.GetHashCode()] - modifyConnectorShrink[curLine.GetHashCode()];
            var dirVec = (connector.centerP - detectP).GetNormal();
            var s = curDuct.curDuctParam.ductSize;
            curDuct.curDuctParam.ductSize = curDuct.modifySize;
            UpdateConnDuct(curDuct.curDuctParam, shrinkLen, dirVec, connector);
            curDuct.curDuctParam.ductSize = s;
            for (int i = 0; i < lines.Count; ++i)
            {
                ThDuctPortsDrawService.ClearGraph(connLines[i].handle);
                shrinkLen = orgConnectorShrink[lines[i].GetHashCode()] - modifyConnectorShrink[lines[i].GetHashCode()];
                dirVec = (connector.centerP - lines[i].EndPoint).GetNormal();
                UpdateConnDuct(connLines[i], shrinkLen, dirVec, connector);
                service.DrawDuctByDuct(connLines[i]);
            }
        }
        private void UpdateConnDuct(DuctModifyParam ductParam, double shrinkLen, Vector3d dirVec, EntityModifyParam connector)
        {
            if (connector.centerP.DistanceTo(ductParam.sp) < connector.centerP.DistanceTo(ductParam.ep))
            {
                var orgP = ductParam.sp;
                ductParam.sp += (dirVec * shrinkLen);
                UpdateDuctValves(orgP, ductParam.ep, ductParam.sp, ductParam.ductSize);
            }
            else
            {
                var orgP = ductParam.ep;
                ductParam.ep += (dirVec * shrinkLen);
                UpdateDuctValves(ductParam.sp, orgP, ductParam.ep, ductParam.ductSize);
            }
        }
        private void UpdateDuctValves(Point3d sp, Point3d ep, Point3d newP, string ductSize)
        {
            var l = new Line(sp, ep);
            var pl = l.Buffer(1);
            var w = ThMEPHVACService.GetWidth(ductSize);
            fanConnService.UpdateVHM(pl, newP, w);
        }
        private void GetOtherPortInfo(EntityModifyParam tee, Point3d detectP, out List<Line> lines, out List<DuctModifyParam> connLines)
        {
            lines = new List<Line>();
            connLines = new List<DuctModifyParam>();
            foreach (Point3d p in tee.portWidths.Keys)
            {
                if (p.IsEqualTo(detectP, tor))
                    continue;
                lines.Add(new Line(tee.centerP, p));
                var pl = CreateDetectPl(p);
                var res = ductsIndex.SelectCrossingPolygon(pl);
                if (res.Count != 1)
                    throw new NotImplementedException("[CheckError]: Tee endline cross with none or multi ducts!");
                var polyline = res[0] as Polyline;
                var param = ductsDic[polyline];
                connLines.Add(param);
            }
        }
        private Dictionary<Point3d, Point3d> UpdateElbow(EntityModifyParam elbow, string modifyWidth)
        {
            // 缩小弯头的宽度
            var points = elbow.portWidths.Keys.ToList();
            var p1 = points.FirstOrDefault();
            var p2 = points.LastOrDefault();
            var dir1 = (p1 - elbow.centerP).GetNormal();
            var dir2 = (p2 - elbow.centerP).GetNormal();
            var angle = dir1.GetAngleTo(dir2);
            var w = ThMEPHVACService.GetWidth(elbow.portWidths[p1]);
            var orgElbowShrink = ThDuctPortsShapeService.GetElbowShrink(angle, w);
            w = ThMEPHVACService.GetWidth(modifyWidth);
            var modifyElbowShrink = ThDuctPortsShapeService.GetElbowShrink(angle, w);
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