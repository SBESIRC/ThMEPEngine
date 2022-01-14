﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Algorithm;
using ThMEPHVAC.IndoorFanLayout.Models;
using ThMEPHVAC.IndoorFanModels;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.IndoorFanLayout.Business
{
    class FanRectangleToBlock
    {
        ThMEPOriginTransformer _originTransformer;
        ThDuctPortsDrawService drawServiceAirSupply;
        ThDuctPortsDrawService drawServiceReturnAir;
        List<FanLoadBase> _allFanLoad;
        EnumFanType _enumFanType;
        double _fanPipLevel = 3000.0;
        IndoorFanLayoutModel _indoorFanLayout;
        public FanRectangleToBlock(List<FanLoadBase> allFanLoad, ThMEPOriginTransformer originTransformer, IndoorFanLayoutModel indoorFanLayout)
        {
            _allFanLoad = new List<FanLoadBase>();
            _originTransformer = originTransformer;
            _indoorFanLayout = indoorFanLayout;
            foreach (var item in allFanLoad)
            {
                _allFanLoad.Add(item);
            }
            drawServiceAirSupply = new ThDuctPortsDrawService("空调送风", "");
            drawServiceReturnAir = new ThDuctPortsDrawService("空调回风", "");
        }
        public void AddBlock(List<FanLayoutRect> fanLayoutRects, EnumFanType enumFanType)
        {
            _enumFanType = enumFanType;
            if (null == fanLayoutRects || fanLayoutRects.Count < 1)
                return;
            using (var acdb = AcadDatabase.Active())
            {
                foreach (var item in fanLayoutRects)
                {
                    if (null == item || null == item.FanDirection)
                        continue;
                    switch (enumFanType)
                    {
                        case EnumFanType.FanCoilUnitFourControls:
                        case EnumFanType.FanCoilUnitTwoControls:
                            AddCoilFan(acdb, item);
                            break;
                        case EnumFanType.VRFConditioninConduit:
                            AddVRFImpellerFan(acdb, item);
                            break;
                        case EnumFanType.VRFConditioninFourSides:
                            AddVRFFourSide(acdb, item);
                            break;
                    }
                }
            }
        }
        public void AddBlock(List<FanLayoutDetailed> fanLayoutRects, EnumFanType enumFanType)
        {
            _enumFanType = enumFanType;
            if (null == fanLayoutRects || fanLayoutRects.Count < 1)
                return;
            using (var acdb = AcadDatabase.Active())
            {
                foreach (var layoutRect in fanLayoutRects)
                {
                    if (null == layoutRect || null == layoutRect.FanDirection)
                        continue;
                    string fanName = layoutRect.FanLayoutName;
                    var fanLoad = _allFanLoad.Where(c => c.FanNumber == fanName).FirstOrDefault();
                    if (fanLoad == null)
                        return;
                    var startPoint = layoutRect.StartPoint;
                    var endPoint = layoutRect.EndPoint;
                    if (null != _originTransformer)
                    {
                        endPoint = _originTransformer.Reset(endPoint);
                        startPoint = _originTransformer.Reset(startPoint);
                    }
                    var fanDir = layoutRect.FanDirection;
                    var otherDir = fanDir.CrossProduct(Vector3d.ZAxis);
                    var coolAngle = Vector3d.YAxis.GetAngleTo(layoutRect.FanDirection, Vector3d.ZAxis);
                    coolAngle = coolAngle % (Math.PI * 2);
                    var addFanId = AddFanBlock(acdb, fanLoad, layoutRect.FanPoint, coolAngle);
                    if (addFanId == null || !addFanId.IsValid)
                        continue;
                    var value = addFanId.GetDynBlockValue("设备深度");
                    double.TryParse(value, out double fanLength);
                    switch (enumFanType)
                    {
                        case EnumFanType.FanCoilUnitFourControls:
                        case EnumFanType.FanCoilUnitTwoControls:
                            var coonectorWidth = fanLoad.FanWidth;
                            var centerWidth = coonectorWidth - 134.0 - 15 * 2;
                            if (layoutRect.HaveReturnVent)
                            {
                                AddRetrunAirPort(acdb, layoutRect.FanReturnVentCenterPoint, fanLoad, coolAngle);
                                //根据连接件计算变径
                                fanLength = fanLength - 135.0 + 19.0;
                                if (_indoorFanLayout.AirReturnType == EnumAirReturnType.AirReturnPipe)
                                {
                                    //回风管
                                    var SectionEnd = layoutRect.FanPoint - fanDir.MultiplyBy(fanLength + IndoorFanCommon.ReducingLength);
                                    if (null != _originTransformer)
                                        SectionEnd = _originTransformer.Reset(SectionEnd);
                                    AddAirDuct(new Line(startPoint, SectionEnd), layoutRect.Width, 10.00, _fanPipLevel, true);
                                    //连接件变径
                                    var startReducingSPoint = SectionEnd;
                                    var startReducingEPoint = SectionEnd + layoutRect.FanDirection.MultiplyBy(IndoorFanCommon.ReducingLength);
                                    AddDuctReducing(new Line(startReducingSPoint, startReducingEPoint), layoutRect.Width, centerWidth, false);
                                }
                                else
                                {
                                    //回风箱
                                    var boxLength = layoutRect.FanPoint.DistanceTo(layoutRect.StartPoint) - fanLength;
                                    var pt1 = startPoint - otherDir.MultiplyBy(centerWidth / 2);
                                    var pt2 = startPoint + otherDir.MultiplyBy(centerWidth / 2);
                                    var pt1End = pt1 + fanDir.MultiplyBy(boxLength);
                                    var pt2End = pt2 + fanDir.MultiplyBy(boxLength);
                                    var poly = new Polyline();
                                    poly.Layer = IndoorFanBlockServices.FanBoxLayerName;
                                    poly.Closed = true;
                                    poly.AddVertexAt(0, pt1End.ToPoint2D(), 0, 0, 0);
                                    poly.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
                                    poly.AddVertexAt(0, pt2.ToPoint2D(), 0, 0, 0);
                                    poly.AddVertexAt(0, pt2End.ToPoint2D(), 0, 0, 0);
                                    acdb.ModelSpace.Add(poly);
                                }
                            }
                            if (layoutRect.FanInnerVents.Count < 1)
                                continue;
                            var centerPoint1 = layoutRect.FanPoint;
                            if (null != _originTransformer)
                                centerPoint1 = _originTransformer.Reset(centerPoint1);
                            var centerPoint2 = centerPoint1 + fanDir.MultiplyBy(IndoorFanCommon.ReducingLength);
                            AddDuctReducing(new Line(centerPoint1, centerPoint2), centerWidth, layoutRect.Width, true);
                            //第二段风管 送风管
                            var secondStart = centerPoint2; //startPoint + dir.MultiplyBy(startSectionLength + secondSectionLenght);
                            AddAirDuct(new Line(secondStart, endPoint), layoutRect.Width, 10.0, _fanPipLevel, false);
                            break;
                        case EnumFanType.VRFConditioninConduit:
                            //var vafFanId = AddVRFFan(acdb, fanLoad, layoutRect.FanPoint, coolAngle);
                            if (layoutRect.HaveReturnVent)
                            {
                                AddRetrunAirPort(acdb, layoutRect.FanReturnVentCenterPoint, fanLoad, coolAngle);
                                var SectionEnd = startPoint + fanDir.MultiplyBy(layoutRect.StartPoint.DistanceTo(layoutRect.FanPoint) - fanLength);
                                //根据连接件计算变径
                                if (_indoorFanLayout.AirReturnType == EnumAirReturnType.AirReturnPipe)
                                {
                                    //回风管
                                    AddAirDuct(new Line(startPoint, SectionEnd), layoutRect.Width, 10.00, _fanPipLevel, true);
                                }
                                else
                                {
                                    //回风箱
                                    var boxLength = SectionEnd.DistanceTo(startPoint);
                                    var pt1 = startPoint - otherDir.MultiplyBy(layoutRect.Width / 2);
                                    var pt2 = startPoint + otherDir.MultiplyBy(layoutRect.Width / 2);
                                    var pt1End = pt1 + fanDir.MultiplyBy(boxLength);
                                    var pt2End = pt2 + fanDir.MultiplyBy(boxLength);
                                    var poly = new Polyline();
                                    poly.Layer = IndoorFanBlockServices.FanBoxLayerName;
                                    poly.Closed = true;
                                    poly.AddVertexAt(0, pt1End.ToPoint2D(), 0, 0, 0);
                                    poly.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
                                    poly.AddVertexAt(0, pt2.ToPoint2D(), 0, 0, 0);
                                    poly.AddVertexAt(0, pt2End.ToPoint2D(), 0, 0, 0);
                                    acdb.ModelSpace.Add(poly);
                                }
                            }
                            if (layoutRect.FanInnerVents.Count < 1)
                                continue;
                            var vrfStart = layoutRect.FanPoint;
                            if (null != _originTransformer)
                                vrfStart = _originTransformer.Reset(vrfStart);
                            AddAirDuct(new Line(vrfStart, endPoint), layoutRect.Width, 10.0, _fanPipLevel, false);
                            break;
                        case EnumFanType.IntegratedAirConditionin:
                            var airFanWidth = fanLoad.FanWidth - 200;
                            if (layoutRect.HaveReturnVent)
                            {
                                AddRetrunAirPort(acdb, layoutRect.FanReturnVentCenterPoint, fanLoad, coolAngle);
                                //根据连接件计算变径
                                if (_indoorFanLayout.AirReturnType == EnumAirReturnType.AirReturnPipe)
                                {
                                    fanLength = fanLength + 60;
                                    //回风管
                                    var SectionEnd = layoutRect.FanPoint - fanDir.MultiplyBy(fanLength + IndoorFanCommon.ReducingLength);
                                    if (null != _originTransformer)
                                        SectionEnd = _originTransformer.Reset(SectionEnd);
                                    AddAirDuct(new Line(startPoint, SectionEnd), layoutRect.Width, 10.00, _fanPipLevel, true);
                                    //连接件变径
                                    var startReducingSPoint = SectionEnd;
                                    var startReducingEPoint = SectionEnd + layoutRect.FanDirection.MultiplyBy(IndoorFanCommon.ReducingLength);
                                    AddDuctReducing(new Line(startReducingSPoint, startReducingEPoint), layoutRect.Width, airFanWidth, false);
                                }
                                else
                                {
                                    //回风箱
                                    var boxLength = layoutRect.FanPoint.DistanceTo(layoutRect.StartPoint) - fanLength;
                                    var pt1 = startPoint - otherDir.MultiplyBy(fanLoad.FanWidth / 2);
                                    var pt2 = startPoint + otherDir.MultiplyBy(fanLoad.FanWidth / 2);
                                    var pt1End = pt1 + fanDir.MultiplyBy(boxLength);
                                    var pt2End = pt2 + fanDir.MultiplyBy(boxLength);
                                    var poly = new Polyline();
                                    poly.Layer = IndoorFanBlockServices.FanBoxLayerName;
                                    poly.Closed = true;
                                    poly.AddVertexAt(0, pt1End.ToPoint2D(), 0, 0, 0);
                                    poly.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
                                    poly.AddVertexAt(0, pt2.ToPoint2D(), 0, 0, 0);
                                    poly.AddVertexAt(0, pt2End.ToPoint2D(), 0, 0, 0);
                                    acdb.ModelSpace.Add(poly);
                                }
                            }

                            break;
                    }
                    AddAirPort(acdb, fanLoad, layoutRect.FanInnerVents, coolAngle);
                }
            }
        }
        private void AddCoilFan(AcadDatabase acdb, FanLayoutRect layoutRect)
        {
            string fanName = layoutRect.FanLayoutName;
            var fanLoad = _allFanLoad.Where(c => c.FanNumber == fanName).FirstOrDefault();
            if (fanLoad == null)
                return;
            var volume = fanLoad.FanAirVolumeDouble;
            double startSectionLength = 429.0;
            double secondSectionLenght = fanLoad.FanLength;
            var center = _originTransformer.Reset(layoutRect.CenterPoint);
            var dir = layoutRect.FanDirection;
            var otherDir = Vector3d.ZAxis.CrossProduct(dir);
            var length = layoutRect.Length;
            var baseCenter = center - dir.MultiplyBy(length / 2);
            var fanPoint = baseCenter + dir.MultiplyBy(startSectionLength / 2);
            var attr = new Dictionary<string, string>();
            attr.Add("风量", string.Format("{0}m3/h", volume));
            var dynAttr = new Dictionary<string, object>();
            dynAttr.Add("风口类型", "下回风口");
            dynAttr.Add("风口长度", fanLoad.ReturnAirSizeWidth);
            dynAttr.Add("风口宽度", fanLoad.ReturnAirSizeLength);
            var angle = Vector3d.YAxis.GetAngleTo(dir, Vector3d.ZAxis);
            angle = angle % (Math.PI * 2);
            AddCoilAirPort(acdb, fanPoint, angle, attr, dynAttr);

            //添加连接件（风机）
            string blockName = _enumFanType == EnumFanType.FanCoilUnitTwoControls ? IndoorFanBlockServices.CoilFanTwoBlackName : IndoorFanBlockServices.CoilFanFourBlackName;
            var connectorPoint = baseCenter + layoutRect.FanDirection.MultiplyBy(470 + startSectionLength);
            var connectorDynAttrs = new Dictionary<string, object>();
            var connectorAttrs = IndoorFanBlockServices.GetFanBlockAttrDynAttrs(fanLoad, out connectorDynAttrs);
            var connectorId = acdb.ModelSpace.ObjectId.InsertBlockReference(
                IndoorFanBlockServices.CoilFanLayerName,
                blockName,
                connectorPoint,
                new Scale3d(1),
                angle,
                connectorAttrs);
            if (null == connectorId || !connectorId.IsValid)
                return;
            SetBlockDynAttrs(connectorId, connectorDynAttrs);
            ChangeBlockTextAttrAngle(connectorId, connectorAttrs.Select(c => c.Key).ToList(), angle);

            //第一段风管,回风管或风箱
            var startPoint = center - dir.MultiplyBy(length / 2);

            //根据连接件计算变径
            var coonectorWidth = fanLoad.FanWidth;
            var centerWidth = coonectorWidth - 134.0 - 15 * 2;
            if (_indoorFanLayout.AirReturnType == EnumAirReturnType.AirReturnPipe)
            {
                //回风管
                var SectionEnd = startPoint + dir.MultiplyBy(startSectionLength);
                AddAirDuct(new Line(startPoint, SectionEnd), layoutRect.Width, 10.00, _fanPipLevel, true);
                //连接件变径
                var startReducingSPoint = SectionEnd;
                var startReducingEPoint = SectionEnd + dir.MultiplyBy(IndoorFanCommon.ReducingLength);
                AddDuctReducing(new Line(startReducingSPoint, startReducingEPoint), layoutRect.Width, centerWidth, false);
            }
            else
            {
                //回风箱
                var boxLength = startSectionLength + IndoorFanCommon.ReducingLength;
                var pt1 = startPoint - otherDir.MultiplyBy(centerWidth / 2);
                var pt2 = startPoint + otherDir.MultiplyBy(centerWidth / 2);
                var pt1End = pt1 + dir.MultiplyBy(boxLength);
                var pt2End = pt2 + dir.MultiplyBy(boxLength);
                var poly = new Polyline();
                poly.Layer = IndoorFanBlockServices.FanBoxLayerName;
                poly.Closed = true;
                poly.AddVertexAt(0, pt1End.ToPoint2D(), 0, 0, 0);
                poly.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
                poly.AddVertexAt(0, pt2.ToPoint2D(), 0, 0, 0);
                poly.AddVertexAt(0, pt2End.ToPoint2D(), 0, 0, 0);
                acdb.ModelSpace.Add(poly);
            }
            if (!_indoorFanLayout.CreateBlastPipe)
                return;
            //送风管和风机的变径
            var centerPoint1 = connectorPoint;
            var centerPoint2 = connectorPoint + dir.MultiplyBy(IndoorFanCommon.ReducingLength);
            AddDuctReducing(new Line(centerPoint1, centerPoint2), centerWidth, layoutRect.Width, true);
            //第二段风管 送风管
            var secondStart = centerPoint2; //startPoint + dir.MultiplyBy(startSectionLength + secondSectionLenght);
            var endPoint = center + dir.MultiplyBy(length / 2);
            AddAirDuct(new Line(secondStart, endPoint), layoutRect.Width, 10.0, _fanPipLevel, false);

            AddAirPort(acdb, fanLoad, layoutRect.InnerVentRects.Select(c => c.CenterPoint).ToList(), angle);
        }
        private void AddVRFImpellerFan(AcadDatabase acdb, FanLayoutRect layoutRect)
        {
            //VRF是室内机（管道机）
            string fanName = layoutRect.FanLayoutName;
            var fanLoad = _allFanLoad.Where(c => c.FanNumber == fanName).FirstOrDefault();
            if (fanLoad == null)
                return;
            double fanWidth = fanLoad.ReturnAirSizeWidth;
            double fanLength = fanLoad.ReturnAirSizeLength;
            var volume = fanLoad.FanAirVolumeDouble;
            var vrfFan = fanLoad as VRFImpellerFanLoad;
            double startSectionLength = 100 + fanLoad.ReturnAirSizeLength + 50;
            double secondSectionLenght = fanLoad.FanLength;//vrfFan.;
            var center = layoutRect.CenterPoint;
            if (null != _originTransformer)
                center = _originTransformer.Reset(center);
            var dir = layoutRect.FanDirection;
            var otherDir = Vector3d.ZAxis.CrossProduct(dir);
            var length = layoutRect.Length;
            var baseCenter = center - dir.MultiplyBy(length / 2);
            var fanPoint = baseCenter + dir.MultiplyBy(100 + fanLoad.ReturnAirSizeLength / 2);
            var attr = new Dictionary<string, string>();
            attr.Add("风量", string.Format("{0}m3/h", volume));
            var dynAttr = new Dictionary<string, object>();
            dynAttr.Add("风口类型", "下回风口");
            dynAttr.Add("风口长度", fanWidth);
            dynAttr.Add("风口宽度", fanLength);
            var angle = Vector3d.YAxis.GetAngleTo(dir, Vector3d.ZAxis);
            angle = angle % (Math.PI * 2);
            //AddCoilAirPort(acdb, fanPoint, angle, attr, dynAttr,Math.PI/2);
            AddCoilAirPort(acdb, fanPoint, angle, attr, dynAttr);

            var startPoint = center - dir.MultiplyBy(length / 2);
            if (_indoorFanLayout.AirReturnType == EnumAirReturnType.AirReturnPipe)
            {
                //回风管
                var SectionEnd = startPoint + dir.MultiplyBy(startSectionLength);
                AddAirDuct(new Line(startPoint, SectionEnd), layoutRect.Width, 10.00, _fanPipLevel, true);
            }
            else
            {
                //回风箱
                var boxLength = startSectionLength;
                var pt1 = startPoint - otherDir.MultiplyBy(layoutRect.Width / 2);
                var pt2 = startPoint + otherDir.MultiplyBy(layoutRect.Width / 2);
                var pt1End = pt1 + dir.MultiplyBy(boxLength);
                var pt2End = pt2 + dir.MultiplyBy(boxLength);
                var poly = new Polyline();
                poly.Layer = IndoorFanBlockServices.FanBoxLayerName;
                poly.Closed = true;
                poly.AddVertexAt(0, pt1End.ToPoint2D(), 0, 0, 0);
                poly.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
                poly.AddVertexAt(0, pt2.ToPoint2D(), 0, 0, 0);
                poly.AddVertexAt(0, pt2End.ToPoint2D(), 0, 0, 0);
                acdb.ModelSpace.Add(poly);
            }
            //添加连接件（风机）
            var connectorPoint = baseCenter + layoutRect.FanDirection.MultiplyBy(secondSectionLenght + startSectionLength);
            var connectorDynAttrs = new Dictionary<string, object>();
            var connectorAttrs = IndoorFanBlockServices.GetFanBlockAttrDynAttrs(fanLoad, out connectorDynAttrs);
            var connectorId = acdb.ModelSpace.ObjectId.InsertBlockReference(
                IndoorFanBlockServices.VRFFanLayerName,
                IndoorFanBlockServices.VRFFanBlackName,
                connectorPoint,
                new Scale3d(1),
                angle,
                connectorAttrs);
            if (null == connectorId || !connectorId.IsValid)
                return;
            SetBlockDynAttrs(connectorId, connectorDynAttrs);
            ChangeBlockTextAttrAngle(connectorId, new List<string> { "设备编号" }, angle + Math.PI / 2);
            ChangeBlockTextAttrAngle(connectorId, new List<string> { "设备电量", "制冷量/制热量" }, angle);
            if (!_indoorFanLayout.CreateBlastPipe)
                return;
            //第二段风管 送风管
            var secondStart = startPoint + dir.MultiplyBy(startSectionLength + secondSectionLenght);
            var endPoint = center + dir.MultiplyBy(length / 2);
            AddAirDuct(new Line(secondStart, endPoint), layoutRect.Width, 10.0, _fanPipLevel, false);
            AddAirPort(acdb, fanLoad, layoutRect.InnerVentRects.Select(c => c.CenterPoint).ToList(), angle);
        }
        private void AddVRFFourSide(AcadDatabase acdb, FanLayoutRect layoutRect)
        {
            //VRF室内机，四面出风型
            string fanName = layoutRect.FanLayoutName;
            var fanLoad = _allFanLoad.Where(c => c.FanNumber == fanName).FirstOrDefault();
            if (fanLoad == null)
                return;
            var center = null != _originTransformer ? _originTransformer.Reset(layoutRect.CenterPoint) : layoutRect.CenterPoint;
            var connectorDynAttrs = new Dictionary<string, object>();
            var connectorAttrs = IndoorFanBlockServices.GetFanBlockAttrDynAttrs(fanLoad, out connectorDynAttrs);
            var angle = Vector3d.YAxis.GetAngleTo(layoutRect.FanDirection, Vector3d.ZAxis);
            angle = angle % (Math.PI * 2);
            var connectorId = acdb.ModelSpace.ObjectId.InsertBlockReference(
                IndoorFanBlockServices.VRFFanLayerName,
                IndoorFanBlockServices.VRFFanFourSideBlackName,
                center,
                new Scale3d(1),
                angle,
                connectorAttrs);
            if (null == connectorId || !connectorId.IsValid)
                return;
            SetBlockDynAttrs(connectorId, connectorDynAttrs);
            ChangeBlockTextAttrAngle(connectorId, new List<string> { "制冷量/制热量", "设备电量", "设备编号" }, angle);
        }

        private ObjectId AddFanBlock(AcadDatabase acdb, FanLoadBase fanLoad, Point3d fanPoint, double angle) 
        {
            var createPoint = null != _originTransformer ? _originTransformer.Reset(fanPoint) : fanPoint;
            var connectorDynAttrs = new Dictionary<string, object>();
            var connectorAttrs = IndoorFanBlockServices.GetFanBlockAttrDynAttrs(fanLoad, out connectorDynAttrs);

            string layerName = "";
            string blockName = "";
            blockName = IndoorFanBlockServices.GetBlockLayerNameTextAngle(_enumFanType, out layerName, out double textAngle);

            var addId = acdb.ModelSpace.ObjectId.InsertBlockReference(
                                layerName, blockName,
                                createPoint,
                                new Scale3d(1, 1, 1),
                                angle,
                                connectorAttrs);
            if (null == addId || !addId.IsValid)
                return new ObjectId();
            SetBlockDynAttrs(addId, connectorDynAttrs);
            ChangeBlockTextAttrAngle(addId, connectorAttrs.Select(c => c.Key).ToList(), angle);
            ChangeBlockTextAttrAngle(addId, new List<string> { "设备编号" }, angle + textAngle);
            return addId;
        }
        private void AddRetrunAirPort(AcadDatabase acdb,Point3d portPoint, FanLoadBase fanLoad, double angle) 
        {
            var fanPoint = null != _originTransformer ? _originTransformer.Reset(portPoint):portPoint;
            var attr = new Dictionary<string, string>();
            attr.Add("风量", string.Format("{0}m3/h", fanLoad.FanAirVolumeDouble));
            var dynAttr = new Dictionary<string, object>();
            dynAttr.Add("风口类型", "下回风口");
            dynAttr.Add("风口长度", fanLoad.ReturnAirSizeWidth);
            dynAttr.Add("风口宽度", fanLoad.ReturnAirSizeLength);
            AddCoilAirPort(acdb, fanPoint, angle, attr, dynAttr);
        }
        private void AddAirPort(AcadDatabase acdb, FanLoadBase fanLoad, List<Point3d> portPoints,double angle) 
        {
            if (portPoints == null || portPoints.Count < 1)
                return;
            //添加出风口
            double ventWidth = fanLoad.GetCoilFanVentSize(portPoints.Count, out double ventLength);
            double ventVolume = fanLoad.FanAirVolumeDouble / portPoints.Count;
            string ventType = VentAttrName(fanLoad.AirSupplyOutletType);
            var atts = VentAttrs(ventType,ventWidth, ventLength);
            foreach (var portPoint in portPoints)
            {
                var ventCenter = portPoint;
                if(null != _originTransformer)
                    ventCenter = _originTransformer.Reset(ventCenter);
                var ventAttrs = new Dictionary<string, string>();
                ventAttrs.Add("风量", string.Format("{0}m3/h", ventVolume));
                var ventDynAttrs = new Dictionary<string, object>();
                ventDynAttrs.Add("风口类型", ventType);
                foreach(var keyValue in atts)
                    ventDynAttrs.Add(keyValue.Key, keyValue.Value);
                AddCoilAirPort(acdb, ventCenter, angle, ventAttrs, ventDynAttrs);
            }
        }
        private string VentAttrName(string ventType) 
        {
            string ventAttrName = ventType;
            if (ventType.Contains("百叶"))
                ventAttrName = "下送风口";
            return ventAttrName;
        }
        private Dictionary<string,object> VentAttrs(string ventType,double width,double length)
        {
            var ventAttrs = new Dictionary<string, object>();
            if (ventType.Contains("圆")) 
            {
                ventAttrs.Add("圆形风口直径", width);
            }
            else if (ventType.Contains("下送风口"))
            {
                ventAttrs.Add("风口宽度", width);
                ventAttrs.Add("风口长度", length);
            }
            else 
            {
                ventAttrs.Add("方形散流器喉部宽度", width);
            }
            return ventAttrs;
        }
        private void AddCoilAirPort(AcadDatabase acdb, Point3d createPoint, double angle, Dictionary<string, string> attr, Dictionary<string, object> dynAttr, double textAngleOffSet =0)
        {
            var id = acdb.ModelSpace.ObjectId.InsertBlockReference(
                IndoorFanBlockServices.FanVentLayerName,
                IndoorFanBlockServices.FanVentBlackName,
                createPoint,
                new Scale3d(1),
                angle,
                attr);
            if (null == id || !id.IsValid)
                return;
            SetBlockDynAttrs(id, dynAttr);
            ChangeBlockTextAttrAngle(id, attr.Select(c => c.Key).ToList(), angle+ textAngleOffSet);
        }
        private void SetBlockDynAttrs(ObjectId blockId, Dictionary<string, object> dynAttr)
        {
            if(null == blockId || !blockId.IsValid)
                return;
            foreach (var dyAttr in dynAttr)
            {
                if (dyAttr.Key == null || dyAttr.Value == null)
                    continue;
                blockId.SetDynBlockValue(dyAttr.Key, dyAttr.Value);
            }
        }
        private void ChangeBlockTextAttrAngle(ObjectId blockId,List<string> changeAngleAttrs,double angle) 
        {
            var block = blockId.GetDBObject<BlockReference>();
            // 遍历块参照的属性
            foreach (ObjectId attId in block.AttributeCollection)
            {
                AttributeReference attRef = attId.GetDBObject<AttributeReference>();
                if (!changeAngleAttrs.Any(c => c.Equals(attRef.Tag)))
                    continue;
                attRef.Rotation = angle;
            }
        }
        void AddAirDuct(Line centerLine, double width, double volume, double elevation,bool isReturnAir)
        {
            var lineGeo = GetDuctLineSeg(centerLine, width, volume, elevation);
            AddAirDuct(lineGeo, isReturnAir);
        }
        void AddAirDuct(SegInfo ductLine,bool isReturnAir) 
        {
            if(isReturnAir)
                drawServiceReturnAir.DrawDuct(new List<SegInfo> { ductLine }, Matrix3d.Identity);
            else
                drawServiceAirSupply.DrawDuct(new List<SegInfo> { ductLine }, Matrix3d.Identity);
        }
        void AddAirReducing(LineGeoInfo linGeo, bool isReturnAir) 
        {
            if (isReturnAir)
                drawServiceReturnAir.DrawReducing(new List<LineGeoInfo> { linGeo }, Matrix3d.Identity);
            else
                drawServiceAirSupply.DrawReducing(new List<LineGeoInfo> { linGeo }, Matrix3d.Identity);
        }
        SegInfo GetDuctLineSeg(Line centerLine,double width,double volume,double elevation)
        {
            var ductLine = new SegInfo();
            ductLine.l = centerLine;
            ductLine.ductSize = width.ToString()+"x100";
            ductLine.srcShrink = 0.0;
            ductLine.dstShrink = 0.0;
            ductLine.airVolume = volume;
            ductLine.elevation = elevation.ToString();
            return ductLine;
        }
        void AddDuctReducing(Line centerLine, double startWidth, double endWidth,bool isReturnAir) 
        {
            var lineInfo = ThDuctPortsFactory.CreateReducing(centerLine, startWidth, endWidth, false);
            AddAirReducing(lineInfo, isReturnAir);
        }
    }
}