using Autodesk.AutoCAD.DatabaseServices;
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
                    FanLayoutDetailed fanDetailed = null;
                    switch (enumFanType)
                    {
                        case EnumFanType.FanCoilUnitFourControls:
                        case EnumFanType.FanCoilUnitTwoControls:
                            fanDetailed = GetAddCoilFanData(item);
                            break;
                        case EnumFanType.VRFConditioninConduit:
                            fanDetailed = GetAddVRFImpellerFanData(acdb, item);
                            break;
                        case EnumFanType.VRFConditioninFourSides:
                            fanDetailed = GetAddVRFFourSideData(item);
                            break;
                    }
                    if (null == fanDetailed)
                        continue;
                    FanLayoutDetailedToBlock(acdb, fanDetailed, enumFanType);
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
                    FanLayoutDetailedToBlock(acdb, layoutRect, enumFanType);
                }
            }
        }
        private void FanLayoutDetailedToBlock(AcadDatabase acdb, FanLayoutDetailed layoutRect, EnumFanType enumFanType) 
        {
            if (layoutRect == null)
                return;
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
                return;
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
                            var SectionEnd = layoutRect.FanPoint - fanDir.MultiplyBy(fanLength + IndoorFanDistance.ReducingLength);
                            if (null != _originTransformer)
                                SectionEnd = _originTransformer.Reset(SectionEnd);
                            AddAirDuct(new Line(startPoint, SectionEnd), layoutRect.Width, 10.00, _fanPipLevel, true);
                            //连接件变径
                            var startReducingSPoint = SectionEnd;
                            var startReducingEPoint = SectionEnd + layoutRect.FanDirection.MultiplyBy(IndoorFanDistance.ReducingLength);
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
                        return;
                    var centerPoint1 = layoutRect.FanPoint;
                    if (null != _originTransformer)
                        centerPoint1 = _originTransformer.Reset(centerPoint1);
                    var centerPoint2 = centerPoint1 + fanDir.MultiplyBy(IndoorFanDistance.ReducingLength);
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
                        return;
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
                            var SectionEnd = layoutRect.FanPoint - fanDir.MultiplyBy(fanLength + IndoorFanDistance.ReducingLength);
                            if (null != _originTransformer)
                                SectionEnd = _originTransformer.Reset(SectionEnd);
                            AddAirDuct(new Line(startPoint, SectionEnd), layoutRect.Width, 10.00, _fanPipLevel, true);
                            //连接件变径
                            var startReducingSPoint = SectionEnd;
                            var startReducingEPoint = SectionEnd + layoutRect.FanDirection.MultiplyBy(IndoorFanDistance.ReducingLength);
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
        private FanLayoutDetailed GetAddCoilFanData(FanLayoutRect layoutRect)
        {
            FanLayoutDetailed coilFanDetailed = null;
            string fanName = layoutRect.FanLayoutName;
            var fanLoad = _allFanLoad.Where(c => c.FanNumber == fanName).FirstOrDefault();
            if (fanLoad == null)
                return coilFanDetailed;
            
            var fanDisToStart = IndoorFanDistance.CoilFanDistanceToStart(fanLoad, _indoorFanLayout.AirReturnType);
            var returnCenter = IndoorFanDistance.CoilReturnVentCenterDisToFan(fanLoad, _indoorFanLayout.AirReturnType);
            var dir = layoutRect.FanDirection;
            var length = layoutRect.Length;
            var startPoint = layoutRect.CenterPoint - dir.MultiplyBy(length/2);
            var endPoint = layoutRect.CenterPoint + dir.MultiplyBy(length / 2);
            coilFanDetailed = new FanLayoutDetailed(startPoint, endPoint, layoutRect.Width, dir);
            coilFanDetailed.FanLayoutName = layoutRect.FanLayoutName;
            coilFanDetailed.FanPoint = startPoint + layoutRect.FanDirection.MultiplyBy(fanDisToStart);
            coilFanDetailed.FanReturnVentCenterPoint = coilFanDetailed.FanPoint - dir.MultiplyBy(returnCenter);
            coilFanDetailed.HaveReturnVent = true;
            if (!_indoorFanLayout.CreateBlastPipe)
                return coilFanDetailed;
            coilFanDetailed.FanInnerVents.AddRange(layoutRect.InnerVentRects.Select(c => c.CenterPoint).ToList());
            return coilFanDetailed;
        }
        private FanLayoutDetailed GetAddVRFImpellerFanData(AcadDatabase acdb, FanLayoutRect layoutRect)
        {
            //VRF是室内机（管道机）
            FanLayoutDetailed coilFanDetailed = null;
            string fanName = layoutRect.FanLayoutName;
            var fanLoad = _allFanLoad.Where(c => c.FanNumber == fanName).FirstOrDefault();
            if (fanLoad == null)
                return coilFanDetailed;

            var fanDisToStart = IndoorFanDistance.VRFFanDistanceToStart(fanLoad);
            var returnCenter = IndoorFanDistance.VRFReturnVentCenterDisToFan(fanLoad);
            var dir = layoutRect.FanDirection;
            var length = layoutRect.Length;
            var startPoint = layoutRect.CenterPoint - dir.MultiplyBy(length / 2);
            var endPoint = layoutRect.CenterPoint + dir.MultiplyBy(length / 2);
            coilFanDetailed = new FanLayoutDetailed(startPoint, endPoint, layoutRect.Width, dir);
            coilFanDetailed.FanLayoutName = layoutRect.FanLayoutName;
            coilFanDetailed.FanPoint = startPoint + layoutRect.FanDirection.MultiplyBy(fanDisToStart);
            coilFanDetailed.FanReturnVentCenterPoint = coilFanDetailed.FanPoint - dir.MultiplyBy(returnCenter);
            coilFanDetailed.HaveReturnVent = true;
            if (!_indoorFanLayout.CreateBlastPipe)
                return coilFanDetailed;
            coilFanDetailed.FanInnerVents.AddRange(layoutRect.InnerVentRects.Select(c => c.CenterPoint).ToList());
            return coilFanDetailed;
        }
        private FanLayoutDetailed GetAddVRFFourSideData(FanLayoutRect layoutRect)
        {
            //VRF室内机，四面出风型
            FanLayoutDetailed coilFanDetailed = null;
            string fanName = layoutRect.FanLayoutName;
            var fanLoad = _allFanLoad.Where(c => c.FanNumber == fanName).FirstOrDefault();
            if (fanLoad == null)
                return coilFanDetailed;
            var fanDisToStart = IndoorFanDistance.CoilFanDistanceToStart(fanLoad, _indoorFanLayout.AirReturnType);
            var returnCenter = IndoorFanDistance.CoilReturnVentCenterDisToFan(fanLoad, _indoorFanLayout.AirReturnType);
            var dir = layoutRect.FanDirection;
            var length = layoutRect.Length;
            var startPoint = layoutRect.CenterPoint - dir.MultiplyBy(length / 2);
            var endPoint = layoutRect.CenterPoint + dir.MultiplyBy(length / 2);
            coilFanDetailed = new FanLayoutDetailed(startPoint, endPoint, layoutRect.Width, dir);
            coilFanDetailed.FanLayoutName = layoutRect.FanLayoutName;
            coilFanDetailed.FanPoint = layoutRect.CenterPoint;
            coilFanDetailed.HaveReturnVent = false;
            return coilFanDetailed;
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