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
            //ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(cleanData.Cast<Entity>().ToList(), AcHelper.Active.Database,1);
            var temp1 = new DBObjectCollection();
            cleanData.Cast<Entity>().ForEach(o =>
            {
                temp1.Add(o.ToNTSPolygon().Shell.ToDbPolyline());
            });
            cleanData = temp1; 
            cleanData = Union(cleanData);
            //ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(cleanData.Cast<Entity>().ToList(), AcHelper.Active.Database, 1);
            var temp2 = new DBObjectCollection();
            cleanData.Cast<Entity>().ForEach(o =>
            {
                temp2.Add(o.ToNTSPolygon().Shell.ToDbPolyline());
            });
            cleanData = temp2;
            cleanData = Buffer(cleanData, -ProOffsetDistance);
            cleanData = FilterSmallArea(cleanData, ProAREATOLERANCE);
            cleanData = PostProcess(cleanData);
            cleanData = Union(cleanData);
            Results = cleanData.Cast<Polyline>().ToList();
        }
        private DBObjectCollection PostProcess(DBObjectCollection objs)
        {
            var result = FilterSmallArea(objs, PostAREATOLERANCE);
            //result = Buffer(result, PostOffsetDistance);
            //result = FilterSmallArea(result, PostBufferAREATOLERANCE);
            //result = Buffer(result, -PostOffsetDistance);
            result = Buffer(result, ExpansionJointLength);
            result = Union(result);
            result = Buffer(result, -ExpansionJointLength);
            result = FilterSmallArea(result, PostBufferAREATOLERANCE);
            return result;
        }
        private DBObjectCollection Union(DBObjectCollection objs)
        {
            var result = objs.UnionPolygons();
            //return result;
            return FilterSmallArea(result, ProAREATOLERANCE);
        }
        private DBObjectCollection Buffer(DBObjectCollection objs,double disttance)
        {
            DBObjectCollection result = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            foreach(Entity obj in objs)
            {
                result.Add(bufferService.Buffer(obj, disttance));
            }
            return result;
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
        public DBObjectCollection _shearWalls;
        public DBObjectCollection _columns; 
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
        public readonly DBObjectCollection _archWall;
        public readonly DBObjectCollection _doors;
        public readonly DBObjectCollection _windows;
        public readonly DBObjectCollection _cornices;
        public readonly DBObjectCollection _slab;
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
            _slab = new DBObjectCollection();
            var slabEngine = new ThDB3SlabRecognitionEngine();
            slabEngine.Recognize(database, polygon);
            _slab = slabEngine.Geometries;
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
            _slab.Cast<Entity>().ForEach(o => results.Add(o));
            return results;
        }
    }
    public class Model2Data : ModelData
    {
        public readonly DBObjectCollection _beams;
        public ThBeamConnectRecogitionEngine BeamEngine { get; private set; }
        public Model2Data(Database database, Point3dCollection polygon)
        {
            _beams = new DBObjectCollection();
            BeamEngine = ThBeamConnectRecogitionEngine.ExecuteRecognize(database, polygon);
            _beams = BeamEngine.BeamEngine.Geometries;
            _shearWalls = BeamEngine.ShearWallEngine.Geometries;
            _columns = BeamEngine.ColumnEngine.Geometries;
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
