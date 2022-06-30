using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System.Linq;
using ThParkingStall.Core.MPartitionLayout;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using ThCADCore.NTS;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.Geometry;
using static ThParkingStall.Core.MPartitionLayout.MDebugTools;
using System.IO;
using System;
using System.Text;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThMEPArchitecture.PartitionLayout
{
    public class MultiProcessTestCommand
    {
        [CommandMethod("TIANHUACAD", "THMPTest", CommandFlags.Modal)]
        public void THMPTest()
        {
            List<LineString> walls = new List<LineString>();
            List<LineSegment> inilanes = new List<LineSegment>();
            List<Polygon> obs = new List<Polygon>();
            List<Polygon> box = new List<Polygon>();
            Polygon boundary = new Polygon(new LinearRing(new Coordinate[0]));
            ReadDataToMParkingPartitionPro(ref walls, ref inilanes, ref obs, ref boundary, ref box);
            //write_to_txt(walls, inilanes, obs, boundary, box);
            //List<LineString> twalls = new List<LineString>();
            //List<LineSegment> tinilanes = new List<LineSegment>();
            //List<Polygon> tobs = new List<Polygon>();
            //List<Polygon> tbox = new List<Polygon>();
            //Polygon tboundary = new Polygon(new LinearRing(new Coordinate[0]));
            //string[] paras = read_to_string();
            //read_from_string(paras, ref twalls, ref tinilanes, ref tobs, ref tboundary, ref tbox);
            MParkingPartitionPro mParkingPartitionPro = ConvertToMParkingPartitionPro(walls, inilanes, obs, boundary, box);
            mParkingPartitionPro.GenerateParkingSpaces();
            write_test(mParkingPartitionPro);

        }
        [CommandMethod("TIANHUACAD", "THMPTestControlGroup", CommandFlags.Modal)]
        public void THMPTestControlGroup()
        {
            List<Line> lines = new List<Line>();
            List<Polyline> plys = new List<Polyline>();
            read_entities(ref lines, ref plys);
            var polys = plys.Select(e => e.ToNTSPolygon()).ToList();
            var line = new LineString(new Coordinate[] { new Coordinate(lines[0].StartPoint.X,lines[0].StartPoint.Y)
            ,new Coordinate(lines[0].EndPoint.X,lines[0].EndPoint.Y)});
            var g = polys[0].Intersection(line);
        }
        private void read_entities(ref List<Line> lines, ref List<Polyline> plys)
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var entities = new List<Entity>();
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                lines = result.Value
                   .GetObjectIds()
                   .Select(o => adb.Element<Entity>(o))
                   .Where(e => e is Line)
                   .Select(e => e.Clone() as Line)
                   .ToList();
                plys = result.Value
                    .GetObjectIds()
                    .Select(o => adb.Element<Entity>(o))
                    .Where(e => e is Polyline)
                    .Select(e => e.Clone() as Polyline)
                    .ToList();
                return;
            }
        }

        private MParkingPartitionPro ConvertToMParkingPartitionPro(List<LineString> walls,
            List<LineSegment> inilanes, List<Polygon> obs, Polygon boundary, List<Polygon> box)
        {
            MParkingPartitionPro mParkingPartitionPro = new MParkingPartitionPro(
              walls, inilanes, obs, boundary);
            mParkingPartitionPro.OutBoundary = boundary;
            mParkingPartitionPro.BuildingBoxes = box;
            mParkingPartitionPro.ObstaclesSpatialIndex = new MNTSSpatialIndex(obs);
            return mParkingPartitionPro;
        }

        private void ReadDataToMParkingPartitionPro(ref List<LineString> walls,
            ref List<LineSegment> inilanes, ref List<Polygon> obs, ref Polygon boundary, ref List<Polygon> boxes)
        {
            var lines = new List<Line>();
            var plys = new List<Polyline>();
            read_entities(ref lines, ref plys);
            var db_walls = plys.Where(e => e.Layer == "walls")
                .Where(e => e is Polyline).ToList();
            db_walls.AddRange(lines.Where(e => e.Layer == "walls")
                .Select(e => GeoUtilities.CreatePolyFromLine(e)));
            var obstacles = plys.Where(e => e.Layer == "obstacles")
                .Where(e => e is Polyline)
                .Select(e => e.ToNTSPolygon());
            var db_lanes = lines.Where(e => e.Layer == "lanes")
                .Where(e => e is Line);
            var db_boxes = plys.Where(e => e.Layer == "boxes")
                .Where(e => e is Polyline)
                .Select(e => e.ToNTSPolygon());
            var bound = GeoUtilities.JoinCurves(db_walls.ToList(), db_lanes.ToList())[0].ToNTSPolygon();
            walls = db_walls.Select(e => new LineString(e.Vertices().Cast<Point3d>().Select(f => new Coordinate(f.X, f.Y)).ToArray())).ToList();
            inilanes = db_lanes.Select(e => e.ToNTSLineSegment()).ToList();
            obs = obstacles.ToList();
            boundary = bound;
            boxes = db_boxes.ToList();
            return;
        }

        public static void DisplayMParkingPartitionPros(MParkingPartitionPro mParkingPartitionPro,
            string carLayerName = "AI-停车位", string columnLayerName = "AI-柱子", string laneLayerName = "AI-车道中心线", int carindex = 30, int columncolor = -1, int lanecolor = 20)
        {
            var cars = new List<InfoCar>();
            foreach (var mcar in mParkingPartitionPro.Cars)
            {
                InfoCar infoCar = new InfoCar(mcar.Polyline.ToDbPolylines()[0],
                    new Point3d(mcar.Point.X, mcar.Point.Y, 0), new Vector3d(mcar.Vector.X, mcar.Vector.Y, 0));
                infoCar.CarLayoutMode = mcar.CarLayoutMode;
                cars.Add(infoCar);
            }
            LayoutOutput.CarLayerName = carLayerName;
            LayoutOutput.ColumnLayerName = columnLayerName;
            LayoutOutput.LaneLayerName = laneLayerName;
            LayoutOutput.LaneDisplayColorIndex = lanecolor;
            LayoutOutput.InitializeLayer();
            var vertcar = LayoutOutput.VCar;
            var vertbackcar = LayoutOutput.VBackCar;
            var pcar = LayoutOutput.PCar;
            LayoutOutput layout = new LayoutOutput(cars,
                mParkingPartitionPro.Pillars.Select(e => e.ToDbPolylines()[0]).ToList(),
                mParkingPartitionPro.OutputLanes.Select(e => e.ToDbLine()).ToList());
            layout.DisplayColumns();
            layout.DisplayCars();
            layout.DisplayLanes();

            //foreach (var lane in mParkingPartitionPro.OutEnsuredLanes)
            //{
            //    var line = lane.ToDbLine();
            //    line.ColorIndex = 92;
            //    line.AddToCurrentSpace();
            //}
            //foreach (var lane in mParkingPartitionPro.OutUnsuredLanes)
            //{
            //    var line = lane.ToDbLine();
            //    line.ColorIndex = 241;
            //    line.AddToCurrentSpace();
            //}
        }

        public static void write_test(MParkingPartitionPro mParkingPartitionPro)
        {
            foreach (var e in mParkingPartitionPro.OutputLanes)
            {
                var line = new Line(new Point3d(e.P0.X, e.P0.Y, 0),
                    new Point3d(e.P1.X, e.P1.Y, 0));
                line.AddToCurrentSpace();
            }
            List<Polyline> cars = new List<Polyline>();
            foreach (var car in mParkingPartitionPro.Cars)
            {
                var pl = GeoUtilities.CreatePolyFromPoints(car.Polyline.Coordinates.Select(e =>
                  new Point3d(e.X, e.Y, 0)).ToArray());
                cars.Add(pl);
            }
            cars.Select(e => { e.ColorIndex = 30; return e; }).AddToCurrentSpace();
            List<Polyline> pillars = new List<Polyline>();
            foreach (var pillar in mParkingPartitionPro.Pillars)
            {
                var pl = GeoUtilities.CreatePolyFromPoints(pillar.Coordinates.Select(e =>
                  new Point3d(e.X, e.Y, 0)).ToArray());
                pillars.Add(pl);
            }
            pillars.AddToCurrentSpace();
        }

        private void write_to_txt(List<LineString> walls,
            List<LineSegment> inilanes, List<Polygon> obs, Polygon boundary, List<Polygon> box)
        {
            string swalls = "walls:";
            foreach (var wall in walls)
            {
                foreach (var co in wall.Coordinates)
                    swalls += co.X.ToString() + "," + co.Y.ToString() + ",";
                swalls += ";";
            }
            string sinilanes = "lanes:";
            foreach (var lane in inilanes)
            {
                sinilanes += lane.P0.X.ToString() + "," + lane.P0.Y.ToString() + "," +
                    lane.P1.X.ToString() + "," + lane.P1.Y.ToString() + ";";
            }
            string sobs = "obs:";
            foreach (var ob in obs)
            {
                foreach (var co in ob.Coordinates)
                    sobs += co.X.ToString() + "," + co.Y.ToString() + ",";
                sobs += ";";
            }
            string sbox = "box:";
            foreach (var bo in box)
            {
                foreach (var co in bo.Coordinates)
                    sbox += co.X.ToString() + "," + co.Y.ToString() + ",";
                sbox += ";";
            }
            string sbound = "bound:";
            foreach (var co in boundary.Coordinates)
                sbound += co.X.ToString() + "," + co.Y.ToString() + ",";

            string dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            FileStream fs = new FileStream(dir + "\\geodatas.txt", FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(swalls);
            sw.WriteLine(sinilanes);
            sw.WriteLine(sobs);
            sw.WriteLine(sbox);
            sw.WriteLine(sbound);
            sw.Close();
            fs.Close();
        }
        private string[] read_to_string()
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string path = dir + "\\geodatas.txt";
            StreamReader sr = new StreamReader(path, Encoding.Default);
            string content;
            List<string> strs = new List<string>();
            while ((content = sr.ReadLine()) != null)
            {
                strs.Add(content);
            }
            return strs.ToArray();
        }
        private static void read_from_string(string[] parameters, ref List<LineString> walls,
            ref List<LineSegment> inilanes, ref List<Polygon> obs, ref Polygon boundary, ref List<Polygon> boxes)
        {
            foreach (var content in parameters.ToList())
            {
                var list = content.Split(':', ';').Where(e => e.Length > 0).ToList();
                list.RemoveAt(0);
                if (content.Contains("walls"))
                {
                    foreach (var wall in list)
                    {
                        var ss = wall.Split(',').Where(e => e.Length > 0).ToList();
                        List<Coordinate> coords = new List<Coordinate>();
                        for (int i = 0; i < ss.Count - 1; i += 2)
                        {
                            var co = new Coordinate(double.Parse(ss[i]), double.Parse(ss[i + 1]));
                            coords.Add(co);
                        }
                        walls.Add(new LineString(coords.ToArray()));
                    }
                }
                else if (content.Contains("lanes"))
                {
                    foreach (var lane in list)
                    {
                        var ss = lane.Split(',').Where(e => e.Length > 0).ToList();
                        List<Coordinate> coords = new List<Coordinate>();
                        for (int i = 0; i < ss.Count - 3; i += 4)
                        {
                            var c0 = new Coordinate(double.Parse(ss[i]), double.Parse(ss[i + 1]));
                            var c1 = new Coordinate(double.Parse(ss[i + 2]), double.Parse(ss[i + 3]));
                            inilanes.Add(new LineSegment(c0, c1));
                        }
                    }
                }
                else if (content.Contains("obs"))
                {
                    foreach (var ob in list)
                    {
                        var ss = ob.Split(',').Where(e => e.Length > 0).ToList();
                        List<Coordinate> coords = new List<Coordinate>();
                        for (int i = 0; i < ss.Count - 1; i += 2)
                        {
                            var co = new Coordinate(double.Parse(ss[i]), double.Parse(ss[i + 1]));
                            coords.Add(co);
                        }
                        obs.Add(new Polygon(new LinearRing(coords.ToArray())));
                    }
                }
                else if (content.Contains("box"))
                {
                    foreach (var bo in list)
                    {
                        var ss = bo.Split(',').Where(e => e.Length > 0).ToList();
                        List<Coordinate> coords = new List<Coordinate>();
                        for (int i = 0; i < ss.Count - 1; i += 2)
                        {
                            var co = new Coordinate(double.Parse(ss[i]), double.Parse(ss[i + 1]));
                            coords.Add(co);
                        }
                        boxes.Add(new Polygon(new LinearRing(coords.ToArray())));
                    }
                }
                else if (content.Contains("bound"))
                {
                    foreach (var bd in list)
                    {
                        var ss = bd.Split(',').Where(e => e.Length > 0).ToList();
                        List<Coordinate> coords = new List<Coordinate>();
                        for (int i = 0; i < ss.Count - 1; i += 2)
                        {
                            var co = new Coordinate(double.Parse(ss[i]), double.Parse(ss[i + 1]));
                            coords.Add(co);
                        }
                        boundary = new Polygon(new LinearRing(coords.ToArray()));
                    }
                }
            }
        }
    }
}
