using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Engine;

namespace ThMEPEngineCore.Service
{
    public class ThArchitectureOutlineBuilder
    {
        public List<Polyline> Results { get; private set; }
        private DBObjectCollection _polygons;
        private const double ProOffsetDistance = 5.0;
        private const double ProAREATOLERANCE = 1.0;
        private const double PostOffsetDistance = -500.0;
        private const double PostAREATOLERANCE = 5000000.0;
        private const double PostBufferAREATOLERANCE = 0.0;
        private const double ExpansionJointLength = 151.0;
        public ThArchitectureOutlineBuilder(DBObjectCollection polygons)
        {
            Results = new List<Polyline>();
            _polygons = polygons;
        }

        public void Build()
        {
            if(_polygons.Count==0 || _polygons is null)
            {
                return;
            }
            //var cleanData = PreProcess(Polygons);
            //认为里面的数据均已进行了Simplifier的处理
            var cleanData = Buffer(_polygons, ProOffsetDistance);
            cleanData = Union(cleanData);
            cleanData.Cast<Entity>().ForEach(o =>
            {
                cleanData.Add(o.ToNTSPolygon().Shell.ToDbPolyline());
                cleanData.Remove(o);
            });
            cleanData = Union(cleanData);
            cleanData.Cast<Entity>().ForEach(o =>
            {
                cleanData.Add(o.ToNTSPolygon().Shell.ToDbPolyline());
                cleanData.Remove(o);
            });
            cleanData = Buffer(cleanData, -ProOffsetDistance);
            cleanData = FilterSmallArea(cleanData, ProAREATOLERANCE);
            cleanData = PostProcess(cleanData);
            Results = cleanData.Cast<Polyline>().ToList();
        }
        private DBObjectCollection PostProcess(DBObjectCollection objs)
        {
            var result = FilterSmallArea(objs, PostAREATOLERANCE);
            result = Buffer(result, PostOffsetDistance);
            result = FilterSmallArea(result, PostBufferAREATOLERANCE);
            result = Buffer(result, -PostOffsetDistance);
            result = Buffer(result, ExpansionJointLength);
            result = Buffer(result, -ExpansionJointLength);
            return FilterSmallArea(result, PostBufferAREATOLERANCE);
        }
        private DBObjectCollection Union(DBObjectCollection objs)
        {
            var result = objs.UnionPolygons();
            return FilterSmallArea(result, ProAREATOLERANCE);
        }
        private DBObjectCollection Buffer(DBObjectCollection objs,double disttance)
        {
            return objs.BufferPolygons(disttance);
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
            var shearWallEngine = new ThDB3ShearWallRecognitionEngine();
            shearWallEngine.Recognize(database, polygon);
            _shearWalls = shearWallEngine.Geometries;
            _columns = new DBObjectCollection();
            var columnEngine = new ThDB3ColumnRecognitionEngine();
            columnEngine.Recognize(database, polygon);
            _columns = columnEngine.Geometries;
        }
        protected ModelData()
        {
            _shearWalls = new DBObjectCollection();
            _columns = new DBObjectCollection();
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
            _doors = doorEngine.Geometries;
            _windows = new DBObjectCollection();
            var windowEngine = new ThDB3WindowRecognitionEngine();
            windowEngine.Recognize(database, polygon);
            _windows = windowEngine.Geometries;
            _cornices = new DBObjectCollection();
            var corniceEngine = new ThDB3CorniceRecognitionEngine();
            corniceEngine.Recognize(database, polygon);
            _cornices = corniceEngine.Geometries;
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
        public Model2Data(Database database, Point3dCollection polygon)
        {
            _beams = new DBObjectCollection();
            var beamEngine = ThBeamConnectRecogitionEngine.ExecuteRecognize(database, polygon);
            _beams = beamEngine.BeamEngine.Geometries;
            _shearWalls = beamEngine.ShearWallEngine.Geometries;
            _columns = beamEngine.ColumnEngine.Geometries;
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
