using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Engine
{
    public class ThColumnRecognitionEngine : ThBuildingElementRecognitionEngine, IDisposable
    {
        public ThColumnRecognitionEngine()
        {
            Elements = new List<ThIfcBuildingElement>();
        }

        public void Dispose()
        {
            //ToDo
        }

        public override void Recognize(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var columnDbExtension = new ThStructureColumnDbExtension(database))
            {
                columnDbExtension.BuildElementCurves();
                DBObjectCollection dbObjs = new DBObjectCollection();
                columnDbExtension.ColumnCurves.ForEach(o => dbObjs.Add(o.Clone() as Curve));
                foreach (Curve curve in dbObjs)
                {
                    Elements.Add(ThIfcColumn.CreateColumnEntity(curve));
                }
            }
        }
    }
}
