using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.BlockConvert
{
    class ThBConvertWipeOut
    {
        public void FixWipeOutDrawOrder(Database database, string name)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var wipeOuts = new ObjectIdCollection();
                var lines = new ObjectIdCollection();
                var btr = acadDatabase.Blocks.ElementOrDefault(name);
                foreach (ObjectId objId in btr)
                {
                    var entity = acadDatabase.Element<Entity>(objId);
                    if (entity is Wipeout)
                    {
                        wipeOuts.Add(entity.ObjectId);
                    }
                    if (entity is Line)
                    {
                        lines.Add(entity.ObjectId);
                    }
                }
                if (wipeOuts.Count > 0)
                {
                    var drawOrder = acadDatabase.Element<DrawOrderTable>(btr.DrawOrderTableId, true);
                    drawOrder.MoveToBottom(wipeOuts);
                }
                if (lines.Count > 0)
                {
                    var drawOrder = acadDatabase.Element<DrawOrderTable>(btr.DrawOrderTableId, true);
                    drawOrder.MoveToBottom(lines);
                }
            }
        }
    }
}
