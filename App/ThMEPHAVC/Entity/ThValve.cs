using ThMEPHVAC.Duct;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.IO;
using System.Linq;
using System;

namespace ThMEPHVAC.Entity
{
    public enum ValveGroupPosionType
    {
        Inlet = 1,
        Outlet = 2
    }

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
        public ValveGroupPosionType ValvePosionType { get; set; }
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
                // 为了保持文字方向朝上,
                // 若管道中心线处于三四象限（180，360]，则补偿阀的旋转角度，即旋转180度
                // 若换算到管道中心线的法向方向，则其处于二三象限（90,270]时需要补偿阀的旋转
                if ((RotationAngle > 0.5 * Math.PI && RotationAngle <= 1.5 * Math.PI))
                {
                    rotation = Matrix3d.Rotation(Math.PI, Vector3d.ZAxis, Point3d.Origin);
                }
            }
            else if (ValveVisibility == ThDuctUtils.CheckValveModelName() && ValvePosionType == ValveGroupPosionType.Inlet)
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
        public double ValveToFanSpacing { get; set; }
        public Point3d GroupInsertPoint { get; set; }
        public ValveGroupPosionType ValveGroupPosion { get; set; }
    }

    public class ThValveGroup
    {
        public List<ThValve> ValvesInGroup { get; set; }
        public ThValveGroupParameters Parameters { get; set; }

        public ThValveGroup(ThValveGroupParameters parameters, string fanlayer)
        {
            Parameters = parameters;
            ValvesInGroup = CreateValvesFromValveGroup(fanlayer);
        }

        //设置机房内管段阀组
        private List<ThValve> SetInnerValveGroup(string fanlayer, ValveGroupPosionType valveposiontype)
        {
            List<ThValve> valves = new List<ThValve>();

            var hole = CreateHole();
            hole.ValveOffsetFromCenter = -hole.Length;
            var firevalve = CreateFireValve(fanlayer);
            firevalve.ValveOffsetFromCenter = 0;
            var checkvalve = CreateCheckValve(fanlayer, valveposiontype);
            checkvalve.ValveOffsetFromCenter = firevalve.Length;

            valves.AddRange(new List<ThValve> { firevalve, hole });
            //空间足够，阀组中添加截止阀
            //若空间不够，截止阀在机房外设置
            if (valveposiontype== ValveGroupPosionType.Outlet && Parameters.ValveToFanSpacing > checkvalve.Length + firevalve.Length)
            {
                valves.Add(checkvalve);
            }
            return valves;
        }
        
        //设置机房外管段阀组
        private List<ThValve> SetOuterValveGroup(string fanlayer, ValveGroupPosionType valveposiontype)
        {
            List<ThValve> valves = new List<ThValve>();

            var silencer = CreateSilencer(fanlayer);
            var checkvalve = CreateCheckValve(fanlayer, valveposiontype);
            var hole = CreateHole();
            var firevalve = CreateFireValve(fanlayer);

            //正常情况下，空间足够
            if (Parameters.ValveToFanSpacing > checkvalve.Length + firevalve.Length)
            {
                silencer.ValveOffsetFromCenter = -silencer.Length - hole.Length;
                hole.ValveOffsetFromCenter = -hole.Length;
                firevalve.ValveOffsetFromCenter = 0;
                checkvalve.ValveOffsetFromCenter = firevalve.Length;
            }
            //机房外空间放不下防火阀加止回阀
            else
            {
                //机房外空间连一个止回阀都放不下
                if (Parameters.ValveToFanSpacing < checkvalve.Length)
                {
                    silencer.ValveOffsetFromCenter = -silencer.Length - firevalve.Length - checkvalve.Length - hole.Length;
                    checkvalve.ValveOffsetFromCenter = -checkvalve.Length - firevalve.Length - hole.Length;
                    firevalve.ValveOffsetFromCenter = -firevalve.Length - hole.Length;
                    hole.ValveOffsetFromCenter = -hole.Length;
                }
                //机房外空间可以放一个止回阀
                else
                {
                    silencer.ValveOffsetFromCenter = -silencer.Length - firevalve.Length - hole.Length;
                    firevalve.ValveOffsetFromCenter = -firevalve.Length - hole.Length;
                    hole.ValveOffsetFromCenter = -hole.Length;
                    checkvalve.ValveOffsetFromCenter = 0;
                }
            }

            switch (Parameters.FanScenario)
            {
                case "消防排烟":
                case "消防补风":
                case "消防加压送风":
                    valves.AddRange(new List<ThValve> { checkvalve, firevalve, hole });
                    break;
                default:
                    valves.AddRange(new List<ThValve> { checkvalve, firevalve, hole, silencer });
                    break;
            }

            return valves;
        }

        private List<ThValve> CreateValvesFromValveGroup(string fanlayer)
        {
            List<ThValve> valves = new List<ThValve>();

            var jsonReader = new ThDuctInOutMappingJsonReader();
            var innerRomDuctPosition = jsonReader.Mappings.First(d => d.WorkingScenario == Parameters.FanScenario).InnerRoomDuctType;
            
            //设置风机进风口段阀组
            if (Parameters.ValveGroupPosion == ValveGroupPosionType.Inlet)
            {
                //若当前工作场景中，风机进风口段对应机房内管段
                if (innerRomDuctPosition == "进风段")
                {
                    return SetInnerValveGroup(fanlayer, ValveGroupPosionType.Inlet);
                }
                //若当前工作场景中，风机出风口段对应机房内管段，即风机进风口段对应机房外管段
                else
                {
                    return SetOuterValveGroup(fanlayer, ValveGroupPosionType.Inlet);
                }
            }
            //出风段
            else
            {
                //若当前工作场景中，风机进风口段对应机房内管段
                if (innerRomDuctPosition == "进风段")
                {
                    return SetOuterValveGroup(fanlayer, ValveGroupPosionType.Outlet);
                }
                //若当前工作场景中，风机出风口段对应机房内管段，即风机进风口段对应机房外管段
                else
                {
                    return SetInnerValveGroup(fanlayer, ValveGroupPosionType.Outlet);
                }
            }
        }

        private ThValve CreateSilencer(string fanlayer)
        {
            return new ThValve()
            {
                Length = 1600,
                Width = Parameters.DuctWidth,
                RotationAngle = Parameters.RotationAngle,
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThHvacCommon.SILENCER_BLOCK_NAME,
                ValveBlockLayer = ThDuctUtils.SilencerLayerName(fanlayer),
                ValveVisibility = "",
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTH,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_LENGTH,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
        }

        private ThValve CreateHole()
        {
            return new ThValve()
            {
                Length = 200,
                Width = Parameters.DuctWidth + 100,
                RotationAngle = Parameters.RotationAngle,
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThHvacCommon.WALLHOLE_BLOCK_NAME,
                ValveBlockLayer = ThHvacCommon.WALLHOLE_LAYER,
                ValveVisibility = "",
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_LENGTH,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
        }

        private ThValve CreateFireValve(string fanlayer)
        {
            return new ThValve()
            {
                Length = 320,
                Width = Parameters.DuctWidth,
                RotationAngle = Parameters.RotationAngle,
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThDuctUtils.FireValveBlockName(),
                ValveBlockLayer = ThDuctUtils.ValveLayerName(fanlayer),
                ValveVisibility = ThDuctUtils.FireValveModelName(Parameters.FanScenario),
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
        }

        private ThValve CreateCheckValve(string fanlayer, ValveGroupPosionType valveposiontype)
        {
            return new ThValve()
            {
                Length = 200,
                Width = Parameters.DuctWidth,
                RotationAngle = Parameters.RotationAngle,
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThHvacCommon.AIRVALVE_BLOCK_NAME,
                ValveBlockLayer = ThDuctUtils.ValveLayerName(fanlayer),
                ValveVisibility = ThDuctUtils.CheckValveModelName(),
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
                ValvePosionType = valveposiontype,
            };
        }

        private ThValve CreateElectricValve(string fanlayer)
        {
            return new ThValve()
            {
                Length = 200,
                Width = Parameters.DuctWidth,
                RotationAngle = Parameters.RotationAngle,
                ValvePosition = Parameters.GroupInsertPoint,
                ValveBlockName = ThHvacCommon.AIRVALVE_BLOCK_NAME,
                ValveBlockLayer = ThDuctUtils.ValveLayerName(fanlayer),
                ValveVisibility = ThDuctUtils.ElectricValveModelName(),
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
        }
    }
}
