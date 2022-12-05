using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;

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
        private const double ExpansionJointLength = 300.0;
        private const double ExtendLineLength = 5.0;
        public ThArchitectureOutlineBuilder(DBObjectCollection polygons)
        {
            Results = new List<Polyline>();
            _polygons = polygons;
        }

        public void Build()
        {
            if (_polygons.Count==0 || _polygons is null)
            {
                return;
            }
            var transPolygons = TransPolygons();
            //var cleanData = PreProcess(Polygons);
            //认为里面的数据均已进行了Simplifier的处理
            var cleanData = Buffer(transPolygons, ProOffsetDistance);
            cleanData = Union(cleanData);
            
            //ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(cleanData.Cast<Entity>().ToList(), AcHelper.Active.Database,1);
            var temp1 = new DBObjectCollection();
            cleanData.Cast<Entity>().ForEach(o =>
            {
                temp1.Add(o.ToNTSPolygonalGeometry().Shell.ToDbPolyline());
            });
            cleanData = temp1;          
            cleanData = Union(cleanData);
            var temp2 = new DBObjectCollection();
            cleanData.Cast<Entity>().ForEach(o =>
            {
                temp2.Add(o.ToNTSPolygonalGeometry().Shell.ToDbPolyline());
            });          
            cleanData = temp2;
            cleanData = Buffer(cleanData, -ProOffsetDistance);
            cleanData = FilterSmallArea(cleanData, ProAREATOLERANCE);
            cleanData = PostProcess(cleanData);
            //ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(cleanData.Cast<Entity>().ToList(), AcHelper.Active.Database, 1);
            cleanData = Union(cleanData);
            Results = cleanData.Cast<Polyline>().ToList();
        }
        private DBObjectCollection TransPolygons()
        {
            var results = new DBObjectCollection();
            var lines = new List<Line>();
            _polygons.Cast<Entity>().ForEach(e =>
            {
                if (e is Polyline polyline)
                {
                    lines.AddRange(polyline.ToLines());
                }
                else if (e is MPolygon mPolygon)
                {
                    var polys = mPolygon.Loops();
                    polys.ForEach(p => lines.AddRange(p.ToLines()));
                }
                else
                {
                    throw new NotSupportedException();
                }
            });
            lines = lines.Select(o => o.ExtendLine(ExtendLineLength)).ToList();
            return lines.ToCollection().Polygons();
        }
        private DBObjectCollection PostProcess(DBObjectCollection objs)
        {
            var result = FilterSmallArea(objs, PostAREATOLERANCE);
            //result = Buffer(result, PostOffsetDistance);
            //result = FilterSmallArea(result, PostBufferAREATOLERANCE);
            //result = Buffer(result, -PostOffsetDistance);
            result = Buffer(result, ExpansionJointLength);
            //ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(result.Cast<Entity>().ToList(), AcHelper.Active.Database, 1);
            result = Union(result);
            result = Buffer(result, -ExpansionJointLength);
            //result = FilterSmallArea(result, PostBufferAREATOLERANCE);
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
                var entity = bufferService.Buffer(obj, disttance);
                if(entity!=null)
                {
                    result.Add(entity);
                }    
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
        protected DBObjectCollection _shearWalls;
        protected DBObjectCollection _columns;
        protected DBObjectCollection _beams;
        public DBObjectCollection Beams => _beams;
        public DBObjectCollection Columns => _columns;
        public DBObjectCollection ShearWalls => _shearWalls;
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
        private DBObjectCollection _archWalls;       
        private DBObjectCollection _cornices;
        private DBObjectCollection _doors;
        private DBObjectCollection _slabs;
        private DBObjectCollection _windows;
        public DBObjectCollection ArchWalls => _archWalls;        
        public DBObjectCollection Cornices => _cornices;
        public DBObjectCollection Doors => _doors;
        public DBObjectCollection Slabs => _slabs;
        public DBObjectCollection Windows => _windows;

        public ThBeamConnectRecogitionEngine BeamEngine { get; private set; }
        public Model1Data(Database database, Point3dCollection polygon)
        {
            _archWalls = new DBObjectCollection();
            var archWallEngine = new ThDB3ArchWallRecognitionEngine();
            archWallEngine.Recognize(database, polygon);
            _archWalls = archWallEngine.Geometries;
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
            _slabs = new DBObjectCollection();
            var slabEngine = new ThDB3SlabRecognitionEngine();
            slabEngine.Recognize(database, polygon);
            _slabs = slabEngine.Geometries;
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
            _archWalls.Cast<Entity>().ForEach(o => results.Add(o));
            _doors.Cast<Entity>().ForEach(o => results.Add(o));
            _windows.Cast<Entity>().ForEach(o => results.Add(o));
            _cornices.Cast<Entity>().ForEach(o => results.Add(o));
            _slabs.Cast<Entity>().ForEach(o => results.Add(o));
            _beams.Cast<Entity>().ForEach(o => results.Add(o));
            return results;
        }
    }
    public class Model2Data : ModelData
    {
        public List<ThBeamLink> PrimaryBeamLinks { get; set; }
        public List<ThBeamLink> OverhangingPrimaryBeamLinks { get; set; }

        public Model2Data(Database database, Point3dCollection polygon)
        {
            _beams = new DBObjectCollection();
            PrimaryBeamLinks = new List<ThBeamLink>();
            OverhangingPrimaryBeamLinks = new List<ThBeamLink>();
            var beamEngine = ThBeamConnectRecogitionEngine.ExecuteRecognize(database, polygon);
            _beams = beamEngine.BeamEngine.Geometries;
            _shearWalls = beamEngine.ShearWallEngine.Geometries;
            _columns = beamEngine.ColumnEngine.Geometries;
            PrimaryBeamLinks.AddRange(beamEngine.PrimaryBeamLinks);
            OverhangingPrimaryBeamLinks.AddRange(beamEngine.OverhangingPrimaryBeamLinks);
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
