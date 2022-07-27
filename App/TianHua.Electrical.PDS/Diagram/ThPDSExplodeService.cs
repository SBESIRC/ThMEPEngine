using System.Linq;

using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;

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
