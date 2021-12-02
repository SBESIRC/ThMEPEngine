using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPHVAC.FanConnect.Model;

namespace ThMEPHVAC.FanConnect.Service
{
    /// <summary>
    /// 管路路由生成器
    /// </summary>
    public class ThPipeExtractServiece
    {
        public Point3d PipeStartPt { set; get; }//水管起始点
        public List<ThFanCUModel> EquipModel { set; get; }//风机设备
        public List<Line> TrunkLines { set; get; }//干路线
        public List<Line> BranchLines { set; get; }//支干路
        public List<Polyline> ObstacleRooms { set; get; }//可穿越区域，但是必须垂直穿越且代价大(房间框线)
        public List<Polyline> ObstacleHoles { set; get; }//不可穿越区域
        /// <summary>
        /// 生成管路路由
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<Polyline> CreatePipePath(int type)
        {
            List<Polyline> retPline = null;
            switch (type)
            {
                case 0:
                    retPline = GetPipeTreeModel0();
                    break;
                case 1:
                    retPline = GetPipeTreeModel1();
                    break;
                default:
                    break;
            }

            return retPline;
        }
        private List<Polyline> GetPipePath0()
        {
            var retList = new List<Polyline>();
            var pathServiece = new ThCreatePathServiece();
            pathServiece.TrunkLines = TrunkLines;
            pathServiece.ObstacleRooms = ObstacleRooms;
            pathServiece.ObstacleHoles = ObstacleHoles;
            pathServiece.InitData();
            foreach (var equip in EquipModel)
            {
                var tmpPath = pathServiece.CreatePath(equip);
                if(tmpPath != null)
                {
                    retList.Add(tmpPath);
                }
            }
            return retList;
        }
        private List<Line> GetPipePath1()
        {
            var retList = new List<Line>();

            return retList;
        }
        private List<Polyline> GetPipeTreeModel0()
        {
            //将设备进行连接，得到管路路由
            var termLine = GetPipePath0();//支路
            return termLine;
        }
        private List<Polyline> GetPipeTreeModel1()
        {
            List<Polyline> retPLine = new List<Polyline>();
            //可以引用自己写类和方法
            return retPLine;
        }
        //private ThFanTreeModel<ThFanPipeModel> GetPipeTreeModel2()
        //{
        //    ThFanTreeModel<ThFanPipeModel> retTree = new ThFanTreeModel<ThFanPipeModel>();

        //    //root结点
        //    var rootLine = new Line();
        //    rootLine.StartPoint = new Point3d(0.0, 1000.0,0.0);
        //    rootLine.EndPoint = new Point3d(3000.0, 1000.0,0.0);
        //    ThFanPipeModel rooModel = new ThFanPipeModel(rootLine, PIPELEVEL.LEVEL1);
        //    rooModel.PipeWidth = 100;
        //    ThFanTreeNode<ThFanPipeModel> rootNode = new ThFanTreeNode<ThFanPipeModel>(rooModel);
        //    //Child1
        //    var childLine1 = new Line();
        //    childLine1.StartPoint = new Point3d(2000.0, 1000.0, 0.0);
        //    childLine1.EndPoint = new Point3d(2000.0, 4000.0,0.0);
        //    ThFanPipeModel childModel1 = new ThFanPipeModel(childLine1, PIPELEVEL.LEVEL2);
        //    childModel1.PipeWidth = 50;
        //    ThFanTreeNode<ThFanPipeModel> childNode1 = new ThFanTreeNode<ThFanPipeModel>(childModel1);

        //    //Child2
        //    var childLine2 = new Line();
        //    childLine2.StartPoint = new Point3d(3000.0, 1000.0, 0.0);
        //    childLine2.EndPoint = new Point3d(3000.0, 0.0,0.0);
        //    ThFanPipeModel childModel2 = new ThFanPipeModel(childLine2, PIPELEVEL.LEVEL2);
        //    childModel2.PipeWidth = 50;
        //    ThFanTreeNode<ThFanPipeModel> childNode2 = new ThFanTreeNode<ThFanPipeModel>(childModel2);
        //    //Child Child1
        //    var childChildLine1 = new Line();
        //    childChildLine1.StartPoint = new Point3d(2000.0, 3000.0, 0.0);
        //    childChildLine1.EndPoint = new Point3d(1000.0, 3000.0,0.0);

        //    ThFanPipeModel childChildModel1 = new ThFanPipeModel(childChildLine1, PIPELEVEL.LEVEL3);
        //    childChildModel1.PipeWidth = 20;
        //    ThFanTreeNode<ThFanPipeModel> childChildNode1 = new ThFanTreeNode<ThFanPipeModel>(childChildModel1);
        //    childNode1.InsertChild(childChildNode1);
        //    rootNode.InsertChild(childNode1);
        //    rootNode.InsertChild(childNode2);
        //    retTree.RootNode = rootNode;
        //    return retTree;
        //}

    }
}
