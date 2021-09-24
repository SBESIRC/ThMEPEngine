using Linq2Acad;
using System.Linq;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThColumnPersister : ThBuildingElementPersister
    {
        public override void Persist(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var layerId = acadDatabase.Database.CreateAIColumnLayer();
                Engines.ForEach(e =>
                {
                    e.Elements.Select(o => o.Outline)
                    .OfType<Polyline>()
                    .ForEach(o =>
                    {
                        o.LayerId = layerId;
                        acadDatabase.ModelSpace.Add(o);
                    });
                });
            }
        }
    }
}
