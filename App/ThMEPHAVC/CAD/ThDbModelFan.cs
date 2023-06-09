﻿using System;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using ThCADExtension;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.Model;
using TianHua.FanSelection;
using TianHua.Publics.BaseCode;

namespace ThMEPHVAC.CAD
{
    public class FanOpening
    {
        public double Angle { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsCircleOpening { get; set; }
    }
    public class ThDbModelFan
    {
        public string Name { get; set; }
        public ObjectId Model { get; set; }
        public ThBlockReferenceData Data { get; set; }
        
        public FanOpening FanInlet;
        public FanOpening FanOutlet;
        public double airVolume;
        public double fanInWidth;
        public double fanOutWidth;
        public double lowAirVolume;
        public Point3d FanBasePoint;
        public Point3d FanInletBasePoint;
        public Point3d FanOutletBasePoint;
        public string scenario;
        public string IntakeForm;
        public string strAirVolume;
        public bool isExhaust;
        public ThDbModelFan(ObjectId FanObjectId)
        {
            using (var db = AcadDatabase.Active())// 立即显示重绘效果
            {
                Model = FanObjectId;
                Data = new ThBlockReferenceData(FanObjectId);
                airVolume = GetFanVolume();
                if (airVolume < 0)
                    return;
                lowAirVolume = GetLowFanVolume();
                var obj = FanObjectId.GetDBObject();
                if (obj is BlockReference reference)
                    Name = reference.GetEffectiveName();
                FanBasePoint = GetFanBasePoint();
                FanInletBasePoint = GetFanInletBasePoint();
                FanOutletBasePoint = GetFanOutletBasePoint();
                scenario = GetFanScenario();
                IntakeForm = GetIntakeForm();
                FanOutlet = GetFanOutlet();
                FanInlet = GetFanInlet();
                isExhaust = !(scenario.Contains("补") || scenario.Contains("送"));
                var isAxis = (Name.Contains("轴流风机"));
                ThDuctPortsDrawService.GetFanDynBlockProperity(Data, isAxis, out fanInWidth, out fanOutWidth);
            } 
        }
        private string GetIntakeForm()
        {
            if (Model.IsRawHTFCModel())
            {
                // 从离心风机块名解析处进风形式
                return Data.EffectiveName.Replace(" ", "").Split('、')[1];
            }
            else
            {
                return "直进直出";
            }
        }
        private double GetFanVolume()
        {
            var service = new ThFanModelDataService();
            var volums = service.CalcAirVolume(Data.ObjId);
            if (volums.Count == 1)
            {
                strAirVolume = volums[0].ToString();
                return volums[0];
            }
            else if (volums.Count == 2)
            {
                strAirVolume = volums[0].ToString() + "/" + volums[1].ToString();
                return volums[1];
            }
            else
                return -1;
        }
        private double GetLowFanVolume()
        {
            var fanvolumevaluestring = Data.Attributes[ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_VOLUME];
            var volumegroup = fanvolumevaluestring.Replace(" ", "").Replace("风量：", "").Replace("cmh", "").Split('/');
            if (volumegroup.Count() < 2)
            {
                return 0;
            }
            else
            {
                return volumegroup.Min(s=>s.NullToDouble());
            }
        }
        private FanOpening GetFanInlet()
        {
            double angle2Property = Convert.ToDouble(Data.CustomProperties
                    .Cast<DynamicBlockReferenceProperty>()
                    .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_ANGLE2)
                    .First().Value) * 180 / Math.PI;
            double blockrotation = Data.Rotation * 180 / Math.PI;
            double totalrotation = angle2Property + blockrotation > 360 ? angle2Property + blockrotation - 360 : angle2Property + blockrotation;
            if (Model.IsRawAXIALModel())
            {
                var redius = Convert.ToDouble(Data.CustomProperties
                    .Cast<DynamicBlockReferenceProperty>()
                    .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_DIAMETER)
                    .First().Value);
                return new FanOpening()
                {
                    IsCircleOpening = true,
                    Width = redius,
                    Height = redius,
                    Angle = totalrotation <= 270 ? totalrotation + 90 : totalrotation - 180 - 90
                };
            }
            else
            {
                string blockname = Data.EffectiveName;
                if (blockname.Contains("直进") || blockname.Contains("侧进"))
                {
                    FanOpening fanopening = new FanOpening() 
                    {
                        IsCircleOpening = false,
                        Height = 0,
                        Width = Convert.ToDouble(Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_HORIZONTAL)
                        .First().Value),
                    };
                    if (blockname.Contains("直进"))
                    {
                        fanopening.Angle = totalrotation < 180 ? totalrotation + 180 : totalrotation - 180;
                    }
                    else
                    {
                        string BlockFliped = Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_ROTATE2)
                        .First().Value.ToString();
                        var realrotationangle = totalrotation == 270 ? totalrotation + 90 : totalrotation - 180 - 90;
                        if (BlockFliped == "1")
                        {
                            fanopening.Angle = realrotationangle < 180 ? realrotationangle + 180 : realrotationangle - 180;
                        }
                        else
                        {
                            fanopening.Angle = realrotationangle;
                        }
                    }
                    return fanopening;
                }
                else
                {
                    return new FanOpening()
                    {
                        IsCircleOpening = false,
                        Angle = totalrotation,
                        Height = Convert.ToDouble(Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_HORIZONTAL)
                        .First().Value),

                        Width = Convert.ToDouble(Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_VERTICAL)
                        .First().Value),
                    };
                }
            }
        }
        private FanOpening GetFanOutlet()
        {
            double angle2Property = Convert.ToDouble(Data.CustomProperties
                    .Cast<DynamicBlockReferenceProperty>()
                    .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_ANGLE2)
                    .First().Value) * 180 / Math.PI;
            double blockrotation = Data.Rotation * 180 / Math.PI;
            double totalrotation = angle2Property + blockrotation > 360 ? angle2Property + blockrotation - 360 : angle2Property + blockrotation;
            if (Model.IsRawAXIALModel())
            {
                var redius = Convert.ToDouble(Data.CustomProperties
                    .Cast<DynamicBlockReferenceProperty>()
                    .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_DIAMETER)
                    .First().Value);
                return new FanOpening()
                {
                    IsCircleOpening = true,
                    Width = redius,
                    Height = redius,
                    Angle = totalrotation < 90 ? totalrotation + 270 : totalrotation - 90
                };
            }
            else
            {
                string blockname = Data.EffectiveName;
                if (blockname.Contains("直出"))
                {
                    return new FanOpening()
                    {
                        IsCircleOpening = false,
                        Height = 0,
                        Angle = totalrotation,
                        Width = Convert.ToDouble(Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_HORIZONTAL)
                        .First().Value),
                    };
                }
                else
                {
                    return new FanOpening()
                    {
                        IsCircleOpening = false,
                        Angle = totalrotation,
                        Height = Convert.ToDouble(Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_HORIZONTAL)
                        .First().Value),

                        Width = Convert.ToDouble(Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_VERTICAL)
                        .First().Value),
                    };
                }
            }
        }
        private Point3d GetFanBasePoint()
        {
            Matrix3d ocs2Wcs = Data.BlockTransform;
            Point3d axialinletposition = CreatePointFromProperty(
                        ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X,
                        ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_Y);
            return axialinletposition.TransformBy(ocs2Wcs);
        }
        private Point3d GetFanInletBasePoint()
        {
            string blockname = Data.EffectiveName;
            Matrix3d ocs2Wcs = Data.BlockTransform;
            if (blockname.Contains("直进") || blockname.Contains("侧进"))
            {
                Point3d inletposition = CreatePointFromProperty(
                    ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_X,
                    ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_Y);
                return inletposition.TransformBy(ocs2Wcs);
            }
            else
            {
                if (Model.IsRawAXIALModel())
                {
                    Point3d axialinletposition = CreatePointFromProperty(
                        ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X,
                        ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_Y);
                    return axialinletposition.TransformBy(ocs2Wcs);
                }
                else
                {
                    Point3d inletposition = CreatePointFromProperty(
                        ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X,
                        ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_Y);
                    return inletposition.TransformBy(ocs2Wcs);
                }
            }
        }

        private Point3d GetFanOutletBasePoint()
        {
            Matrix3d ocs2Wcs = Data.BlockTransform;
            if (Model.IsRawAXIALModel())
            {
                Point3d axialinletposition = CreatePointFromProperty(
                    ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_X,
                    ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_Y);
                return axialinletposition.TransformBy(ocs2Wcs);
            }
            else
            {
                Point3d inletposition = CreatePointFromProperty(
                    ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_X,
                    ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_Y);
                return inletposition.TransformBy(ocs2Wcs);
            }
        }
        private Point3d CreatePointFromProperty(string xname,string yname)
        {
            double inletX = Convert.ToDouble(Data.CustomProperties
                .Cast<DynamicBlockReferenceProperty>()
                .Where(d => d.PropertyName == xname)
                .First().Value);

            double inletY = Convert.ToDouble(Data.CustomProperties
                .Cast<DynamicBlockReferenceProperty>()
                .Where(d => d.PropertyName == yname)
                .First().Value);

            return new Point3d(inletX, inletY, 0);
        }

        private string GetFanScenario()
        {
            var scenario = Model.GetModelScenario();
            if (string.IsNullOrEmpty(scenario))
            {
                return "平时送风";
            }
            return scenario;
        }
    }
}
