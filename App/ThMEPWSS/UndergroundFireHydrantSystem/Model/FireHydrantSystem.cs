using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    class FireHydrantSystemOut
    {
        public Dictionary<Point3dEx, Point3dEx> BranchDrawDic { get; set; }
        public List<Line> LoopLine { get; set; }
        public List<Line> TextLine { get; set; }
        public List<DBText> TextList { get; set; }
        public Dictionary<Point3d, double> PipeInterrupted { get; set; }
        public List<Point3d> Valve { get; set; }

        public List<Point3d> FireHydrant { get; set; }
        public Point3d InsertPoint { get; set; }

        public FireHydrantSystemOut()
        {
            BranchDrawDic = new Dictionary<Point3dEx, Point3dEx>();
            LoopLine = new List<Line>();
            TextLine = new List<Line>();
            TextList = new List<DBText>();
            PipeInterrupted = new Dictionary<Point3d, double>();
            Valve = new List<Point3d>();
            FireHydrant = new List<Point3d>();
            var opt = new PromptPointOptions("指定消火栓系统图插入点");
            InsertPoint = Active.Editor.GetPoint(opt).Value;
        }

        public void Draw()
        {
            using ( var acadDatabase = AcadDatabase.Active())
            {
                foreach(var line in LoopLine)
                {
                    line.LayerId = DbHelper.GetLayerId("W-FRPT-HYDT-PIPE");
                    acadDatabase.CurrentSpace.Add(line);
                }

                foreach(var text in TextList)
                {
                    acadDatabase.CurrentSpace.Add(text);
                }

                foreach(var line in TextLine)
                {
                    //line.LayerId = DbHelper.GetLayerId("W-FRPT-HYDT-DIMS");
                    acadDatabase.CurrentSpace.Add(line);
                }
            

                foreach(var pipeInt in PipeInterrupted.Keys)
                {
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-DIMS", "水管中断",
                    pipeInt, new Scale3d(-0.8, 0.8, 0.8), PipeInterrupted[pipeInt]);
                }

                foreach(var valve in Valve)
                {
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "蝶阀",
                    valve, new Scale3d(1, 1, 1), 0);
                }

                foreach(var fh in FireHydrant)
                {
                    var objID = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-PIPE", "室内消火栓系统", 
                        fh, new Scale3d(1, 1, 1), 0);
                    objID.SetDynBlockValue("可见性", "单栓");
                }
            }
        }
    }

    class FireHydrantSystemIn
    {
        public List<Line> markLine { get; set; }
        public List<List<Point3dEx>> nodeList { get; set; }
        public Dictionary<Point3dEx, double> angleList { get; set; }
        public Dictionary<Point3dEx, string> markList { get; set; }
        public Dictionary<Point3dEx, string> ptTypeDic { get; set; }
        public List<Point3dEx> hydrantPosition { get; set; }
        public Dictionary<Point3dEx, TermPoint> termPointDic { get; set; }
        public Dictionary<Point3dEx, List<Point3dEx>> ptDic { get; set; }

        public bool ValveIsBkReference { get; set; }

        public FireHydrantSystemIn()
        {
            markLine = new List<Line>();//环管标记所在直线
            nodeList = new List<List<Point3dEx>>();//次环节点
            angleList = new Dictionary<Point3dEx, double>();//次环节点角度
            markList = new Dictionary<Point3dEx, string>();//次环节点名称
            ptTypeDic = new Dictionary<Point3dEx, string>();//当前点的类型字典对
            hydrantPosition = new List<Point3dEx>(); //消火栓端点
            termPointDic = new Dictionary<Point3dEx, TermPoint>();
            ptDic = new Dictionary<Point3dEx, List<Point3dEx>>();//当前点和邻接点字典对
        }
    }
}
