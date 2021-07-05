using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Service
{
    public class ThArchitectureOutlineBuilder
    {
        //
        public List<Polyline> Results { get; set; }
        public Model Model { get; set; }
        public ModelData Data { get; set; }
        public ThArchitectureOutlineBuilder()
        {
            Model = Model.MODELFIRST;
            Results = new List<Polyline>();
        }

        public void Build()
        {
            var modelData = PrepareData();
            var cleanData = PreProcess(modelData.MergeData());
            cleanData = Buffer(cleanData);
            var results = Union(cleanData);
            results = PostProcess(results);
            Results = results.Cast<Polyline>().ToList();
        }
        private DBObjectCollection PostProcess(DBObjectCollection objs)
        {
            //ToDo
            return new DBObjectCollection();
        }
        private DBObjectCollection Union(DBObjectCollection objs)
        {
            //ToDo
            return new DBObjectCollection();
        }
        private DBObjectCollection Buffer(DBObjectCollection objs)
        {
            //ToDo
            return new DBObjectCollection();
        }
        private DBObjectCollection PreProcess(DBObjectCollection objs)
        {
            //ToDo
            return new DBObjectCollection();
        }
        private ModelData PrepareData()
        {
            if (Model== Model.MODELFIRST)
            {
                return PrepareModel1Data();
            }
            else if (Model == Model.MODELSECOND)
            {
                return PrepareMode21Data();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private Model1Data PrepareModel1Data()
        {
            var data = new Model1Data();
            //ToDo
            return data;
        }
        private Model2Data PrepareMode21Data()
        {
            var data = new Model2Data();
            //ToDo
            return data;
        }
    }
    public abstract class ModelData
    {
        public List<Entity> ShearWalls { get; set; }
        public List<Entity> ArchitectureWalls { get; set; }
        public ModelData()
        {
            ShearWalls = new List<Entity>();
            ArchitectureWalls = new List<Entity>();
        }
        public abstract DBObjectCollection MergeData();
    }

    public class Model1Data : ModelData
    {
        public List<Polyline> Columns { get; set; }
        public List<Polyline> Doors { get; set; }
        public List<Polyline> Windows { get; set; }
        public List<Polyline> Cornices { get; set; }
        public Model1Data()
        {            
            Columns = new List<Polyline>();
            Doors = new List<Polyline>(); 
            Windows = new List<Polyline>();
            Cornices = new List<Polyline>();
        }

        public override DBObjectCollection MergeData()
        {
            throw new NotImplementedException();
        }
    }
    public class Model2Data : ModelData
    {
        public List<Entity> Beams { get; set; }
        public Model2Data()
        {
            Beams = new List<Entity>();
        }

        public override DBObjectCollection MergeData()
        {
            throw new NotImplementedException();
        }
    }
    public enum Model
    {
        MODELFIRST, 
        MODELSECOND
    }
}
