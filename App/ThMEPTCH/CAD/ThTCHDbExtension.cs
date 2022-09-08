using System;
using Linq2Acad;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;
using ThMEPTCH.PropertyServices.EntityProperties;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.CAD
{
    public static partial class ThTCHDbExtension
    {
        public static TArchDoor LoadDoorFromDb(this Database database, ObjectId tch, Matrix3d matrix, int uid)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var door = new TArchDoor()
                {
                    Id = (ulong)ThMEPDbUniqueIdService.UniqueId(tch, uid, matrix),
                };
                var dxfData = GetDXFData(tch);
                foreach (TypedValue tv in dxfData.AsArray())
                {
                    switch ((DxfCode)tv.TypeCode)
                    {
                        case (DxfCode)1:
                            {
                                door.OperationType = GetOperationType(tv);
                            }
                            break;
                        case (DxfCode)10:
                            {
                                door.BasePoint = (Point3d)tv.Value;
                            }
                            break;
                        case (DxfCode)40:
                            {
                                door.Width = (double)tv.Value;
                            }
                            break;
                        case (DxfCode)41:
                            {
                                door.Height = (double)tv.Value;
                            }
                            break;
                        case (DxfCode)50:
                            {
                                door.Rotation = (double)tv.Value;
                            }
                            break;
                        case (DxfCode)70:
                            {
                                door.Swing = GetSwing(tv);
                            }
                            break;
                        case (DxfCode)148:
                            {
                                door.Thickness = (double)tv.Value;
                            }
                            break;
                        default:
                            break;
                    }
                }

                Override(door, tch);

                door.TransformBy(matrix);
                return door;
            }
        }

        private static void Override(TArchDoor door, ObjectId tch)
        {
            // 从XData获取门底高
            var service = new TCHDoorPropertyService();
            var property = service.GetProperty(tch) as TCHDoorProperty;
            door.Height = property.Height;
        }

        public static TArchWindow LoadWindowFromDb(this Database database, ObjectId tch, Matrix3d matrix, int uid)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var window = new TArchWindow()
                {
                    Id = (ulong)ThMEPDbUniqueIdService.UniqueId(tch, uid, matrix),
                };
                var dxfData = GetDXFData(tch);
                foreach (TypedValue tv in dxfData.AsArray())
                {
                    switch ((DxfCode)tv.TypeCode)
                    {
                        case (DxfCode)1:
                            {
                                window.WindowType = GetWindowType(tv);
                            }
                            break;
                        case (DxfCode)10:
                            {
                                window.BasePoint = (Point3d)tv.Value;
                            }
                            break;
                        case (DxfCode)40:
                            {
                                window.Width = (double)tv.Value;
                            }
                            break;
                        case (DxfCode)41:
                            {
                                window.Height = (double)tv.Value;
                            }
                            break;
                        case (DxfCode)50:
                            {
                                window.Rotation = (double)tv.Value;
                            }
                            break;
                        case (DxfCode)148:
                            {
                                window.Thickness = (double)tv.Value;
                            }
                            break;
                        default:
                            break;
                    }
                }

                Override(window, tch);

                window.TransformBy(matrix);
                return window;
            }
        }

        private static void Override(TArchWindow window, ObjectId tch)
        {
            // 从XData获取窗高和底高
            var service = new TCHWindowPropertyService();
            var property = service.GetProperty(tch) as TCHWindowProperty;
            window.Height = property.Height;
            window.BasePoint = new Point3d(window.BasePoint.X, window.BasePoint.Y, property.BottomElevation);
        }

        public static TArchWall LoadWallFromDb(this Database database, ObjectId tch, Matrix3d matrix, int uid)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var curve = GetCurve(tch);
                dynamic acadObj = curve.AcadObject;
                bool IsArc()
                {
                    if (acadObj.IsArc is string v)
                    {
                        return v == "弧墙";
                    }
                    return false;
                }
                bool IsLinear()
                {
                    if (acadObj.IsArc is string v)
                    {
                        return v == "直墙";
                    }
                    return false;
                }
                Curve GetTransformedCurve()
                {
                    if (IsLinear())
                    {
                        var line = new Line(curve.StartPoint, curve.EndPoint);
                        return line.GetTransformedCopy(matrix) as Curve;
                    }
                    else if (IsArc())
                    {
                        // https://forums.autodesk.com/t5/net/arc-3-points/td-p/9424441
                        var geArc = new CircularArc3d(
                            curve.StartPoint,
                            curve.GetMidpoint(),
                            curve.EndPoint);
                        if (Curve.CreateFromGeCurve(geArc) is Arc arc)
                        {
                            return arc.GetTransformedCopy(matrix) as Curve;
                        }
                    }
                    throw new NotSupportedException();
                }
                var transCurve = GetTransformedCurve();
                double Bulge()
                {
                    if (transCurve is Line)
                    {
                        return 0.0;
                    }
                    else if (transCurve is Arc arc)
                    {
                        return arc.BulgeFromCurve(false);
                    }
                    throw new NotSupportedException();
                }
                var wall = new TArchWall
                {
                    // 标识信息
                    Id = (ulong)ThMEPDbUniqueIdService.UniqueId(tch, uid, matrix),

                    // 几何信息
                    StartPoint = transCurve.StartPoint,
                    EndPoint = transCurve.EndPoint,
                    LeftWidth = acadObj.LeftWidth,
                    RightWidth = acadObj.RightWidth,
                    Height = acadObj.Height,

                    // 圆弧信息
                    Bulge = Bulge(),
                    IsArc = IsArc(),
                };

                Override(wall, tch);

                return wall;
            }
        }

        private static void Override(TArchWall wall, ObjectId tch)
        {
            // 从XData获取墙高和底高
            var service = new TCHWallPropertyService();
            var property = service.GetProperty(tch) as TCHWallProperty;
            wall.Height = property.Height;
            wall.EndPoint = new Point3d(wall.EndPoint.X, wall.EndPoint.Y, property.BottomElevation);
            wall.StartPoint = new Point3d(wall.StartPoint.X, wall.StartPoint.Y, property.BottomElevation);
        }

        private static ResultBuffer GetDXFData(ObjectId tch)
        {
            InvokeTool.ads_name name = new InvokeTool.ads_name();
            InvokeTool.acdbGetAdsName(ref name, tch);

            ResultBuffer rb = new ResultBuffer();
            Interop.AttachUnmanagedObject(rb, InvokeTool.acdbEntGet(ref name), true);

            return rb;
        }
        private static DoorTypeOperationEnum GetOperationType(TypedValue tv)
        {
            var name = Convert.ToString(tv.Value);
            if (ThTCHDbCommon.DoorTypeOperationMapping.ContainsKey(name))
            {
                return ThTCHDbCommon.DoorTypeOperationMapping[name];
            }
            return DoorTypeOperationEnum.SWING;
        }

        private static WindowTypeEnum GetWindowType(TypedValue tv)
        {
            var name = Convert.ToString(tv.Value);
            if (ThTCHDbCommon.WindowTypeMapping.ContainsKey(name))
            {
                return ThTCHDbCommon.WindowTypeMapping[name];
            }
            return WindowTypeEnum.Window;
        }

        private static SwingEnum GetSwing(TypedValue tv)
        {
            return (SwingEnum)Convert.ToUInt16(tv.Value);
        }

        private static Curve GetCurve(ObjectId tch)
        {
            return tch.GetObject(OpenMode.ForRead) as Curve;
        }
    }
}
