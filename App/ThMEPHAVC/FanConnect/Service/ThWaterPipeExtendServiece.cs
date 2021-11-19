using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanConnect.ViewModel;
using ThCADExtension;


namespace ThMEPHVAC.FanConnect.Service
{
    class ThWaterPipeExtendServiece : ThPipeExtendBaseServiece
    {
        public ThWaterPipeConfigInfo ConfigInfo { set; get; }//界面输入信息
        public override void PipeExtend(ThFanTreeModel<ThFanPipeModel> tree)
        {
            //遍历树
           BianLiTree(tree.RootNode);
        }

        public void BianLiTree(ThFanTreeNode<ThFanPipeModel>  node)
        {
            //获取当前结点T1
            ThFanPipeModel t1 = node.Item;
            //设置方向
            //对当前结点进行扩展
            WaterPipeExtend(t1);
            //将连接处的线进行补齐并绘制小圆点
            ExtendEnds(node);

            for (int i = 0; i < node.Item.ExPline.Count;i++)
            {
                var l = node.Item.ExPline[i];
                l.ColorIndex = i+1;
                Draw.AddToCurrentSpace(l);
            }
            //
            foreach (var n in node.Children)
            {
                BianLiTree(n);
            }
        }
        public void WaterPipeExtend(ThFanPipeModel pipeModel)
        {
            var pipeLine = pipeModel.PLine;

            var pipeWidth = pipeModel.PipeWidth;

            List<Line> plines = new List<Line>();

            switch (ConfigInfo.WaterSystemConfigInfo.SystemType)//系统
            {
                case 0://水系统
                    {
                        switch (ConfigInfo.WaterSystemConfigInfo.PipeSystemType)//管制
                        {
                            case 0://两管制
                                {
                                    plines = OffsetLines(pipeLine, pipeWidth, 2);
                                }
                                break;
                            case 1://四管制
                                {
                                    switch (pipeModel.PipeLevel)
                                    {
                                        //
                                        case PIPELEVEL.LEVEL1:
                                        case PIPELEVEL.LEVEL2:
                                            {
                                                plines = OffsetLines(pipeLine, pipeWidth, 4);
                                            }
                                            break;
                                        case PIPELEVEL.LEVEL3:
                                            {
                                                //根据路由生成CHS(路由线)+CHR+C
                                                plines = OffsetLines(pipeLine, pipeWidth, 2);
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
                    }
                    break;
                case 1://冷媒系统
                    {
                        plines = OffsetLines(pipeLine, pipeWidth, 2);
                    }
                    break;
                default:
                    break;
            }
            pipeModel.ExPline = plines;
            return ;
            //根据PipeType确定颜色和图层，线型
        }
        public List<Line> OffsetLines(Line line,double offset,int count)
        {
            var retLine = new List<Line>();
            double tmpOffset;
            int number = count / 2;
            for (int i  = number;i >= 1;--i)
            {
                tmpOffset = offset * i;
                var tmpLine = OffsetLine(line, tmpOffset);
                retLine.Add(tmpLine);

            }
            retLine.Add(line);
            for (int i = 1; i <= number; i++)
            {
                tmpOffset = -offset * i;
                var tmpLine = OffsetLine(line, tmpOffset);
                retLine.Add(tmpLine);
            }
            return retLine;
        }
        public Line OffsetLine(Line line, double offset)
        {
            var retLine = new Line();
            var objCollection = line.GetOffsetCurves(offset);
            foreach (var obj in objCollection)
            {
                if (obj is Line)
                {
                    retLine = obj as Line;
                }
            }
            return retLine;

        }

        public void ExtendEnds(ThFanTreeNode<ThFanPipeModel> node)
        {
            if (node.Parent == null)
            {
                return;
            }
            
            var currentLine = node.Item.PLine;
            var currentExLines = node.Item.ExPline;
            var parentLine = node.Parent.Item.PLine;
            var parentExLines = node.Parent.Item.ExPline;

            bool isConnet = false;
            if(parentLine.EndPoint.IsEqualTo(currentLine.StartPoint))
            {
                isConnet = true;
            }

            switch (ConfigInfo.WaterSystemConfigInfo.SystemType)//系统
            {
                case 0://水系统
                    {
                        switch (ConfigInfo.WaterSystemConfigInfo.PipeSystemType)//管制
                        {
                            case 0://两管制
                                {
                                    for(int i = 0; i < currentExLines.Count;i++)
                                    {
                                        var cline = currentExLines[i];
                                        var pline = parentExLines[i];
                                        var closPt = pline.GetClosestPointTo(cline.StartPoint, true);
                                        cline.StartPoint = closPt;
                                        node.Item.ExPoint.Add(closPt);
                                        if (isConnet)
                                        {
                                            pline.EndPoint = closPt;
                                        }
                                    }
                                }
                                break;
                            case 1://四管制
                                {
                                    switch (node.Item.PipeLevel)
                                    {
                                        //
                                        case PIPELEVEL.LEVEL1:
                                        case PIPELEVEL.LEVEL2:
                                            {
                                                for (int i = 0; i < currentExLines.Count; i++)
                                                {
                                                    var cline = currentExLines[i];
                                                    var pline = parentExLines[i];
                                                    var closPt = pline.GetClosestPointTo(cline.StartPoint, true);
                                                    cline.StartPoint = closPt;
                                                    node.Item.ExPoint.Add(closPt);
                                                    if (isConnet)
                                                    {
                                                        pline.EndPoint = closPt;
                                                    }
                                                }
                                            }
                                            break;
                                        case PIPELEVEL.LEVEL3:
                                            {
                                                var cline1 = currentExLines[0];
                                                var cline2 = currentExLines[1];
                                                var cline3 = currentExLines[2];
                                                var pline1 = parentExLines[0];
                                                var pline2 = parentExLines[1];
                                                var pline3 = parentExLines[2];
                                                var pline4 = parentExLines[3];
                                                var pline5 = parentExLines[4];

                                                var closPt1 = pline1.GetClosestPointTo(cline1.StartPoint, true);
                                                var closPt2 = pline2.GetClosestPointTo(cline1.StartPoint, true);
                                                var closPt3 = pline3.GetClosestPointTo(cline2.StartPoint, true);
                                                var closPt4 = pline4.GetClosestPointTo(cline2.StartPoint, true);
                                                var closPt5 = pline5.GetClosestPointTo(cline3.StartPoint, true);

                                                cline1.StartPoint = closPt2;
                                                cline2.StartPoint = closPt4;
                                                cline3.StartPoint = closPt5;
                                                node.Item.ExPoint.Add(closPt1);
                                                node.Item.ExPoint.Add(closPt2);
                                                node.Item.ExPoint.Add(closPt3);
                                                node.Item.ExPoint.Add(closPt4);
                                                node.Item.ExPoint.Add(closPt5);
                                                if (isConnet)
                                                {
                                                    pline1.EndPoint = closPt1;
                                                    pline2.EndPoint = closPt2;
                                                    pline3.EndPoint = closPt3;
                                                    pline4.EndPoint = closPt4;
                                                    pline5.EndPoint = closPt5;
                                                }
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
                    }
                    break;
                case 1://冷媒系统
                    {
                        for (int i = 0; i < currentExLines.Count; i++)
                        {
                            var cline = currentExLines[i];
                            var pline = parentExLines[i];
                            var closPt = pline.GetClosestPointTo(cline.StartPoint, true);
                            cline.StartPoint = closPt;
                            node.Item.ExPoint.Add(closPt);
                            if (isConnet)
                            {
                                pline.EndPoint = closPt;
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
