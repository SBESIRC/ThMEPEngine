using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Engine;

namespace ThMEPEngineCore.Service
{
    public class ThArchitectureOutlineBuilder
    {
        //
        public List<Polyline> Results { get; private set; }
        private DBObjectCollection _polygons { get; set; }
        private const double ProOffsetDistance = 5.0;
        private const double ProAREATOLERANCE = 1.0;
        private const double PostOffsetDistance = -500.0;
        private const double PostAREATOLERANCE = 5000000;
        private const double PostBufferAREATOLERANCE = 0.0;
        public ThArchitectureOutlineBuilder(DBObjectCollection polygons)
        {
            Results = new List<Polyline>();
            _polygons = polygons;
        }

        public void Build()
        {
            //var cleanData = PreProcess(Polygons);
            var cleanData = Buffer(_polygons, ProOffsetDistance);
            var results = Union(cleanData);
            results = Buffer(results, -ProOffsetDistance);
            FilterSmallArea(results, ProAREATOLERANCE);
            results = PostProcess(results);
            //TODO : 取Shell
            Results = results.Cast<Polyline>().ToList();
        }
        private DBObjectCollection PostProcess(DBObjectCollection objs)
        {
            var result = FilterSmallArea(objs, PostAREATOLERANCE);
            result = Buffer(result, PostOffsetDistance);
            FilterSmallArea(result, PostBufferAREATOLERANCE);
            result = Buffer(result, -PostOffsetDistance);
            return FilterSmallArea(result, PostBufferAREATOLERANCE);
        }
        private DBObjectCollection Union(DBObjectCollection objs)
        {
            var result = objs.UnionPolygons();
            return FilterSmallArea(result, ProAREATOLERANCE);
        }
        private DBObjectCollection Buffer(DBObjectCollection objs,double disttance)
        {
            return objs.Buffer(disttance);
        }
        private DBObjectCollection PreProcess(DBObjectCollection objs)
        {
            var results = new DBObjectCollection();
            results = objs;

            return results;
        }      
        private DBObjectCollection FilterSmallArea(DBObjectCollection objs,double areaTolerance)
        {
            return objs.Cast<Entity>().Where(o =>
            {
                if (o is Polyline polygon)
                {
                    return polygon.Area > areaTolerance;
                }
                else if (o is MPolygon mPolygon)
                {
                    return mPolygon.Area > areaTolerance;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }).ToCollection();
        }
    }
    public abstract class ModelData
    {
        protected DBObjectCollection _shearWalls;
        protected DBObjectCollection _columns; 
        public ModelData(Database database, Point3dCollection polygon)
        {
            _shearWalls = new DBObjectCollection();
            var shearWallEngine = new ThShearWallRecognitionEngine();
            shearWallEngine.Recognize(database, polygon);
            _shearWalls = shearWallEngine.Geometries;
            _columns = new DBObjectCollection();
            var columnEngine = new ThDB3ColumnRecognitionEngine();
            columnEngine.Recognize(database, polygon);
            _columns = columnEngine.Geometries;
        }
        public abstract DBObjectCollection MergeData();
    }

    public class Model1Data : ModelData
    {
        private readonly DBObjectCollection _archWall;
        private readonly DBObjectCollection _doors;
        private readonly DBObjectCollection _windows;
        private readonly DBObjectCollection _cornices;
        public Model1Data(Database database, Point3dCollection polygon) : base(database,polygon)
        {
            _archWall = new DBObjectCollection();
            var archWallEngine = new ThDB3ArchWallRecognitionEngine();
            archWallEngine.Recognize(database, polygon);
            _archWall = archWallEngine.Geometries;
            _doors = new DBObjectCollection();
            var doorEngine = new ThDB3DoorRecognitionEngine();
            doorEngine.Recognize(database, polygon);
            _doors = archWallEngine.Geometries;
            _windows = new DBObjectCollection();
            var windowEngine = new ThDB3WindowRecognitionEngine();
            windowEngine.Recognize(database, polygon);
            _windows = archWallEngine.Geometries;
            _cornices = new DBObjectCollection();
            var corniceEngine = new ThDB3CorniceRecognitionEngine();
            corniceEngine.Recognize(database, polygon);
            _cornices = archWallEngine.Geometries;
        }

        public override DBObjectCollection MergeData()
        {
            var results = new DBObjectCollection();
            _columns.Cast<Entity>().ForEach(o => results.Add(o));
            _shearWalls.Cast<Entity>().ForEach(o => results.Add(o));
            _archWall.Cast<Entity>().ForEach(o => results.Add(o));
            _doors.Cast<Entity>().ForEach(o => results.Add(o));
            _windows.Cast<Entity>().ForEach(o => results.Add(o));
            _cornices.Cast<Entity>().ForEach(o => results.Add(o));
            return results;
        }
    }
    public class Model2Data : ModelData
    {
        private readonly DBObjectCollection _beams;
        public Model2Data(Database database, Point3dCollection polygon) : base(database,polygon)
        {
            _beams = new DBObjectCollection();
            var beamEngine = ThMEPEngineCoreService.Instance.CreateBeamEngine();
            beamEngine.Recognize(database, polygon);
            _beams = beamEngine.Geometries;
        }

        public override DBObjectCollection MergeData()
        {
            var results = new DBObjectCollection();
            _columns.Cast<Entity>().ForEach(o => results.Add(o));
            _shearWalls.Cast<Entity>().ForEach(o => results.Add(o));
            _beams.Cast<Entity>().ForEach(o => results.Add(o));
            return results;
        }
    }
}
