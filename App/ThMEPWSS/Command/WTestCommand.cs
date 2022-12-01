using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPWSS.CADExtensionsNs;

namespace ThMEPWSS.Command
{
    public class WTestCommand
    {
        [CommandMethod("TIANHUACAD", "WTestGCicle", CommandFlags.Modal)]
        public void GenerateCicleFromLinePlus()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())//使用命名空间Linq2Acad的类的一个方法叫做active
            {
                var ents= acadDatabase.ModelSpace.OfType<Entity>().ToList();//取出cad数据库的所有元素的列表A
                var ents_in_bound = ents.Where(e => e.Layer.Equals("bound")).ToList();//在列表A里面找到名字是bound的图层的列表
                var ents_in_block= ents.Where(e => e.Layer.Equals("block")).ToList();//在列表A里面找到名字是block的图层的列表
                var blks_in_block = ents_in_block.Where(e => e is BlockReference).Select(e => (BlockReference)e).ToList();
                //找到block图层里面的具有块属性的元素B
                var ply = (Polyline)ents_in_bound[0];
                blks_in_block = blks_in_block.Where(e=> ply.Contains(e.Position)).ToList();//矩形里面容纳具有块属性的元素B
                
                var lines = new List<Line>();
                foreach (var br in blks_in_block)
                {
                    var objs=br.ExplodeToDBObjectCollection().Cast<Entity>().ToList();
                    ;
                    foreach (var obj in objs)
                    {
                        if (obj is Line)
                            lines.Add((Line)obj);
                    }
                }
                var ents_in_test = ents.Where(e => e.Layer.Equals("test")).Where(r  => r is Line).ToList();
                foreach(Line r in ents_in_test)
                {
                    var newlines = new List<Line>();


                    
                    foreach (var li in lines)
                    {
                       
                        if(li.Buffer(5000).Intersect(r,Intersect.OnBothOperands).Count>0||ply.Contains(r))
                        {
                            newlines.Add(li);
                        }
                    }
                    GenerateCircleFromLine(newlines);

                }
                //BlockReference br;
                //var objs=br.ExplodeToDBObjectCollection().Cast<Entity>().ToList();
            }


            //var lines = readLine();
            //var cicles = new List<Circle>();
            ////foreach (var ln in lines)
            ////{
            ////    var ci = GenerateCicle(ln);
            ////    cicles.Add(ci);
            ////}
            //cicles= lines.Select(e => GenerateCicle(e)).ToList();
            //cicles.AddToCurrentSpace();
        }
        public void GenerateCircleFromLine(List<Line> lines)
        {
            //var lines = readLine();
            var cicles = new List<Circle>();
            //foreach (var ln in lines)
            //{
            //    var ci = GenerateCicle(ln);
            //    cicles.Add(ci);
            //}
            cicles = lines.Select(e => GenerateCicle(e)).ToList();
            cicles.AddToCurrentSpace();
        }
        public Circle GenerateCicle(Line line)
        {
            Circle ci = new Circle(line.GetMidpoint(), Autodesk.AutoCAD.Geometry.Vector3d.ZAxis, line.Length / 2);
            return ci;
        }
        public List<Line> readLine()
        {
            var res=new List<Line>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return res;
                }
                var objs = result.Value
                                  .GetObjectIds()
                                  .Select(o => acadDatabase.Element<Entity>(o))
                                  .Where(o => o is Line)
                                  .ToList();
                objs = objs.Where(e => e is Line).ToList();
                var lines=objs.Select(e => (Line)e).ToList();
                if(lines.Count > 0) 
                    return lines;
                else
                    return res;
            }
        }
    }
}
namespace Wtest
{
    public class WtestCommand
    {
        [CommandMethod("TIANHUACAD", "WTestGCircle", CommandFlags.Modal)]
        public void GenerateCircleFromLine()
        {

            var line = ReadLine();

            //var pls = new List<Polyline>();
            //Polyline pl=new Polyline();

            ////对象pl和多线段list，找到交点
            //foreach (var p in pls)
            //{
            //    if (pl.Intersect(p, Intersect.OnBothOperands).Count > 0)
            //    {

            //    }
            //}
            ////这里把多线段列表建立索引，然后用这个索引自带的函数去找到交点
            //ThCADCoreNTSSpatialIndex spacialIndex=new ThCADCoreNTSSpatialIndex(pls.ToCollection());
            //var crossed= spacialIndex.SelectCrossingPolygon(pl).Cast<Polyline>().ToList();
        }
        public void FindCrossing()
        {
            var pls = new List<Polyline>();
            var pl = new Polyline();
            ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(pls.ToCollection());
            var coss = spatialIndex.SelectCrossingPolygon(pl).Cast<Polyline>().ToList();
            
        }
        public List<Line> ReadLine()
        {
            var res = new List<Line>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return res;
                }
                var objs = result.Value.GetObjectIds().Select(o => acadDatabase.Element<Entity>(o))
                                  .Where(o => o is Line)
                                  .ToList();
                var obj=objs.Where(e => e is Line ).ToList();
                var lines = obj.Select(e => (Line)e).ToList();
                if (lines.Count > 0) return lines;
                else
                    return res;



            }
        }
        public Circle GenerateCircle(Line line)
        {
            var res = new Circle();
            Circle c = new Circle(line.GetMidpoint(), Autodesk.AutoCAD.Geometry.Vector3d.ZAxis, line.Length / 2);
            return res;
        }
    };
}

