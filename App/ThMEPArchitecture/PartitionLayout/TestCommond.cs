using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using ThMEPArchitecture.ParkingStallArrangement.PreProcess;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.ObliqueMPartitionLayout;
using ThMEPArchitecture.MultiProcess;
using Converter = ThMEPArchitecture.MultiProcess.Converter;
using ThMEPArchitecture.ViewModel;
using ThParkingStall.Core.OInterProcess;
using System.IO;

namespace ThMEPArchitecture.PartitionLayout
{
    public partial class TestCommond
    {
        [CommandMethod("TIANHUACAD", "ThParkPartitionTest", CommandFlags.Modal)]
        public void ThParkPartitionTest()
        {
            Execute();
        }
        [CommandMethod("TIANHUACAD", "ThOParkPartitionTest", CommandFlags.Modal)]
        public void ThOParkPartitionTest()
        {
            try
            {
                if (true)
                {
                    ParameterViewModel = new ParkingStallArrangementViewModel();
                    ParameterStock.Set(ParameterViewModel);
                    ObliqueExecute();
                }
                else
                {
                    _ObliqueExecute();
                }
            }
            catch (System.Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }
        public static ParkingStallArrangementViewModel ParameterViewModel { get; set; }
        private void _ObliqueExecute()
        {
            var walls = new List<Polyline>();
            var iniLanes = new List<Line>();
            var obstacles = new List<Polyline>();
            var buildingBox = new List<Polyline>();
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var objs = result.Value
                   .GetObjectIds()
                   .Select(o => adb.Element<Entity>(o))
                   .Where(o => o is Line || o is Polyline)
                   .Select(o => o.Clone() as Entity)
                   .ToList();
                foreach (var o in objs)
                {
                    if (o.Layer == "inilanes") iniLanes.Add((Line)o);
                    else if (o.Layer == "walls")
                    {
                        if (o is Polyline) walls.Add((Polyline)o);
                        else if (o is Line) walls.Add(GeoUtilities.CreatePolyFromLine((Line)o));
                    }
                    else if (o.Layer == "obstacles")
                    {
                        if (o is Polyline) obstacles.Add((Polyline)o);
                    }
                }
            }
            var boundary = GeoUtilities.JoinCurves(walls, iniLanes)[0];
            boundary.Closed = true;

            var polygon_bound = new Polygon(new LinearRing(boundary.Vertices().Cast<Point3d>().Select(p => new Coordinate(p.X, p.Y)).ToArray()));
            ObliqueMPartition mParkingPartitionPro = new ObliqueMPartition(
                walls.Select(e => new LineString(e.Vertices().Cast<Point3d>().Select(p => new Coordinate(p.X, p.Y)).ToArray())).ToList(),
                iniLanes.Select(e => new LineSegment(new Coordinate(e.StartPoint.X, e.StartPoint.Y), new Coordinate(e.EndPoint.X, e.EndPoint.Y))).ToList(),
                obstacles.Select(e => new Polygon(new LinearRing(e.Vertices().Cast<Point3d>().Select(p => new Coordinate(p.X, p.Y)).ToArray()))).ToList(),
                polygon_bound);
            mParkingPartitionPro.OutputLanes = new List<LineSegment>();
            mParkingPartitionPro.OutBoundary = polygon_bound;
            mParkingPartitionPro.BuildingBoxes = new List<Polygon>();
            //mParkingPartitionPro.ObstaclesSpatialIndex = new MNTSSpatialIndex(obs);
            mParkingPartitionPro.ObstaclesSpatialIndex = new MNTSSpatialIndex(mParkingPartitionPro.Obstacles);
            mParkingPartitionPro.Process(true);
            MultiProcessTestCommand.DisplayMParkingPartitionPros(mParkingPartitionPro.ConvertToMParkingPartitionPro());
            mParkingPartitionPro.IniLanes.Select(e => e.Line.ToDbLine()).AddToCurrentSpace();
        }
        private void ObliqueExecute()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var block = InputData.SelectBlock(acdb);//提取地库对象
                var layoutData = new LayoutData();
                layoutData.TryInit(block,true);
                var dataWraper = Converter.GetDataWraper(layoutData, ParameterViewModel, false);
                OInterParameter.Init(dataWraper);
                var oSubAreas = OInterParameter.GetSubAreas();

