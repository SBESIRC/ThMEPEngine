using System.Linq;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanConnect.ViewModel;
using ThMEPHVAC.FanLayout.Service;
using ThCADExtension;
using ThMEPHVAC.FanConnect.Command;
using System;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThAddValveService
    {
        public ThWaterPipeConfigInfo ConfigInfo { set; get; }//界面输入信息
        public void AddValve(ThFanTreeModel tree)
        {
            if (tree.RootNode.Children.Count == 0)
            {
                return;
            }
            //遍历树
            BianLiTree(tree.RootNode);
        }
        public void BianLiTree(ThFanTreeNode<ThFanPipeModel> node)
        {
            foreach (var child in node.Children)
            {
                BianLiTree(child);
            }
            if(node.Item.IsValve)
            {
                return;
            }
            //找到结点与阀门的交点
            var pts = GetIntersectPoints(node);
            //找到距离起点最远的一个点
            if (pts.Count > 0)
            {
                pts = pts.OrderBy(o => o.DistanceTo(node.Item.PLine.StartPoint)).ToList();
                InsertValve(node,pts.Last());
            }
        }
        public void InsertValve(ThFanTreeNode<ThFanPipeModel> node,Point3d pt)
        {
            //
            var vector = node.Item.PLine.LineDirection().GetNormal();
            var valveAg = ThFanConnectUtils.GetVectorAngle(vector);
            var direct = new Vector3d(Math.Cos(valveAg + Math.PI / 2.0), Math.Sin(valveAg + Math.PI / 2.0), 0.0);
            if (node.Item.IsFlag)
            {
                direct = -direct;
            }
            switch (ConfigInfo.WaterSystemConfigInfo.SystemType)//系统
            {
                case 0://水系统
                    {
                        switch (ConfigInfo.WaterSystemConfigInfo.PipeSystemType)//管制
                        {
                            case 0://两管制
                                {
                                    var chspt = pt + direct * node.Item.PipeWidth;//冷热水供水管
                                    var chrpt = pt;//冷热水回水管
                                    InsertValve(vector, chspt, valveAg, 0);
                                    InsertValve(vector, chrpt, valveAg, 1);
                                }
                                break;
                            case 1://四管制
                                {
                                    var cspt = pt + direct * node.Item.PipeWidth * 2.0;//冷水供水
                                    var crpt = pt + direct * node.Item.PipeWidth;//冷水回水
                                    var hspt = pt;//热水供水管
                                    var hrpt = pt - direct * node.Item.PipeWidth;//热水回水
                                    InsertValve(vector, cspt, valveAg, 0);
                                    InsertValve(vector, crpt, valveAg, 1);
                                    InsertValve(vector, hspt, valveAg, 0);
                                    InsertValve(vector, hrpt, valveAg, 1);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                default:
                    break;
            }
            SetIsValve(node);
        }
        public void InsertValve(Vector3d vector, Point3d pt,double angle, int type)
        {
            ThFanToDBServiece toDbServerviece = new ThFanToDBServiece();
            pt = pt + vector * 350.0;
            if(type == 0)//供水管
            {
                foreach(var valve in ConfigInfo.WaterValveConfigInfo.FeedPipeValves)
                {
                    toDbServerviece.InsertValve("H-PAPP-VALV", "AI-水阀", valve, pt, angle, new Scale3d(1.0, 1.0, 1.0));
                    pt = pt + vector * 500.0;
                }
            }
            else if(type == 1)//回水管
            {
                foreach (var valve in ConfigInfo.WaterValveConfigInfo.ReturnPipeValeves)
                {
                    toDbServerviece.InsertValve("H-PAPP-VALV", "AI-水阀", valve, pt, angle, new Scale3d(1.0, 1.0, 1.0));
                    pt = pt + vector * 500.0;
                }
            }
        }
        public void SetIsValve(ThFanTreeNode<ThFanPipeModel> node)
        {
            node.Item.IsValve = true;
            if (node.Parent != null)
            {
                SetIsValve(node.Parent);
            }
        }
        public List<Point3d> GetIntersectPoints(ThFanTreeNode<ThFanPipeModel> node)
        {
            var retPoints = new List<Point3d>();
            foreach(var room in ConfigInfo.WaterValveConfigInfo.RoomObb)
            {
                var tmpPts = node.Item.PLine.IntersectWithEx(room).OfType<Point3d>().ToList();
                retPoints.AddRange(tmpPts);
            }
            return retPoints;
        }
    }
}
