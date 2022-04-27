using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThMEPHVAC.IO;
using ThMEPHVAC.Duct;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThValve
    {
        public Point3d ValvePosition { get; set; }
        public string ValveBlockName { get; set; }
        public string ValveBlockLayer { get; set; }
        public string ValveVisibility { get; set; }
        public double Width { get; set; }
        public double Length { get; set; }
        public string VisibilityPropertyName { get; set; }
        public string WidthPropertyName { get; set; }
        public string LengthPropertyName { get; set; }
        /// <summary>
        /// 风管中心线法向量旋转角度（顺时针）
        /// </summary>
        public double RotationAngle { get; set; }
        public double ValveOffsetFromCenter { get; set; }
        public bool IsInlet;
        public double TextRotateAngle 
        {
            get
            {
                return SetTextAngle();
            }
        }
        private double SetTextAngle()
        {
            switch (ValveVisibility)
            {
                case ThHvacCommon.BLOCK_VALVE_VISIBILITY_CHECK:
                    if (RotationAngle > 0 && RotationAngle <= Math.PI)
                    {
                        return Math.PI;
                    }
                    else
                    {
                        return 0;
                    }
                default:
                    return 0;
            }
        }
        public Matrix3d Marix
        {
            get
            {
                return GetValveMatrix();
            }
        }
        private Matrix3d GetValveMatrix()
        {
            Point3d holeinsertpoint = Point3d.Origin.TransformBy(Matrix3d.Displacement(new Vector3d(0.5 * Width, ValveOffsetFromCenter, 0)));
            Point3d valvecenterpoint = Point3d.Origin.TransformBy(Matrix3d.Displacement(new Vector3d(0.5 * Width, -0.5 * Length, 0)));

            Matrix3d rotation = Matrix3d.Identity;
            if (ValveBlockName == ThDuctUtils.FireValveBlockName())
            {
                // 为了保持防火阀文字方向朝上,
                // 若管道中心线处于三四象限（180，360]，则补偿阀的旋转角度，即旋转180度
                // 若换算到管道中心线的法向方向，则其处于二三象限（90,270]时需要补偿阀的旋转
                if ((RotationAngle > 0.5 * Math.PI && RotationAngle <= 1.5 * Math.PI))
                {
                    rotation = Matrix3d.Rotation(Math.PI, Vector3d.ZAxis, Point3d.Origin);
                }
            }
            else if (ValveBlockName == ThDuctUtils.SilencerBlockName())
            {
                // 为了保持消声器文字方向朝上,
                // 若管道中心线处于二三象限（90，270]，则补偿阀的旋转角度，即旋转180度
                // 若换算到管道中心线的法向方向，则其处于一四象限（0,180]时需要补偿阀的旋转
                if ((RotationAngle > 0 && RotationAngle <= Math.PI))
                {
                    rotation = Matrix3d.Rotation(Math.PI, Vector3d.ZAxis, Point3d.Origin);
                }
            }
            else if (ValveVisibility == ThDuctUtils.CheckValveModelName() && IsInlet)
            {
                rotation = Matrix3d.Rotation(Math.PI, Vector3d.ZAxis, Point3d.Origin);
            }
            // 为了在WCS中正确放置图块，图块需要完成转换：
            //  1. 将图块的中心点移到原点
            //  2. 依据图块的实际放置角度，考虑是否需要旋转一个补偿角度(180)，使文字方向转正
            //  3. 补偿第一步平移变换
            //  4. 基于图块插入点将图块旋转到管线的角度
            //  5. 将图块平移到管线上指定位置
            var marix = Matrix3d.Identity
                .PreMultiplyBy(Matrix3d.Displacement(valvecenterpoint.GetAsVector().Negate()))
                .PreMultiplyBy(rotation)
                .PreMultiplyBy(Matrix3d.Displacement(valvecenterpoint.GetAsVector()))
                .PreMultiplyBy(Matrix3d.Rotation(RotationAngle, Vector3d.ZAxis, holeinsertpoint))
                .PreMultiplyBy(Matrix3d.Displacement(holeinsertpoint.GetVectorTo(ValvePosition)));

            return marix;
        }
    }

    public class ThValveGroupParameters
    {
        public double RotationAngle { get; set; }
        public string FanScenario { get; set; }
        public double DuctWidth { get; set; }
        public double DuctHeight { get; set; }
        public double ValveToFanSpacing { get; set; }
        public Point3d GroupInsertPoint { get; set; }
    }

    public class ThValveGroup
    {
        public List<ThValve> ValvesInGroup { get; set; }
        public ThValveGroupParameters Parameters { get; set; }
        private bool isIn;
        private bool isExhaust;
        private ThDuctPortsDrawService service;

        public ThValveGroup(ThValveGroupParameters parameters, ThDuctPortsDrawService service, bool isIn, bool isExhaust)
        {
            this.isIn = isIn;
            Parameters = parameters;
            this.isExhaust = isExhaust;
            this.service = service;
        }
        public void CreateValvesFromValveGroup()
        {
            List<ThValve> valves = new List<ThValve>();
            var jsonReader = new ThDuctInOutMappingJsonReader();
            var innerRomDuctPosition = jsonReader.Mappings.First(d => d.WorkingScenario == Parameters.FanScenario).InnerRoomDuctType;
            if (isIn)
                ValvesInGroup = SetInnerValveGroup();
            else
                ValvesInGroup = SetOuterValveGroup();
        }
        public void SetFireHoleGroup()
        {
            // 进风口不布置止回阀
            var valves = new List<ThValve>();
            var hole = CreateHole();
            var firevalve = CreateFireValve();
            if (Parameters.ValveToFanSpacing >  firevalve.Length)
            {
                firevalve.ValveOffsetFromCenter = 0;
                hole.ValveOffsetFromCenter = -hole.Length;
            }
            //若空间不够，防火阀移至洞外
            else
            {
                hole.ValveOffsetFromCenter = -hole.Length;
                firevalve.ValveOffsetFromCenter = -firevalve.Length - hole.Length;
            }
            valves.AddRange(new List<ThValve> { firevalve, hole });
            ValvesInGroup = valves;
        }
        //设置机房内管段阀组
        private List<ThValve> SetInnerValveGroup()
        {
            // 进风口不布置止回阀
            List<ThValve> valves = new List<ThValve>();
            var silencer = CreateSilencer();// 如果是非送风场景，进风口和room相连需要布置消声器
            var haveSilencer = !(Parameters.FanScenario == "消防排烟" || Parameters.FanScenario == "消防补风" || 
                                 Parameters.FanScenario == "消防加压送风");
            if (!isExhaust || !haveSilencer)
                silencer.Length = 0;
            if (Parameters.ValveToFanSpacing > silencer.Length)
            {
                silencer.ValveOffsetFromCenter = 0;
            }
            //若空间不够，防火阀移至洞外
            else
            {
                silencer.ValveOffsetFromCenter = silencer.Length;
            }
            if (isExhaust && haveSilencer)
                valves.AddRange(new List<ThValve> { silencer });
            else
                valves.AddRange(new List<ThValve> { });
            return valves;
        }
        
        //设置机房外管段阀组
        private List<ThValve> SetOuterValveGroup()
        {
            List<ThValve> valves = new List<ThValve>();

            var silencer = CreateSilencer();
            var checkvalve = CreateCheckValve();
            var haveSilencer = !(Parameters.FanScenario == "消防排烟" || Parameters.FanScenario == "消防补风" || Parameters.FanScenario == "消防加压送风");
            if (isExhaust || !haveSilencer)// 如果是非送风场景，出风口不布置消声器
                silencer.Length = 0;
            //正常情况下，空间足够
            if (Parameters.ValveToFanSpacing > checkvalve.Length + silencer.Length)
            {
                silencer.ValveOffsetFromCenter = 0;
                checkvalve.ValveOffsetFromCenter = isExhaust ? (silencer.Length) :
                                                   (-(checkvalve.Length + silencer.Length));// 翻转180°
            }
            //机房外空间放不下防火阀加止回阀加消音器
            else
            {
                //若能放得下一个止回阀加防火阀，则将消音器移至洞外
                if (Parameters.ValveToFanSpacing > checkvalve.Length)
                {
                    silencer.ValveOffsetFromCenter = -silencer.Length;
                    checkvalve.ValveOffsetFromCenter = 0;
                }
                //放不下止回阀加防火阀
                //把防火阀移至洞外
                else
                {
                    silencer.ValveOffsetFromCenter = -silencer.Length;
                    checkvalve.ValveOffsetFromCenter = silencer.ValveOffsetFromCenter - checkvalve.Length;
                }
            }
            valves.AddRange(new List<ThValve> { checkvalve});
            if (!isExhaust && haveSilencer)
                valves.Add(silencer);
            return valves;
        }
        
        private ThValve CreateSilencer()
        {
            return new ThValve()
            {
                Length = 1000,
                Width = Parameters.DuctWidth + 200,
                RotationAngle = Parameters.RotationAngle,
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThHvacCommon.SILENCER_BLOCK_NAME,
                ValveBlockLayer = service.silencerLayer,
                ValveVisibility = ThDuctUtils.SilencerModelName(),
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTH,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_LENGTH,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
        }

        private ThValve CreateHole()
        {
            return new ThValve()
            {
                Length = Parameters.DuctHeight + 100,
                Width = Parameters.DuctWidth + 100,
                RotationAngle = Parameters.RotationAngle + Math.PI,
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = service.holeName,
                ValveBlockLayer = service.holeLayer,
                ValveVisibility = "",
                WidthPropertyName = ThHvacCommon.AI_HOLE_WIDTH,
                LengthPropertyName = ThHvacCommon.AI_HOLE_LENGTH,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
        }

        private ThValve CreateFireValve()
        {
            return new ThValve()
            {
                Length = 320,
                Width = Parameters.DuctWidth,
                RotationAngle = Parameters.RotationAngle,
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThDuctUtils.FireValveBlockName(),
                ValveBlockLayer = service.airValveLayer,
                ValveVisibility = ThDuctUtils.FireValveModelName(Parameters.FanScenario),
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
        }

        private ThValve CreateCheckValve()
        {
            var angle = Parameters.RotationAngle;
            return new ThValve()
            {
                Length = 200,
                Width = Parameters.DuctWidth,
                RotationAngle = isExhaust ? angle : angle + Math.PI,
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThHvacCommon.AIRVALVE_BLOCK_NAME,
                ValveBlockLayer = service.airValveLayer,
                ValveVisibility = ThDuctUtils.CheckValveModelName(),
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_CHECK_VALVE_HEIGHT,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
        }
    }
}
