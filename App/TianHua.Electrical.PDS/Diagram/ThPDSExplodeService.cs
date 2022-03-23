using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System.Linq;

namespace TianHua.Electrical.PDS.Diagram
{
    public class ThPDSExplodeService
    {
        public static DBObjectCollection BlockExplode(AcadDatabase activeDb, BlockReference block)
        {
            var objs = new DBObjectCollection();
            block.Explode(objs);
            objs.OfType<Entity>().ForEach(e => activeDb.ModelSpace.Add(e));
            block.Erase();
            return objs;
        }
    }
}
