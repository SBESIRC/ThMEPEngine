using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.IO.ExcelService;

namespace ThMEPWSS.Common
{
    public class ThMEPWSSUtils
    {
        public static ThMEPOriginTransformer GetTransformer(Point3dCollection pts)
        {
            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            return transformer;
        }

        public static Polyline GetVisibleOBB(BlockReference blk)
        {
            var objs = new DBObjectCollection();
            blk.ExplodeWithVisible(objs);
            var curves = objs.OfType<Entity>()
                .Where(e => e is Curve).ToCollection();
            curves = Tesslate(curves);
            curves = curves.OfType<Curve>().Where(o => o != null && o.GetLength() > 1e-6).ToCollection();
            var obb = curves.GetMinimumRectangle();
            return obb;
        }
        private static DBObjectCollection Tesslate(DBObjectCollection curves,
  double arcLength = 50.0, double chordHeight = 50.0)
        {
            var results = new DBObjectCollection();
            curves.OfType<Curve>().ToList().ForEach(o =>
            {
                if (o is Line)
                {
                    results.Add(o);
                }
                else if (o is Arc arc)
                {
                    results.Add(arc.TessellateArcWithArc(arcLength));
                }
                else if (o is Circle circle)
                {
                    results.Add(circle.TessellateCircleWithArc(arcLength));
                }
                else if (o is Polyline polyline)
                {
                    results.Add(polyline.TessellatePolylineWithArc(arcLength));
                }
                else if (o is Ellipse ellipse)
                {
                    results.Add(ellipse.Tessellate(chordHeight));
                }
                else if (o is Spline spline)
                {
                    results.Add(spline.Tessellate(chordHeight));
                }
            });
            return results;
        }
        
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
