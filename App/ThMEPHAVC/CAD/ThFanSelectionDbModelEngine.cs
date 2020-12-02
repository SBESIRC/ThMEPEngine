using System;
using System.Linq;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.FanSelection;
using TianHua.Publics.BaseCode;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using AcHelper;

namespace ThMEPHAVC.CAD
{
    public class FanOpening
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsCircleOpening { get; set; }
        //public Point3d OpeningCenter { get; set; }
    }
    public class ThFanSelectionDbModelEngine
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


        public ThFanSelectionDbModelEngine(ObjectId objectId)
        {
            Model = objectId;
            Data = new ThBlockReferenceData(objectId);
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
            string blockname = Data.EffectiveName;
            if (blockname.Contains("轴流"))
            {
                var redius = Data.CustomProperties
                    .Cast<DynamicBlockReferenceProperty>()
                    .Where(d => d.PropertyName == "风机直径")
                    .First().Value.NullToDouble();
                return new FanOpening()
                {
                    IsCircleOpening = true,
                    Width = redius,
                    Height = redius
                };
            }
            else
            {
                if (blockname.Contains("直进") || blockname.Contains("侧进"))
                {
                    return new FanOpening()
                    {
                        IsCircleOpening = false,
                        Height = 0,
                        Width = Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == "进风口横A")
                        .First().Value.NullToDouble()
                    };
                }
                else
                {
                    return new FanOpening()
                    {
                        IsCircleOpening = false,

                        Height = Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == "进风口竖B")
                        .First().Value.NullToDouble(),

                        Width = Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == "进风口横A")
                        .First().Value.NullToDouble()
                    };
                }
            }
        }

        private FanOpening GetFanOutlet()
        {
            string blockname = Data.EffectiveName;
            if (blockname.Contains("轴流"))
            {
                var redius = Data.CustomProperties
                    .Cast<DynamicBlockReferenceProperty>()
                    .Where(d => d.PropertyName == "风机直径")
                    .First().Value.NullToDouble();
                return new FanOpening()
                {
                    IsCircleOpening = true,
                    Width = redius,
                    Height = redius
                };
            }
            else
            {
                if (blockname.Contains("直出"))
                {
                    return new FanOpening()
                    {
                        IsCircleOpening = false,
                        Height = 0,
                        Width = Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == "出风口横a")
                        .First().Value.NullToDouble()
                    };
                }
                else
                {
                    return new FanOpening()
                    {
                        IsCircleOpening = false,

                        Height = Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == "出风口竖B")
                        .First().Value.NullToDouble(),

                        Width = Data.CustomProperties
                        .Cast<DynamicBlockReferenceProperty>()
                        .Where(d => d.PropertyName == "出风口横A")
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
                Point3d inletposition = CreatePointFromProperty("进风口 X", "进风口 Y");
                return inletposition.TransformBy(ocs2Wcs);
            }
            else
            {
                Point3d inletposition = CreatePointFromProperty("设备基点 X", "设备基点 Y");
                return inletposition.TransformBy(ocs2Wcs);
            }
        }

        private Point3d GetFanOutletBasePoint()
        {
            string blockname = Data.EffectiveName;
            Matrix3d ocs2Wcs = Matrix3d.Displacement(Data.Position.GetAsVector());
            Point3d inletposition = CreatePointFromProperty("出风口 X", "出风口 Y");
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
