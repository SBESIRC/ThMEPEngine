using System;
using System.Linq;
using NFox.Cad;
using DotNetARX;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThPlatform3D.StructPlane.Service
{
    internal class ThBeamMarkXDataService
    {
        public const string RegAppName_BeamMark = "TH_BEAMARK";
        private const string AreaPrefix = "Area:";
        private const string MoveDirPrefix = "MoveDir:";

        public static void WriteBeamArea(ObjectId id, Point3dCollection originArea,Vector3d textMoveDir)
        {
            var tvs = new TypedValueList();
            originArea.OfType<Point3d>().ForEach(p =>
            {
                tvs.Add((int)DxfCode.ExtendedDataAsciiString, AreaPrefix + p.X + "," + p.Y + "," + p.Z);
            });
            tvs.Add((int)DxfCode.ExtendedDataAsciiString, MoveDirPrefix + textMoveDir.X + "," + textMoveDir.Y + "," + textMoveDir.Z);
            id.AddXData(RegAppName_BeamMark, tvs);
        }

        public static Tuple<Point3dCollection, Vector3d> ReadBeamArea(ObjectId beamMark)
        {
            var moveDir = new Vector3d();
            var pts = new Point3dCollection();
            var tvs = beamMark.GetXData(RegAppName_BeamMark);
            if (tvs != null)
            {                
                tvs.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString)
                    .Select(o => o.Value.ToString())
                    .ForEach(o =>
                    {
                        if (o.StartsWith(AreaPrefix))
                        {
                            var cord = o.Substring(AreaPrefix.Length);
                            var values = cord.Split(',');
                            pts.Add(new Point3d(double.Parse(values[0]), double.Parse(values[1]), double.Parse(values[2])));
                        }
                        else if (o.StartsWith(MoveDirPrefix))
                        {
                            var dir = o.Substring(MoveDirPrefix.Length);
                            var values = dir.Split(',');
                            moveDir = new Vector3d(double.Parse(values[0]), double.Parse(values[1]), double.Parse(values[2]));
                        }
                        else
                        {
                            //
                        }
                    });
            }
            return Tuple.Create(pts, moveDir);
        }
    }
}
