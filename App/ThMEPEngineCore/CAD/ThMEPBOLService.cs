using System.Collections.Generic;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ThMEPEngineCore.CAD
{
    public class ThMEPBOLService
    {
        public static List<string> Layers()
        {
            var names = new List<string>();
            foreach (var item in AcApp.UIBindings.Collections.Layers)
            {
                var properties = item.GetProperties();
                names.Add(properties["Name"].GetValue(item) as string);
            }
            return names;
        }

        public static List<string> Blocks()
        {
            var names = new List<string>();
            foreach (var item in AcApp.UIBindings.Collections.Blocks)
            {
                var properties = item.GetProperties();
                names.Add(properties["Name"].GetValue(item) as string);
            }
            return names;
        }
    }
}
