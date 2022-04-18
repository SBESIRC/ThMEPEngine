using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPWSS.CADExtensionsNs;
using static ThMEPWSS.UndergroundWaterSystem.Utilities.GeoUtils;

namespace ThMEPWSS.UndergroundWaterSystem.Command
{
    public class TestCommand
    {
        [CommandMethod("TIANHUACAD", "ThDXJSSSTest", CommandFlags.Modal)]
        public void ThDXJSSSTest()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var objs = result.Value
                   .GetObjectIds()
                   .Select(o => adb.Element<Entity>(o))
                   .ToList();
                foreach (var obj in objs)
                {
                    var k = obj.ExplodeToDBObjectCollection().OfType<Entity>();
                    foreach (var l in k)
                    {
                        if (IsTianZhengElement(l))
                        {
                            var ps = l.ExplodeToDBObjectCollection().OfType<Entity>();
                        }
                       
                    }
                }
            }
        }
    }
}
