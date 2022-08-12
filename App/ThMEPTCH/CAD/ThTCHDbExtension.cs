using System;
using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;

namespace ThMEPTCH.CAD
{
    public static class ThTCHDbExtension
    {
        public static TArchDoor LoadDoorFromDb(this Database database, ObjectId tch, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var door = new TArchDoor()
                {
                    Id = (ulong)tch.Handle.Value,
                };
                var dxfData = GetDXFData(tch);
                foreach (TypedValue tv in dxfData.AsArray())
                {
                    switch ((DxfCode)tv.TypeCode)
                    {
                        case (DxfCode)10:
                            {
                                var pt = (Point3d)tv.Value;
                                door.BasePointX = pt.X;
                                door.BasePointY = pt.Y;
                                door.BasePointZ = pt.Z;
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
                        case (DxfCode)148:
                            {
                                door.Thickness = (double)tv.Value;
                            }
                            break;
                        default:
                            break;
                    }
                }

                door.TransformBy(matrix);
                return door;
            }
        }

        public static TArchWindow LoadWindowFromDb(this Database database, ObjectId tch, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var window = new TArchWindow()
                {
                    Id = (ulong)tch.Handle.Value,
                };
                var dxfData = GetDXFData(tch);
                foreach (TypedValue tv in dxfData.AsArray())
                {
                    switch ((DxfCode)tv.TypeCode)
                    {
                        case (DxfCode)10:
                            {
                                var pt = (Point3d)tv.Value;
                                window.BasePointX = pt.X;
                                window.BasePointY = pt.Y;
                                window.BasePointZ = pt.Z;
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

                window.TransformBy(matrix);
                return window;
            }
        }

        public static TArchWall LoadWallFromDb(this Database database, ObjectId tch, Matrix3d matrix)
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
                    Id = (ulong)tch.Handle.Value,
                    
                    // 几何信息
                    StartPointX = transCurve.StartPoint.X,
                    StartPointY = transCurve.StartPoint.Y,
                    StartPointZ = transCurve.StartPoint.Z,
                    EndPointX = transCurve.EndPoint.X,
                    EndPointY = transCurve.EndPoint.Y,
                    EndPointZ = transCurve.EndPoint.Z,
                    LeftWidth = acadObj.LeftWidth,
                    RightWidth = acadObj.RightWidth,
                    Height = acadObj.Height,
                    Elevtion = acadObj.Elevation,

                    // 圆弧信息
                    Bulge = Bulge(),
                    IsArc = IsArc(),
                };

                return wall;
            }
        }

        private static ResultBuffer GetDXFData(ObjectId tch)
        {
            InvokeTool.ads_name name = new InvokeTool.ads_name();
            InvokeTool.acdbGetAdsName(ref name, tch);

            ResultBuffer rb = new ResultBuffer();
            Interop.AttachUnmanagedObject(rb, InvokeTool.acdbEntGet(ref name), true);

            return rb;
        }

        private static Curve GetCurve(ObjectId tch)
        {
            return tch.GetObject(OpenMode.ForRead) as Curve;
        }
    }
}
