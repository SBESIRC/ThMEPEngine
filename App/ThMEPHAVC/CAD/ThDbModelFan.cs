using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThCADExtension;
using ThMEPEngineCore.Service.Hvac;
using TianHua.FanSelection;
using TianHua.Publics.BaseCode;

namespace ThMEPHVAC.CAD
{
    public class FanOpening
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsCircleOpening { get; set; }
        public double Angle { get; set; }
    }
    public class ThDbModelFan
    {
        public ObjectId Model { get; set; }
        public ThBlockReferenceData Data { get; set; }

        public string IntakeForm
        {
            get
            {
                return GetIntakeForm();
            }
        }

        public double FanVolume
        {
            get
            {
                return GetFanVolume();
            }
        }

        public FanOpening FanInlet
        {
            get
            {
                return GetFanInlet();
            }
        }

        public Point3d FanInletBasePoint
        {
            get
            {
                return GetFanInletBasePoint();
            }
        }

        public Point3d FanOutletBasePoint
        {
            get
            {
                return GetFanOutletBasePoint();
            }
        }

        public FanOpening FanOutlet
        {
            get
            {
                return GetFanOutlet();
            }
        }

        public DBObjectCollection InAndOutLines { get; set; }

        public ThDbModelFan(ObjectId FanObjectId, DBObjectCollection inandoutlines)
        {
            Model = FanObjectId;
            InAndOutLines = inandoutlines;
            Data = new ThBlockReferenceData(FanObjectId);
        }

        private string GetIntakeForm()
        {
            if (Model.IsHTFCModel())
            {
                // 从离心风机块名解析处进风形式
                var blocknamesplit = Data.EffectiveName.Replace(" ", "").Split('、');
                return blocknamesplit[1];
            }
            else
            {
                return "直进直出";
            }
        }

        private double GetFanVolume()
        {
            return Data.Attributes[ThFanSelectionCommon.BLOCK_ATTRIBUTE_FAN_VOLUME].NullToDouble();
        }

        private FanOpening GetFanInlet()
        {
            double angle2Property = Data.CustomProperties
                    .Cast<DynamicBlockReferenceProperty>()
                    .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_ANGLE2)
                    .First().Value.NullToDouble() * 180 / Math.PI;
            if (Model.IsAXIALModel())
            {
                var redius = Data.CustomProperties
                    .Cast<DynamicBlockReferenceProperty>()
                    .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_DIAMETER)
                    .First().Value.NullToDouble();
                return new FanOpening()
                {
                    IsCircleOpening = true,
                    Width = redius,
                    Height = redius,
                    Angle = angle2Property <= 270 ? angle2Property + 90 : angle2Property - 180 - 90
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
                        Width = Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_HORIZONTAL)
                        .First().Value.NullToDouble()
                    };
                    if (blockname.Contains("直进"))
                    {
                        fanopening.Angle = angle2Property <= 180 ? angle2Property + 180 : angle2Property - 180;
                    }
                    else
                    {
                        fanopening.Angle = angle2Property <= 270 ? angle2Property + 90 : angle2Property - 180 - 90;
                    }
                    return fanopening;
                }
                else
                {
                    return new FanOpening()
                    {
                        IsCircleOpening = false,
                        Angle = angle2Property,
                        Height = Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_VERTICAL)
                        .First().Value.NullToDouble(),

                        Width = Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_HORIZONTAL)
                        .First().Value.NullToDouble()
                    };
                }
            }
        }
        private FanOpening GetFanOutlet()
        {
            double angle2Property = Data.CustomProperties
                    .Cast<DynamicBlockReferenceProperty>()
                    .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_ANGLE2)
                    .First().Value.NullToDouble() * 180 / Math.PI;
            if (Model.IsAXIALModel())
            {
                var redius = Data.CustomProperties
                    .Cast<DynamicBlockReferenceProperty>()
                    .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_DIAMETER)
                    .First().Value.NullToDouble();
                return new FanOpening()
                {
                    IsCircleOpening = true,
                    Width = redius,
                    Height = redius,
                    Angle = angle2Property < 90 ? angle2Property + 270 : angle2Property - 90
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
                        //Angle = angle2Property <= 180 ? angle2Property + 180 : angle2Property - 180,
                        Angle = angle2Property,
                        Width = Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_HORIZONTAL)
                        .First().Value.NullToDouble()
                    };
                }
                else
                {
                    return new FanOpening()
                    {
                        IsCircleOpening = false,
                        Angle = angle2Property,
                        Height = Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_VERTICAL)
                        .First().Value.NullToDouble(),

                        Width = Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_HORIZONTAL)
                        .First().Value.NullToDouble()
                    };
                }
            }
        }

        private Point3d GetFanInletBasePoint()
        {
            string blockname = Data.EffectiveName;
            Matrix3d ocs2Wcs = Matrix3d.Displacement(Data.Position.GetAsVector());
            if (blockname.Contains("直进") || blockname.Contains("侧进"))
            {
                Point3d inletposition = CreatePointFromProperty(
                    ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_X,
                    ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_Y);
                return inletposition.TransformBy(ocs2Wcs);
            }
            else
            {
                if (Model.IsAXIALModel())
                {
                    Point3d axialinletposition = CreatePointFromProperty(
                        ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X,
                        ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_Y);
                    return axialinletposition.TransformBy(ocs2Wcs);
                }
                Point3d inletposition = CreatePointFromProperty(
                    ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X,
                    ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_Y);
                return inletposition.TransformBy(ocs2Wcs);
            }
        }

        private Point3d GetFanOutletBasePoint()
        {
            Matrix3d ocs2Wcs = Matrix3d.Displacement(Data.Position.GetAsVector());
            if (Model.IsAXIALModel())
            {
                Point3d axialinletposition = CreatePointFromProperty(
                    ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_X,
                    ThHvacCommon.BLOCK_DYNMAIC_PROPERTY_BASE_POINT_Y);
                return axialinletposition.TransformBy(ocs2Wcs);
            }

            string blockname = Data.EffectiveName;
            Point3d inletposition = CreatePointFromProperty(
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_X,
                ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_Y);
            return inletposition.TransformBy(ocs2Wcs);
        }
        private Point3d CreatePointFromProperty(string xname,string yname)
        {
            double inletX = Data.CustomProperties
                .Cast<DynamicBlockReferenceProperty>()
                .Where(d => d.PropertyName == xname)
                .First().Value.NullToDouble();

            double inletY = Data.CustomProperties
                .Cast<DynamicBlockReferenceProperty>()
                .Where(d => d.PropertyName == yname)
                .First().Value.NullToDouble();

            return new Point3d(inletX, inletY, 0);
        }
    }
}
