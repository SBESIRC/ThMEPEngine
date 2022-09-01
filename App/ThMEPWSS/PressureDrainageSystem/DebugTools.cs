using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ThCADExtension;
using ThMEPWSS.CADExtensionsNs;

namespace ThMEPWSS.PressureDrainageSystem
{
    public static class DebugTools
    {

        public static string ShowLine(Line line)
        {
            return ShowPoints(new List<Point3d>() { line.StartPoint, line.EndPoint });
        }
        public static string ShowPolyline(Polyline ply)
        {
            var points = ply.Vertices().Cast<Point3d>().ToList();
            return ShowPoints(points);
        }
        public static string ShowLineList(List<Line> lines)
        {
            var str = "";
            foreach (var line in lines)
                str += ShowLine(line) + ";";
            return str;
        }
        public static string ShowPolylineList(List<Polyline> plys)
        {
            var str = "";
            foreach (var ply in plys)
                str += ShowPolyline(ply) + ";";
            return str;
        }
        public static string ShowPoints(List<Point3d> points)
        {
            var str = "";
            foreach (var point in points)
            {
                str += point.X.ToString() + "," + point.Y.ToString() + ",";
            }
            if (str.Length > 0) str = str.Remove(str.Length - 1);
            return str;
        }
        public static string ShowPoint(Point3d point)
        {
            return point.X.ToString() + "," + point.Y.ToString();
        }
        public static void LogDebugInfos(string str, string filename, FileMode mode = FileMode.Create)
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            dir += "\\DebugTools";
            var file = dir + "\\" + filename;
            if (!filename.Contains(".txt"))
                file += ".txt";
            FileStream fs = new FileStream(file, mode);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(str);
            sw.Close();
            fs.Close();
        }
    }
    partial class DebugCommand
    {
        [CommandMethod("TIANHUACAD", "ThExplodeCAD", CommandFlags.Modal)]
        public void ThExplodeCAD()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var entities = GetEntities().ToList();
                var adds = new List<Entity>();
                foreach (var element in entities.Where(e => Utils.PressureDrainageUtils.IsTianZhengElement(e)))
                {
                    adds.AddRange(GetAllEntitiesByExplodingTianZhengElementThoroughly(element).Select(ent =>
                    {
                        ent.Layer = element.Layer;
                        return ent;
                    }));
                }
                adds.AddToCurrentSpace();
                string filename = Active.Document.Name;
                var file = filename.Split('\\').Last();
                filename= filename.Replace(file, "ExplodedDebug__"+file);
                adb.Database.SaveAs(filename, DwgVersion.Current);
                MessageBox.Show("转换成功");
            }
        }
        IEnumerable<Entity> GetEntities()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                foreach (var ent in adb.ModelSpace.OfType<Entity>())
                {
                    yield return ent;
                }
            }
        }
        List<Entity> GetAllEntitiesByExplodingTianZhengElementThoroughly(Entity entity)
        {
            try
            {
                if (!Utils.PressureDrainageUtils.IsTianZhengElement(entity))
                    return new List<Entity>() { entity };
                List<Entity> results = new List<Entity>();
                var explodes = entity.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                foreach (var e in explodes)
                {
                    results.AddRange(GetAllEntitiesByExplodingTianZhengElementThoroughly(e));
                }
                return results;
            }
            catch (System.Exception ex) { return new List<Entity>() { entity }; }
        }
    }
}