                foreach(var oSubArea in oSubAreas)
                {
                    try
                    {
                        ObliqueMPartition mParkingPartitionPro = new ObliqueMPartition(oSubArea.Walls, oSubArea.VaildLanes, oSubArea.Buildings, oSubArea.Area);
                        mParkingPartitionPro.OutputLanes = new List<LineSegment>();
                        mParkingPartitionPro.OutBoundary = oSubArea.Area;
                        mParkingPartitionPro.BuildingBoxes = new List<Polygon>();
                        mParkingPartitionPro.ObstaclesSpatialIndex = new MNTSSpatialIndex(mParkingPartitionPro.Obstacles);
#if DEBUG
                        var s = MDebugTools.AnalysisPolygon(mParkingPartitionPro.Boundary);
                        string dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                        FileStream fs = new FileStream(dir + "\\bound.txt", FileMode.Create, FileAccess.Write);
                        StreamWriter sw = new StreamWriter(fs);
                        sw.WriteLine(s);
                        sw.Close();
                        fs.Close();
#endif
                        mParkingPartitionPro.Process(true);
                        MultiProcessTestCommand.DisplayMParkingPartitionPros(mParkingPartitionPro.ConvertToMParkingPartitionPro());
                        mParkingPartitionPro.IniLanes.Select(e => e.Line.ToDbLine()).AddToCurrentSpace();
                    }
                    catch (System.Exception ex)
                    {
                        Active.Editor.WriteMessage(ex.Message);
                    }
                }
            }
        }
        private void Execute()
        {
            var walls = new List<Polyline>();
            var iniLanes = new List<Line>();
            var obstacles = new List<Polyline>();
            var buildingBox = new List<Polyline>();
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var objs = result.Value
                   .GetObjectIds()
                   .Select(o => adb.Element<Entity>(o))
                   .Where(o => o is Line || o is Polyline)
                   .Select(o => o.Clone() as Entity)
                   .ToList();
                foreach (var o in objs)
                {
                    if (o.Layer == "inilanes") iniLanes.Add((Line)o);
                    else if (o.Layer == "walls")
                    {
                        if (o is Polyline) walls.Add((Polyline)o);
                        else if (o is Line) walls.Add(GeoUtilities.CreatePolyFromLine((Line)o));
                    }
                    else if (o.Layer == "obstacles")
                    {
                        if (o is Polyline) obstacles.Add((Polyline)o);
                    }
                }
            }
            var boundary = GeoUtilities.JoinCurves(walls, iniLanes)[0];
            boundary.Closed = true;

            var polygon_bound = new Polygon(new LinearRing(boundary.Vertices().Cast<Point3d>().Select(p => new Coordinate(p.X, p.Y)).ToArray()));
            MParkingPartitionPro mParkingPartitionPro = new MParkingPartitionPro(
                walls.Select(e => new LineString(e.Vertices().Cast<Point3d>().Select(p => new Coordinate(p.X, p.Y)).ToArray())).ToList(),
                iniLanes.Select(e => new LineSegment(new Coordinate(e.StartPoint.X, e.StartPoint.Y), new Coordinate(e.EndPoint.X, e.EndPoint.Y))).ToList(),
                obstacles.Select(e => new Polygon(new LinearRing(e.Vertices().Cast<Point3d>().Select(p => new Coordinate(p.X, p.Y)).ToArray()))).ToList(),
                polygon_bound);
            mParkingPartitionPro.OutputLanes=new List<LineSegment>();
            mParkingPartitionPro.OutBoundary = polygon_bound;
            mParkingPartitionPro.BuildingBoxes = new List<Polygon>();
            //mParkingPartitionPro.ObstaclesSpatialIndex = new MNTSSpatialIndex(obs);
            mParkingPartitionPro.ObstaclesSpatialIndex = new MNTSSpatialIndex(mParkingPartitionPro.Obstacles);
            mParkingPartitionPro.Process(true);
            MultiProcessTestCommand.DisplayMParkingPartitionPros(mParkingPartitionPro);
            mParkingPartitionPro.IniLanes.Select(e => e.Line.ToDbLine()).AddToCurrentSpace();
        }
    }
}
