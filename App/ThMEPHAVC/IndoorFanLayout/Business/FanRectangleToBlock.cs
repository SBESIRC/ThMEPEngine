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
using ThMEPHVAC.ParameterService;

namespace ThMEPHVAC.IndoorFanLayout.Business
{
    class FanRectangleToBlock
    {
        ThMEPOriginTransformer _originTransformer;
        ThDuctPortsDrawService drawServiceAirSupply;
        ThDuctPortsDrawService drawServiceReturnAir;
        List<FanLoadBase> _allFanLoad;
        EnumFanType _enumFanType;
        public FanRectangleToBlock(List<FanLoadBase> allFanLoad, ThMEPOriginTransformer originTransformer) 
        {
            _allFanLoad = new List<FanLoadBase>();
            _originTransformer = originTransformer;
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
            ChangeBlockTextAttrAngle(connectorId, connectorAttrs.Select(c=>c.Key).ToList(), angle);

            //第一段风管,回风管或风箱
            var startPoint = center - dir.MultiplyBy(length / 2);

            //根据连接件计算变径
            var coonectorWidth = fanLoad.FanWidth;
            var centerWidth = coonectorWidth - 134.0 - 15 * 2;
            if (IndoorFanParameter.Instance.LayoutModel.AirReturnType == EnumAirReturnType.AirReturnPipe)
            {
                //回风管
                var SectionEnd = startPoint + dir.MultiplyBy(startSectionLength);
                AddAirDuct(new Line(startPoint, SectionEnd), layoutRect.Width, 10.00, 3000.00, true);
                //连接件变径
                var startReducingSPoint = SectionEnd;
                var startReducingEPoint = SectionEnd + dir.MultiplyBy(150);
                AddDuctReducing(new Line(startReducingSPoint, startReducingEPoint), layoutRect.Width, centerWidth, false);
            }
            else 
            {
                //回风箱
                var boxLength = startSectionLength + 150;
                var pt1 = startPoint - otherDir.MultiplyBy(centerWidth / 2);
                var pt2 = startPoint + otherDir.MultiplyBy(centerWidth / 2);
                var pt1End = pt1 + dir.MultiplyBy(boxLength);
                var pt2End = pt2 + dir.MultiplyBy(boxLength);
                var poly = new Polyline();
                poly.Layer = IndoorFanBlockServices.FanBoxLayerName;
                poly.Closed = false;
                poly.AddVertexAt(0, pt1End.ToPoint2D(), 0, 0, 0);
                poly.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
                poly.AddVertexAt(0, pt2.ToPoint2D(), 0, 0, 0);
                poly.AddVertexAt(0, pt2End.ToPoint2D(), 0, 0, 0);
                acdb.ModelSpace.Add(poly);
            }
            if (!IndoorFanParameter.Instance.LayoutModel.CreateBlastPipe)
                return;
            //送风管和风机的变径
            var centerPoint1 = connectorPoint;
            var centerPoint2 = connectorPoint + dir.MultiplyBy(150);
            AddDuctReducing(new Line(centerPoint1, centerPoint2), centerWidth, layoutRect.Width, true);
            //第二段风管 送风管
            var secondStart = centerPoint2; //startPoint + dir.MultiplyBy(startSectionLength + secondSectionLenght);
            var endPoint = center + dir.MultiplyBy(length / 2);
            AddAirDuct(new Line(secondStart, endPoint), layoutRect.Width, 10.0, 3000.00, false);

            AddAirPort(acdb,fanLoad,layoutRect,angle);
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
            double startSectionLength = 100+ fanLoad.ReturnAirSizeLength + 50;
            double secondSectionLenght = fanLoad.FanLength;//vrfFan.;
            var center = layoutRect.CenterPoint;
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
            if (IndoorFanParameter.Instance.LayoutModel.AirReturnType == EnumAirReturnType.AirReturnPipe)
            {
                //回风管
                var SectionEnd = startPoint + dir.MultiplyBy(startSectionLength);
                AddAirDuct(new Line(startPoint, SectionEnd), layoutRect.Width, 10.00, 3000.00, true);
            }
            else
            {
                //回风箱
                var boxLength = startSectionLength + 150;
                var pt1 = startPoint - otherDir.MultiplyBy(layoutRect.Width / 2);
                var pt2 = startPoint + otherDir.MultiplyBy(layoutRect.Width / 2);
                var pt1End = pt1 + dir.MultiplyBy(boxLength);
                var pt2End = pt2 + dir.MultiplyBy(boxLength);
                var poly = new Polyline();
                poly.Layer = IndoorFanBlockServices.FanBoxLayerName;
                poly.Closed = false;
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
            ChangeBlockTextAttrAngle(connectorId, new List<string> { "设备编号" }, angle+Math.PI/2);
            ChangeBlockTextAttrAngle(connectorId, new List<string> { "设备电量", "制冷量/制热量" }, angle);
            if (!IndoorFanParameter.Instance.LayoutModel.CreateBlastPipe)
                return;
            //第二段风管 送风管
            var secondStart = startPoint + dir.MultiplyBy(startSectionLength + secondSectionLenght);
            var endPoint = center + dir.MultiplyBy(length / 2);
            AddAirDuct(new Line(secondStart, endPoint), layoutRect.Width, 10.0, 3000.00, false);
            AddAirPort(acdb, fanLoad, layoutRect, angle);
        }
        private void AddVRFFourSide(AcadDatabase acdb, FanLayoutRect layoutRect) 
        {
            //VRF室内机，四面出风型
            string fanName = layoutRect.FanLayoutName;
            var fanLoad = _allFanLoad.Where(c => c.FanNumber == fanName).FirstOrDefault();
            if (fanLoad == null)
                return;
            var center = _originTransformer.Reset(layoutRect.CenterPoint);
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

        
        private void AddAirPort(AcadDatabase acdb, FanLoadBase fanLoad, FanLayoutRect layoutRect,double angle) 
        {
            //添加出风口
            double ventWidth = fanLoad.GetCoilFanVentSize(layoutRect.InnerVentRects.Count);
            double ventVolume = fanLoad.FanAirVolumeDouble / layoutRect.InnerVentRects.Count;
            string ventType = fanLoad.AirSupplyOutletType;
            string attName = ventType.Contains("圆") ? "圆形风口直径" : "方形散流器喉部宽度";
            foreach (var vent in layoutRect.InnerVentRects)
            {
                var ventCenter = vent.CenterPoint;
                ventCenter = _originTransformer.Reset(ventCenter);
                var ventAttrs = new Dictionary<string, string>();
                ventAttrs.Add("风量", string.Format("{0}m3/h", ventVolume));
                var ventDynAttrs = new Dictionary<string, object>();
                ventDynAttrs.Add("风口类型", ventType);
                ventDynAttrs.Add(attName, ventWidth);

                AddCoilAirPort(acdb, ventCenter, angle, ventAttrs, ventDynAttrs);
            }
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