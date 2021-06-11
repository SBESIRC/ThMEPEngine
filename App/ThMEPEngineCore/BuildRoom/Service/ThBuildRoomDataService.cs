using System.Linq;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.BuildRoom.Interface;
using AcHelper;
using Dreambuild.AutoCAD;

namespace ThMEPEngineCore.BuildRoom.Service
{
    public class ThBuildRoomDataService : IRoomBuildData
    {
        public List<Entity> Columns { get; private set; }
        public List<Entity> Walls { get; private set; }
        public List<Entity> Doors { get; private set; }
        public List<Entity> Windows { get; private set; }
        public List<Entity> Railings { get; private set; }
        public List<Entity> LineFoots { get; private set; }

        public bool SelfBuildData { get; set; }
        public ThBuildRoomDataService()
        {
            Columns = new List<Entity>();
            Walls = new List<Entity>();
            Doors = new List<Entity>();
            Windows = new List<Entity>();
            Railings = new List<Entity>();
            LineFoots = new List<Entity>();
        }

        public void Build(Database db, Point3dCollection pts)
        {
            if(SelfBuildData)
            {
                Columns = GetEntities("选择柱子");
                Walls = GetEntities("选择墙");
                Doors = GetEntities("选择门");
                Windows = GetEntities("选择窗户");
            }
            else
            {
                ObtainColumns(db, pts);
                ObtainWalls(db, pts);
                ObtainWindows(db, pts);
                ObtainDoors(db, pts);
                //ObtainRailings(db, pts);
                ObtainLineFoots(db, pts);
            }
        }

        private List<Entity> GetEntities(string message)
        {
            var results = new List<Entity>();
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                Active.Editor.WriteMessage("\n" + message);
                var psr = Active.Editor.GetSelection();
                if (psr.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    psr.Value.GetObjectIds().ForEach(o => results.Add(db.Element<Entity>(o)));
                }
                return results;
            }  
        }
        private void ObtainColumns(Database db, Point3dCollection pts)
        {
            using (var engine = new ThColumnRecognitionEngine())
            {
                engine.Recognize(db, pts);
                Columns = engine.Elements.Select(o=>o.Outline).ToList();
            }
        }
        private void ObtainWalls(Database db, Point3dCollection pts)
        {
            using (var architectureEngine = new ThDB3ArchWallRecognitionEngine())
            using (var shearwallEngine = new ThShearWallRecognitionEngine())
            {
                architectureEngine.Recognize(db, pts);
                shearwallEngine.Recognize(db, pts);
                Walls.AddRange(architectureEngine.Elements.Select(o=>o.Outline).ToList());
                Walls.AddRange(shearwallEngine.Elements.Select(o => o.Outline).ToList());
            }
        }
        private void ObtainWindows(Database db, Point3dCollection pts)
        {
            using (var engine = new ThWindowRecognitionEngine())
            {
                engine.Recognize(db, pts);
                Windows = engine.Elements.Select(o=>o.Outline).ToList();
            }
        }
        private void ObtainDoors(Database db, Point3dCollection pts)
        {
            using (var engine = new ThDoorRecognitionEngine())
            {
                engine.Recognize(db, pts);
                Doors = engine.Elements.Select(o=>o.Outline).ToList();
            }
        }
        private void ObtainRailings(Database db, Point3dCollection pts)
        {
            using (var engine = new ThRailingRecognitionEngine())
            {
                engine.Recognize(db, pts);
                Railings = engine.Elements.Select(o => o.Outline).ToList();
            }
        }
        private void ObtainLineFoots(Database db, Point3dCollection pts)
        {
            using (var engine = new ThLineFootRecognitionEngine())
            {
                engine.Recognize(db, pts);
                Railings = engine.Elements.Select(o => o.Outline).ToList();
            }
        }
    }
}
