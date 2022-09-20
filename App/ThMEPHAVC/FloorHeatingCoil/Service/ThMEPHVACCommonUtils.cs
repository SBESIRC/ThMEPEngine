using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;

using ThMEPEngineCore.Algorithm;
using ThCADExtension;

namespace ThMEPHVAC.FloorHeatingCoil.Service
{
    internal class ThMEPHVACCommonUtils
    {
        //public static ThMEPOriginTransformer GetTransformer(Point3dCollection pts)
        //{
        //    var center = pts.Envelope().CenterPoint();
        //    var transformer = new ThMEPOriginTransformer(center);
        //    return transformer;
        //}

        /////-------for no UI mode setting
        public static bool SettingBoolean(string hintString, int defaultValue)
        {
            var ans = false;
            var options = new PromptIntegerOptions(hintString);
            options.DefaultValue = defaultValue;
            var value = Active.Editor.GetInteger(options);
            if (value.Status == PromptStatus.OK)
            {
                ans = value.Value == 1 ? true : false;
            }

            return ans;
        }

        public static int SettingInt(string hintString, int defaultValue)
        {
            var ans = 0;
            var options = new PromptIntegerOptions(hintString);
            options.DefaultValue = defaultValue;
            var value = Active.Editor.GetInteger(options);
            if (value.Status == PromptStatus.OK)
            {
                ans = value.Value;
            }

            return ans;
        }

        public static string SettingString(string hintString)
        {
            var ans = "";
            var value = Active.Editor.GetString(hintString);
            if (value.Status == PromptStatus.OK)
            {
                ans = value.StringResult;
            }

            return ans;
        }

        public static double SettingDouble(string hintString, double defaultValue)
        {
            var ans = 0.0;

            var options = new PromptDoubleOptions(hintString);
            options.DefaultValue = defaultValue;
            var value = Active.Editor.GetDouble(options);
            if (value.Status == PromptStatus.OK)
            {
                ans = value.Value;
            }
            return ans;
        }

        public static string SettingSelection(string hintTitle, Dictionary<string, (string, string)> hintString, string defualt)
        {
            var ans = "";

            var options = new PromptKeywordOptions(hintTitle);
            foreach (var item in hintString)
            {
                options.Keywords.Add(item.Key, item.Value.Item1, item.Value.Item2);
            }
            if (defualt != "")
            {
                options.Keywords.Default = defualt;
            }

            var rst = Active.Editor.GetKeywords(options);
            if (rst.Status == PromptStatus.OK)
            {
                ans = rst.StringResult;
            }

            return ans;
        }

    }
}
