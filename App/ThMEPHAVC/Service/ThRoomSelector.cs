using AcHelper;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Runtime;

namespace ThMEPHVAC.Service
{
    public class ThRoomSelector
    {
        public List<ThIfcRoom> Rooms { get; private set; }
        public ThRoomSelector()
        {
            Rooms = new List<ThIfcRoom>();
        }

        public void Select()
        {
            var roomOutlines = SelectRoomOutlines();
            roomOutlines = Propress(roomOutlines);
            Rooms = CreateRooms(roomOutlines);
        }

        private DBObjectCollection SelectRoomOutlines()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var pso = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "\n请选择房间框线",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                 {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                 };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var psr = Active.Editor.GetSelection(pso, filter);
                if (psr.Status == PromptStatus.OK)
                {
                    return psr.Value.GetObjectIds()
                        .Select(o => acadDb.Element<Entity>(o))
                        .Where(o => o is Polyline)
                        .ToCollection();
                }
                return new DBObjectCollection();
            }
        }

        private DBObjectCollection Propress(DBObjectCollection roomOutlines)
        {
            var simplifier = new ThRoomOutlineSimplifier();
            var results = simplifier.Tessellate(roomOutlines);
            simplifier.MakeClosed(results);
            results = results.FilterSmallArea(1.0);
            results = simplifier.Normalize(results);
            results = results.FilterSmallArea(1.0);
            results = simplifier.MakeValid(results);
            results = results.FilterSmallArea(1.0);
            results = simplifier.Simplify(results);
            results = results.FilterSmallArea(1.0);
            return results;
        }

        private List<ThIfcRoom> CreateRooms(DBObjectCollection roomOutlines)
        {
            var results = new List<ThIfcRoom>();
            var rooms = roomOutlines.OfType<Polyline>().Select(o => ThIfcRoom.Create(o)).ToList();
            var textNotes = new List<ThIfcTextNote>();
            var builder = new ThRoomBuilderEngine();
            builder.Build(rooms, textNotes, true);
            return rooms
                .Where(o=>o.Boundary!=null)
                .ToList();
        }
    }
}
