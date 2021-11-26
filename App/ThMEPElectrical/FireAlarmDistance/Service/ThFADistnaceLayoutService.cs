using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;

using AcHelper;
using Linq2Acad;
using CLI;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Command;

using ThMEPElectrical.FireAlarm;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.FireAlarmDistance.Data;


namespace ThMEPElectrical.FireAlarmDistance.Service
{
    public class ThFADistnaceLayoutService
    {
        public static FeatureCollection Export2NTSFeatures(string geojson)
        {
            var serializer = GeoJsonSerializer.Create();
            using (var stringReader = new StringReader(geojson))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                return serializer.Deserialize<FeatureCollection>(jsonReader);
            }
        }

        public static List<Point3d> ConvertGeom(FeatureCollection features)
        {
            var pts = new List<Point3d>();

            foreach (var f in features)
            {
                var coordinates = f.Geometry.Coordinates;
                var pt = new Point3d(coordinates[0].X, coordinates[0].Y, 0);
                pts.Add(pt);
            }

            return pts;
        }

        public static List<KeyValuePair<Point3d, Vector3d>> FindOutputPtsDir(List<Point3d> pts, List<Polyline> RoomList)
        {
            var ptDir = new List<KeyValuePair<Point3d, Vector3d>>();

            for (int i = 0; i < pts.Count; i++)
            {
                var pt = pts[i];
                var dir = new Vector3d(0, 1, 0);
                var room = RoomList.Where(x => x.ContainsOrOnBoundary(pt)).FirstOrDefault();

                if (room != null)
                {
                    dir = FindPtDir(pt, room);
                }
                ptDir.Add(new KeyValuePair<Point3d, Vector3d>(pt, dir));
            }

            return ptDir;
        }

        private static Vector3d FindPtDir(Point3d pt, Polyline room)
        {
            var dir = new Vector3d(0, 1, 0);
            var closeDist = 100000.0;
            var closeWallIdx = -1;
            var closeLine = new Line();

            for (int i = 0; i < room.NumberOfVertices; i++)
            {
                var l = new Line(room.GetPoint3dAt(i % room.NumberOfVertices), room.GetPoint3dAt((i + 1) % room.NumberOfVertices));
                var dist = l.GetDistToPoint(pt, false);
                if (dist <= closeDist)
                {
                    closeDist = dist;
                    closeWallIdx = i;
                    closeLine = l;
                }
            }
            if (closeWallIdx >= 0)
            {
                var z = Vector3d.ZAxis;
                if (room.IsCCW() == false)
                {
                    z = -z;
                }
                dir = (closeLine.EndPoint - closeLine.StartPoint).RotateBy(90 * Math.PI / 180, z).GetNormal();
            }
            return dir;
        }

        public static void GetLayoutParaForNoUI(ref ThAFASPlacementMountModeMgd mode, ref double stepDistance)
        {
            var isWallPa = Active.Editor.GetInteger("\n吊装（0）壁装（1）");
            if (isWallPa.Status != PromptStatus.OK)
            {
                return;
            }
            mode = isWallPa.Value == 1 ? ThAFASPlacementMountModeMgd.Wall : ThAFASPlacementMountModeMgd.Ceiling;


            var stepDistanceP = Active.Editor.GetDouble("\n步距：");
            if (stepDistanceP.Status != PromptStatus.OK)
            {
                return;
            }
            stepDistance = stepDistanceP.Value;

        }
    }
}
