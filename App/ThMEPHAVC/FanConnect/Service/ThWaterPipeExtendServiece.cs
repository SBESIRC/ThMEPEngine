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

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThWaterPipeExtendServiece : ThPipeExtendBaseServiece
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
            //将连接处的线进行补齐并绘制小圆点
            ExtendEnds(node);
            //绘制扩展线
            DrawExLine(node);
            //绘制接点
            DrawContact(node);
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
                                            {
                                                LineCount = 4;
                                            }
                                            break;
                                        case PIPELEVEL.LEVEL3:
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
                                        var closPt = pline.GetClosestPointTo(cline.StartPoint, true);
                                        cline.StartPoint = closPt;
                                        node.Item.ExPoint.Add(closPt);
                                        if (node.Item.IsConnect)
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
                                                    if (node.Item.IsConnect)
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

                                                Point3d closPt1 = pline1.GetClosestPointTo(cline1.StartPoint, true);
                                                Point3d closPt2 = pline2.GetClosestPointTo(cline1.StartPoint, true);
                                                Point3d closPt3 = pline3.GetClosestPointTo(cline2.StartPoint, true);
                                                Point3d closPt4 = pline4.GetClosestPointTo(cline2.StartPoint, true);
                                                Point3d closPt5 = pline5.GetClosestPointTo(cline3.StartPoint, true);

                                                cline1.StartPoint = closPt2;
                                                if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                                                {
                                                    cline2.StartPoint = closPt4;
                                                }
                                                else
                                                {
                                                    cline2.StartPoint = closPt3;
                                                }
                                                cline3.StartPoint = closPt5;

                                                node.Item.ExPoint.Add(closPt1);
                                                node.Item.ExPoint.Add(closPt2);
                                                node.Item.ExPoint.Add(closPt3);
                                                node.Item.ExPoint.Add(closPt4);
                                                node.Item.ExPoint.Add(closPt5);
                                                if (node.Item.IsConnect)
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
                            if (node.Item.IsConnect)
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
                                        case PIPELEVEL.LEVEL3:
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
                        if (!node.Item.IsConnect || node.Item.PipeLevel == PIPELEVEL.LEVEL3 || node.Item.WayCount == 3)
                        {
                            if (node.Item.WayCount == 2)
                            {
                                if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                                {
                                    for (int i = 0; i < node.Item.ExPoint.Count - 1; i++)
                                    {
                                        var pt = node.Item.ExPoint[i];
                                        var circle = new Circle(pt, new Vector3d(0.0, 0.0, 1.0), 50);
                                        DrawCircle(circle, "H-PIPE-DIMS");
                                    }
                                    if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                    {
                                        if (node.Item.ExPoint.Count > 0)
                                        {
                                            var pt = node.Item.ExPoint.Last();
                                            var circle = new Circle(pt, new Vector3d(0.0, 0.0, 1.0), 50);
                                            DrawCircle(circle, "H-PIPE-DIMS");
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
                                            DrawCircle(circle, "H-PIPE-DIMS");
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
                                            DrawCircle(circle, "H-PIPE-DIMS");
                                        }
                                        if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                        {
                                            if (node.Item.ExPoint.Count > 0)
                                            {
                                                var pt = node.Item.ExPoint.Last();
                                                var circle = new Circle(pt, new Vector3d(0.0, 0.0, 1.0), 50);
                                                DrawCircle(circle, "H-PIPE-DIMS");
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
                                                DrawCircle(circle, "H-PIPE-DIMS");
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
                                            toDbServiece.InsertBlockReference("H-PIPE-R", "AI-分歧管", node.Item.ExPoint[0], angle, scale);

                                            var circle = new Circle(node.Item.ExPoint[2], new Vector3d(0.0, 0.0, 1.0), 50);
                                            DrawCircle(circle, "H-PIPE-DIMS");
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
                                            toDbServiece.InsertBlockReference("H-PIPE-R", "AI-分歧管", node.Item.ExPoint[1], angle, scale);
                                        }
                                    }
                                    else
                                    {
                                        if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                        {
                                            var circle = new Circle(node.Item.ExPoint[1], new Vector3d(0.0, 0.0, 1.0), 50);
                                            DrawCircle(circle, "H-PIPE-DIMS");
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
                                                toDbServiece.InsertBlockReference("H-PIPE-DIMS", "AI-分歧管", node.Item.ExPoint[0], angle, scale);

                                                var circle = new Circle(node.Item.ExPoint[2], new Vector3d(0.0, 0.0, 1.0), 50);
                                                DrawCircle(circle, "H-PIPE-DIMS");
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
                                                toDbServiece.InsertBlockReference("H-PIPE-DIMS", "AI-分歧管", node.Item.ExPoint[1], angle, scale);
                                            }

                                        }
                                        else
                                        {
                                            if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
                                            {
                                                var circle = new Circle(node.Item.ExPoint[1], new Vector3d(0.0, 0.0, 1.0), 50);
                                                DrawCircle(circle, "H-PIPE-DIMS");
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
