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
using Linq2Acad;
using ThMEPHVAC.FanLayout.Service;
using ThMEPEngineCore.CAD;
using ThMEPHVAC.FanConnect.Command;

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThWaterPipeExtendService : ThPipeExtendBaseService
    {
        public ThWaterPipeConfigInfo ConfigInfo { set; get; }//界面输入信息
        public override void PipeExtend(ThFanTreeModel tree)
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
            //将连接处的线进行补齐
            ExtendEnds(node);
            //绘制扩展线
            DrawExLine(node);
            //绘制接点
            DrawContact(node);
            foreach (var n in node.Children)
            {
                BianLiTree(n);
            }
        }
        public void WaterPipeExtend(ThFanPipeModel pipeModel)
        {
            var pipeLine = pipeModel.PLine;

            var pipeWidth = pipeModel.PipeWidth;

            int LineCount = 0;
            switch (ConfigInfo.WaterSystemConfigInfo.SystemType)//系统
            {
                case 0://水系统
                    {
                        switch (ConfigInfo.WaterSystemConfigInfo.PipeSystemType)//管制
                        {
                            case 0://两管制
                                {
                                    LineCount = 2;
                                }
                                break;
                            case 1://四管制
                                {
                                    switch (pipeModel.PipeLevel)
                                    {
                                        //
                                        case PIPELEVEL.LEVEL1:
                                        case PIPELEVEL.LEVEL2:
                                        case PIPELEVEL.LEVEL3:
                                            {
                                                LineCount = 4;
                                            }
                                            break;
                                        case PIPELEVEL.LEVEL4:
                                            {
                                                LineCount = 2;
                                                //根据路由生成CHS(路由线)+CHR+C
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
                        LineCount = 2;
                    }
                    break;
                default:
                    break;
            }
            pipeModel.ExPline = OffsetLines(pipeLine, pipeWidth, LineCount, pipeModel.IsFlag);
        }
        public void ExtendEnds(ThFanTreeNode<ThFanPipeModel> node)
        {
            if (node.Parent == null)
            {
                return;
            }
            var currentExLines = node.Item.ExPline;
            var parentExLines = node.Parent.Item.ExPline;

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
                                        var closPt = ThFanConnectUtils.IntersectWithEx(pline,cline, Intersect.ExtendBoth);
                                        if(closPt.Count > 0)
                                        {
                                            cline.StartPoint = closPt[0];
                                            node.Item.ExPoint.Add(closPt[0]);
                                            if (node.Item.IsConnect)
                                            {
                                                pline.EndPoint = closPt[0];
                                            }
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
                                        case PIPELEVEL.LEVEL3:
                                            {
                                                for (int i = 0; i < currentExLines.Count; i++)
                                                {
                                                    var cline = currentExLines[i];
                                                    var pline = parentExLines[i];
                                                    var closPt = ThFanConnectUtils.IntersectWithEx(pline,cline, Intersect.ExtendBoth);
                                                    if (closPt.Count > 0)
                                                    {
                                                        cline.StartPoint = closPt[0];
                                                        node.Item.ExPoint.Add(closPt[0]);
                                                        if (node.Item.IsConnect)
                                                        {
                                                            pline.EndPoint = closPt[0];
                                                        }
                                                    }

                                                }
                                            }
                                            break;
                                        case PIPELEVEL.LEVEL4:
                                            {
                                                var cline1 = currentExLines[0];
                                                var cline2 = currentExLines[1];
                                                var cline3 = currentExLines[2];
                                                var pline1 = parentExLines[0];
                                                var pline2 = parentExLines[1];
                                                var pline3 = parentExLines[2];
                                                var pline4 = parentExLines[3];
                                                var pline5 = parentExLines[4];

                                                var closPt1 = ThFanConnectUtils.IntersectWithEx(pline1,cline1, Intersect.ExtendBoth);
                                                var closPt2 = ThFanConnectUtils.IntersectWithEx(pline2,cline1, Intersect.ExtendBoth);
                                                var closPt3 = ThFanConnectUtils.IntersectWithEx(pline3,cline2, Intersect.ExtendBoth);
                                                var closPt4 = ThFanConnectUtils.IntersectWithEx(pline4,cline2, Intersect.ExtendBoth);
                                                var closPt5 = ThFanConnectUtils.IntersectWithEx(pline5,cline3, Intersect.ExtendBoth);
                                                if (closPt1.Count > 0 && closPt2.Count > 0 && closPt3.Count > 0 && closPt4.Count > 0 && closPt5.Count > 0)
                                                {
                                                    if(cline1.EndPoint.DistanceTo(closPt1[0]) > cline1.EndPoint.DistanceTo(closPt2[0]))
                                                    {
                                                        cline1.StartPoint = closPt1[0];
                                                    }
                                                    else
                                                    {
                                                        cline1.StartPoint = closPt2[0];
                                                    }
                                                    
                                                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                                                    {
                                                        if(cline2.EndPoint.DistanceTo(closPt3[0]) > cline2.EndPoint.DistanceTo(closPt4[0]))
                                                        {
                                                            cline2.StartPoint = closPt3[0];
                                                        }
                                                        else
                                                        {
                                                            cline2.StartPoint = closPt4[0];
                                                        }
                                                    }
                                                    else
                                                    {
                                                        cline2.StartPoint = closPt3[0];
                                                    }
                                                    cline3.StartPoint = closPt5[0];

                                                    node.Item.ExPoint.Add(closPt1[0]);
                                                    node.Item.ExPoint.Add(closPt2[0]);
                                                    node.Item.ExPoint.Add(closPt3[0]);
                                                    node.Item.ExPoint.Add(closPt4[0]);
                                                    node.Item.ExPoint.Add(closPt5[0]);
                                                    if (node.Item.IsConnect)
                                                    {
                                                        pline1.EndPoint = closPt1[0];
                                                        pline2.EndPoint = closPt2[0];
                                                        pline3.EndPoint = closPt3[0];
                                                        pline4.EndPoint = closPt4[0];
                                                        pline5.EndPoint = closPt5[0];
                                                    }
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
                            var closPt = ThFanConnectUtils.IntersectWithEx(pline,cline, Intersect.ExtendBoth);
                            if (closPt.Count > 0)
                            {
                                cline.StartPoint = closPt[0];
                                node.Item.ExPoint.Add(closPt[0]);
                                if (node.Item.IsConnect)
                                {
                                    pline.EndPoint = closPt[0];
                                }
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        public void DrawExLine(ThFanTreeNode<ThFanPipeModel> node)
        {
            switch (ConfigInfo.WaterSystemConfigInfo.SystemType)//系统
            {
                case 0://水系统
                    {
                        switch (ConfigInfo.WaterSystemConfigInfo.PipeSystemType)//管制
                        {
                            case 0://两管制
                                {
                                    if(ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                                    {
                                        DrawLine(node.Item.ExPline[0], "H-PIPE-CHS");
                                        DrawLine(node.Item.ExPline[1], "H-PIPE-CHR");
                                        if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                        {
                                            DrawLine(node.Item.ExPline[2], "H-PIPE-C");
                                        }
                                    }
                                    else
                                    {
                                        if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                        {
                                            DrawLine(node.Item.ExPline[1], "H-PIPE-C");
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
                                        case PIPELEVEL.LEVEL3:
                                            {
                                                if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                                                {
                                                    DrawLine(node.Item.ExPline[0], "H-PIPE-CS");
                                                    DrawLine(node.Item.ExPline[1], "H-PIPE-CR");
                                                    DrawLine(node.Item.ExPline[2], "H-PIPE-HS");
                                                    DrawLine(node.Item.ExPline[3], "H-PIPE-HR");
                                                    if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                                    {

                                                        DrawLine(node.Item.ExPline[4], "H-PIPE-C");
                                                    }
                                                }
                                                else
                                                {
                                                    if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                                    {
                                                        DrawLine(node.Item.ExPline[2], "H-PIPE-C");
                                                    }
                                                }
                                            }
                                            break;
                                        case PIPELEVEL.LEVEL4:
                                            {
                                                if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                                                {
                                                    DrawLine(node.Item.ExPline[0], "H-PIPE-CHS");
                                                    DrawLine(node.Item.ExPline[1], "H-PIPE-CHR");
                                                    if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                                    {

                                                        DrawLine(node.Item.ExPline[2], "H-PIPE-C");
                                                    }
                                                }
                                                else
                                                {
                                                    if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                                    {
                                                        DrawLine(node.Item.ExPline[1], "H-PIPE-C");
                                                    }
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
                        if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                        {
                            if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                            {
                                DrawLine(node.Item.ExPline[0], "H-PIPE-R");
                                DrawLine(node.Item.ExPline[2], "H-PIPE-C");
                            }
                            else
                            {
                                DrawLine(node.Item.ExPline[1], "H-PIPE-R");
                            }
                        }
                        else
                        {
                            if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                            {
                                DrawLine(node.Item.ExPline[1], "H-PIPE-C");
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        public void DrawContact(ThFanTreeNode<ThFanPipeModel> node)
        {
            switch (ConfigInfo.WaterSystemConfigInfo.SystemType)//系统
            {
                case 0://水系统
                    {
                        if (!node.Item.IsConnect || node.Item.PipeLevel == PIPELEVEL.LEVEL4 || node.Item.WayCount == 3)
                        {
                            if (node.Item.WayCount == 2)
                            {
                                if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                                {
                                    for (int i = 0; i < node.Item.ExPoint.Count - 1; i++)
                                    {
                                        var pt = node.Item.ExPoint[i];
                                        var circle = new Circle(pt, new Vector3d(0.0, 0.0, 1.0), 50);
                                        DrawCircle(circle, "H-PIPE-APPE");
                                    }
                                    if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                    {
                                        if (node.Item.ExPoint.Count > 0)
                                        {
                                            var pt = node.Item.ExPoint.Last();
                                            var circle = new Circle(pt, new Vector3d(0.0, 0.0, 1.0), 50);
                                            DrawCircle(circle, "H-PIPE-APPE");
                                        }
                                    }
                                }
                                else
                                {
                                    if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                    {
                                        if (node.Item.ExPoint.Count > 0)
                                        {
                                            var pt = node.Item.ExPoint[node.Item.ExPoint.Count/2];
                                            var circle = new Circle(pt, new Vector3d(0.0, 0.0, 1.0), 50);
                                            DrawCircle(circle, "H-PIPE-APPE");
                                        }
                                    }
                                }
                            }
                            else if (node.Item.WayCount == 3 || node.Item.WayCount == 4)
                            {
                                if (!node.Item.BrotherItem.IsContacted)
                                {
                                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                                    {
                                        for (int i = 0; i < node.Item.ExPoint.Count - 1; i++)
                                        {
                                            var pt = node.Item.ExPoint[i];
                                            var circle = new Circle(pt, new Vector3d(0.0, 0.0, 1.0), 50);
                                            DrawCircle(circle, "H-PIPE-APPE");
                                        }
                                        if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                        {
                                            if (node.Item.ExPoint.Count > 0)
                                            {
                                                var pt = node.Item.ExPoint.Last();
                                                var circle = new Circle(pt, new Vector3d(0.0, 0.0, 1.0), 50);
                                                DrawCircle(circle, "H-PIPE-APPE");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                        {
                                            if (node.Item.ExPoint.Count > 0)
                                            {
                                                var pt = node.Item.ExPoint[node.Item.ExPoint.Count/2];
                                                var circle = new Circle(pt, new Vector3d(0.0, 0.0, 1.0), 50);
                                                DrawCircle(circle, "H-PIPE-APPE");
                                            }
                                        }
                                    }

                                }
                            }
                            node.Item.IsContacted = true;
                        }
                    }
                    break;
                case 1://冷媒系统
                    {
                        if (!node.Item.IsConnect || node.Item.WayCount == 3)
                        {
                            if (node.Item.ExPoint.Count > 0 && node.Parent != null)
                            {
                                var toDbServiece = new ThFanToDBServiece();
                                if (node.Item.WayCount == 2)
                                {
                                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                                    {
                                        var angle = node.Parent.Item.PLine.Angle + Math.PI / 2.0;
                                        var direction = new Vector3d(Math.Cos(angle), Math.Sin(angle), 0.0);
                                        if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                        {
                                            var tmpPt = node.Item.ExPoint[0] + direction * 100;
                                            var scale = new Scale3d(1.0, 1.0, 1.0);
                                            if (node.Item.CroVector.IsEqualTo(new Vector3d(0.0, 0.0, -1.0)))
                                            {
                                                scale = new Scale3d(-1.0, 1.0, 1.0);
                                                tmpPt = node.Item.ExPoint[0] - direction * 100;
                                            }
                                            node.Item.ExPline[0].StartPoint = tmpPt;
                                            toDbServiece.InsertBlockReference("H-PIPE-APPE", "AI-分歧管", node.Item.ExPoint[0], angle, scale);

                                            var circle = new Circle(node.Item.ExPoint[2], new Vector3d(0.0, 0.0, 1.0), 50);
                                            DrawCircle(circle, "H-PIPE-APPE");
                                        }
                                        else
                                        {
                                            var tmpPt = node.Item.ExPoint[1] + direction * 100;
                                            var scale = new Scale3d(1.0, 1.0, 1.0);
                                            if (node.Item.CroVector.IsEqualTo(new Vector3d(0.0, 0.0, -1.0)))
                                            {
                                                scale = new Scale3d(-1.0, 1.0, 1.0);
                                                tmpPt = node.Item.ExPoint[1] - direction * 100;
                                            }
                                            node.Item.ExPline[1].StartPoint = tmpPt;
                                            toDbServiece.InsertBlockReference("H-PIPE-APPE", "AI-分歧管", node.Item.ExPoint[1], angle, scale);
                                        }
                                    }
                                    else
                                    {
                                        if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                        {
                                            var circle = new Circle(node.Item.ExPoint[1], new Vector3d(0.0, 0.0, 1.0), 50);
                                            DrawCircle(circle, "H-PIPE-APPE");
                                        }
                                    }
                                }
                                else if (node.Item.WayCount == 3)
                                {
                                    if (!node.Item.BrotherItem.IsContacted)
                                    {
                                        if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                                        {
                                            var angle = node.Parent.Item.PLine.Angle + Math.PI / 2.0;
                                            var direction = new Vector3d(Math.Cos(angle), Math.Sin(angle), 0.0);

                                            if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                            {
                                                var tmpPt = node.Item.ExPoint[0] + direction * 100;
                                                var scale = new Scale3d(1.0, 1.0, 1.0);
                                                if (node.Item.CroVector.IsEqualTo(new Vector3d(0.0, 0.0, -1.0)))
                                                {
                                                    scale = new Scale3d(-1.0, 1.0, 1.0);
                                                    tmpPt = node.Item.ExPoint[0] - direction * 100;
                                                }
                                                node.Item.ExPline[0].StartPoint = tmpPt;
                                                toDbServiece.InsertBlockReference("H-PIPE-APPE", "AI-分歧管", node.Item.ExPoint[0], angle, scale);

                                                var circle = new Circle(node.Item.ExPoint[2], new Vector3d(0.0, 0.0, 1.0), 50);
                                                DrawCircle(circle, "H-PIPE-APPE");
                                            }
                                            else
                                            {
                                                var tmpPt = node.Item.ExPoint[1] + direction * 100;
                                                var scale = new Scale3d(1.0, 1.0, 1.0);
                                                if (node.Item.CroVector.IsEqualTo(new Vector3d(0.0, 0.0, -1.0)))
                                                {
                                                    scale = new Scale3d(-1.0, 1.0, 1.0);
                                                    tmpPt = node.Item.ExPoint[1] - direction * 100;
                                                }
                                                node.Item.ExPline[1].StartPoint = tmpPt;
                                                toDbServiece.InsertBlockReference("H-PIPE-APPE", "AI-分歧管", node.Item.ExPoint[1], angle, scale);
                                            }

                                        }
                                        else
                                        {
                                            if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                            {
                                                var circle = new Circle(node.Item.ExPoint[1], new Vector3d(0.0, 0.0, 1.0), 50);
                                                DrawCircle(circle, "H-PIPE-APPE");
                                            }
                                        }

                                    }
                                }
                                node.Item.IsContacted = true;
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        private List<Line> OffsetLines(Line line, double offset, int count, bool isFlag)
        {
            var retLine = new List<Line>();
            double tmpOffset;
            int number = count / 2;
            for (int i = number; i >= 1; --i)
            {
                tmpOffset = offset * i;
                var tmpLine = OffsetLine(line, tmpOffset);
                retLine.Add(tmpLine);

            }

            var midLine = new Line(line.StartPoint, line.EndPoint);
            retLine.Add(midLine);

            for (int i = 1; i <= number; i++)
            {
                tmpOffset = -offset * i;
                var tmpLine = OffsetLine(line, tmpOffset);
                retLine.Add(tmpLine);
            }
            if (isFlag)
            {
                retLine.Reverse();
            }
            return retLine;
        }
        private Line OffsetLine(Line line, double offset)
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
        private void DrawLine(Line line ,string layer)
        {
            using (var database = AcadDatabase.Active())
            {
                database.ModelSpace.Add(line);
                line.Layer = layer;
                line.Linetype = "ByLayer";
                line.LineWeight = LineWeight.ByLayer;
                line.ColorIndex = (int)ColorIndex.BYLAYER;
            }
        }
        private void DrawCircle(Circle circle,string layer)
        {
            using (var database = AcadDatabase.Active())
            {
                database.ModelSpace.Add(circle);
                circle.Layer = layer;
                circle.Linetype = "ByLayer";
                circle.LineWeight = LineWeight.ByLayer;
                circle.ColorIndex = (int)ColorIndex.BYLAYER;
            }
        }

    }
}
