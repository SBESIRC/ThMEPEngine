using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPHVAC.FanConnect.Command;
using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanConnect.ViewModel;
using ThMEPHVAC.FanLayout.Service;

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThWaterPipeMarkService
    {
        public ThWaterPipeConfigInfo ConfigInfo { set; get; }//界面输入信息
        public void CoolHotMark(ThFanTreeNode<ThFanPointModel> node, string coolPipe, string hotPipe, string strMarkHeight)//冷热水管标记
        {
            if (!ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe || node.Item.IsCoolHotMarked)
            {
                return;
            }
            if (node.Parent == null)
            {
                return;
            }
            //标记冷热水管
            ThFanToDBServiece toDbServerviece = new ThFanToDBServiece();
            var vector = node.Parent.Item.CntPoint.GetVectorTo(node.Item.CntPoint).GetNormal();
            var markAg = ThFanConnectUtils.GetVectorAngle(vector);
            var markPt = node.Item.CntPoint.GetMidPt(node.Parent.Item.CntPoint);
            var direct = new Vector3d(Math.Cos(markAg + Math.PI / 2.0), Math.Sin(markAg + Math.PI / 2.0), 0.0);
            if (node.Item.IsFlag)
            {
                direct = -direct;
            }
            string blockName = "";
            List<string> property = new List<string>();
            if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
            {
                blockName = "AI-水管多排标注(2排)";
                if (node.Parent.Item.CoolFlow < node.Parent.Item.HotFlow)
                {
                    string strchs = "CHS " + hotPipe + strMarkHeight;
                    string strchr = "CHR " + hotPipe + strMarkHeight;
                    property.Add(strchs);
                    property.Add(strchr);
                }
                else
                {
                    string strchs = "CHS " + coolPipe + strMarkHeight;
                    string strchr = "CHR " + coolPipe + strMarkHeight;
                    property.Add(strchs);
                    property.Add(strchr);
                }
                if (!node.Item.IsFlag)
                {
                    markPt = markPt + direct * 300 * 1;
                }
                else
                {
                    property.Reverse();
                }

            }
            else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
            {
                blockName = "AI-水管多排标注(4排)";
                string strcs = "CS " + coolPipe + strMarkHeight;
                string strcr = "CR " + coolPipe + strMarkHeight;
                string strhs = "HS " + hotPipe + strMarkHeight;
                string strhr = "HR " + hotPipe + strMarkHeight;
                property.Add(strcs);
                property.Add(strcr);
                property.Add(strhs);
                property.Add(strhr);
                if (!node.Item.IsFlag)
                    markPt = markPt + direct * 300 * 2;
                else
                {
                    markPt = markPt - direct * 300 * 1;
                    property.Reverse();
                }
            }

            var tmpPt1 = node.Parent.Item.CntPoint.TransformBy(Active.Editor.WCS2UCS());
            var tmpPt2 = node.Item.CntPoint.TransformBy(Active.Editor.WCS2UCS());
            var tmpVector = tmpPt1.GetVectorTo(tmpPt2).GetNormal();
            var tmpAg = ThFanConnectUtils.GetVectorAngle(tmpVector);
            if (tmpAg > Math.PI / 2.0 && tmpAg <= Math.PI * 3.0 / 2.0)
            {
                markAg = markAg + Math.PI;
                property.Reverse();
                if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
                {
                    if (!node.Item.IsFlag)
                        markPt = markPt - direct * 300 * 1;
                    else
                        markPt = markPt + direct * 300 * 1;
                }
                else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
                {
                    if (!node.Item.IsFlag)
                        markPt = markPt - direct * 300 * 3;
                    else
                        markPt = markPt + direct * 300 * 3;
                }
            }
            toDbServerviece.InsertPipeMark("H-PIPE-DIMS", blockName, markPt, markAg, property);
        }
        public void CondMark(ThFanTreeNode<ThFanPointModel> node, string condPipe)//冷凝水管标记
        {
            if (!ConfigInfo.WaterSystemConfigInfo.IsCWPipe || node.Item.IsCondMarked)
            {
                return;
            }
            if (node.Parent == null)
            {
                return;
            }
            ThFanToDBServiece toDbServerviece = new ThFanToDBServiece();
            var vector = node.Parent.Item.CntPoint.GetVectorTo(node.Item.CntPoint).GetNormal();
            var markAg = ThFanConnectUtils.GetVectorAngle(vector);
            var markPt = node.Item.CntPoint.GetMidPt(node.Parent.Item.CntPoint);
            var direct = new Vector3d(Math.Cos(markAg + Math.PI / 2.0), Math.Sin(markAg + Math.PI / 2.0), 0.0);
            if (node.Item.IsFlag)
            {
                direct = -direct;
            }
            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
            {
                if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)//两管制
                {
                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                    {
                        markPt = markPt - direct * (300.0 * 1 + 140);
                    }
                    else
                    {
                        markPt = markPt - direct * 140;
                    }
                }
                else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)//四管制
                {
                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                    {
                        markPt = markPt - direct * (300.0 * 2 + 140);
                    }
                    else
                    {
                        markPt = markPt - direct * 140;
                    }
                }
            }
            else if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
            {
                if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                {
                    markPt = markPt - direct * (300.0 * 1 + 140);
                }
                else
                {
                    markPt = markPt - direct * 140;
                }
            }

            var tmpPt1 = node.Parent.Item.CntPoint.TransformBy(Active.Editor.WCS2UCS());
            var tmpPt2 = node.Item.CntPoint.TransformBy(Active.Editor.WCS2UCS());
            var tmpVector = tmpPt1.GetVectorTo(tmpPt2).GetNormal();
            var tmpAg = ThFanConnectUtils.GetVectorAngle(tmpVector);
            if (tmpAg > Math.PI / 2.0 && tmpAg <= Math.PI * 3.0 / 2.0)
            {
                markAg = markAg - Math.PI;
            }
            var strText = "C " + condPipe;
            toDbServerviece.InsertText("H-PIPE-DIMS", strText, markPt, markAg);
        }
        public void UpdateMark(ThFanTreeNode<ThFanPointModel> node, BlockReference blk, string coolPipe, string hotPipe, string strMarkHeight)
        {
            var effName = blk.GetEffectiveName();
            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 0)
            {
                if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
                {
                    if (effName == "AI-水管多排标注(2排)")
                    {
                        string strchs = "CHS " + coolPipe + strMarkHeight;
                        string strchr = "CHR " + hotPipe + strMarkHeight;
                        Dictionary<string, string> attNameValues = new Dictionary<string, string>();
                        attNameValues.Add("水管标注1", strchs);
                        attNameValues.Add("水管标注2", strchr);
                        blk.ObjectId.UpdateAttributesInBlock(attNameValues);
                    }
                    else
                    {
                        blk.UpgradeOpen();
                        blk.Erase();
                        blk.DowngradeOpen();
                        CoolHotMark(node, coolPipe, hotPipe, strMarkHeight);
                    }
                }
                else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
                {
                    if (effName == "AI-水管多排标注(4排)")
                    {
                        string strcs = "CS " + coolPipe + strMarkHeight;
                        string strcr = "CR " + coolPipe + strMarkHeight;
                        string strhs = "HS " + hotPipe + strMarkHeight;
                        string strhr = "HR " + hotPipe + strMarkHeight;
                        Dictionary<string, string> attNameValues = new Dictionary<string, string>();
                        attNameValues.Add("水管标注1", strcs);
                        attNameValues.Add("水管标注2", strcr);
                        attNameValues.Add("水管标注3", strhs);
                        attNameValues.Add("水管标注4", strhr);
                        blk.ObjectId.UpdateAttributesInBlock(attNameValues);
                    }
                    else
                    {
                        blk.UpgradeOpen();
                        blk.Erase();
                        blk.DowngradeOpen();
                        CoolHotMark(node, coolPipe, hotPipe, strMarkHeight);
                    }
                }
            }
            else if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统，如果之前有水管多排标注，全部删除
            {
                if (effName == "AI-水管多排标注(2排)" || effName == "AI-水管多排标注(4排)")
                {
                    blk.UpgradeOpen();
                    blk.Erase();
                    blk.DowngradeOpen();
                }
            }
        }
        public void UpdateText(DBText text, string condPipe)
        {
            var strText = "C " + condPipe;
            text.UpgradeOpen();
            text.TextString = strText;
            text.DowngradeOpen();
        }
        public List<Entity> FindMarkFromLine(Line line, ref List<Entity> marks)
        {
            var box = line.Buffer(650);
            var retMark = new List<Entity>();
            foreach (var mark in marks)
            {
                if (mark is BlockReference)
                {
                    var blk = mark as BlockReference;
                    if (box.Contains(blk.Position))
                    {
                        if (blk.GetEffectiveName().Contains("AI-水管多排标注"))
                        {
                            retMark.Add(mark);
                        }
                    }
                }
            }
            marks = marks.Except(retMark).ToList();
            return retMark;
        }
        public List<Entity> FindTextFromLine(Line line, ref List<Entity> marks)
        {
            var box = line.Buffer(400);
            var retText = new List<Entity>();
            foreach (var mark in marks)
            {
                if (mark is DBText)
                {
                    var text = mark as DBText;
                    if (box.Contains(text.AlignmentPoint))
                    {
                        retText.Add(mark);
                    }
                }
            }
            marks = marks.Except(retText).ToList();
            return retText;
        }
        public List<ThFanTreeNode<ThFanPointModel>> FindConnectNode(ThFanTreeNode<ThFanPointModel> node)
        {
            var retNodes = new List<ThFanTreeNode<ThFanPointModel>>();
            retNodes.Add(node);
            if (!node.Item.IsCrossPoint)
            {
                retNodes.AddRange(FindConnectNode1(node));//找子结点
            }
            retNodes.AddRange(FindConnectNode2(node));//找父结点
            retNodes = retNodes.Distinct().ToList();
            return retNodes;
        }
        public List<ThFanTreeNode<ThFanPointModel>> FindConnectNode1(ThFanTreeNode<ThFanPointModel> node)
        {
            var retNodes = new List<ThFanTreeNode<ThFanPointModel>>();
            if (node.Children.Count > 0)
            {
                if (!node.Children[0].Item.IsCrossPoint)
                {
                    retNodes.Add(node.Children[0]);
                    retNodes.AddRange(FindConnectNode1(node.Children[0]));
                }
            }
            return retNodes;
        }
        public List<ThFanTreeNode<ThFanPointModel>> FindConnectNode2(ThFanTreeNode<ThFanPointModel> node)
        {
            var retNodes = new List<ThFanTreeNode<ThFanPointModel>>();
            if (node.Parent != null)
            {
                if (!node.Parent.Item.IsCrossPoint)
                {
                    retNodes.Add(node.Parent);
                    retNodes.AddRange(FindConnectNode2(node.Parent));
                }
            }
            return retNodes;
        }
        public void PipeMark(ThPointTreeModel tree)
        {
            if (tree.RootNode.Children.Count == 0)
            {
                return;
            }
            //            NodeMark(tree.RootNode.Children.First());
            //遍历树
            BianLiTree(tree.RootNode);
        }
        public void BianLiTree(ThFanTreeNode<ThFanPointModel> node)
        {
            //遍历子结点
            foreach (var child in node.Children)
            {
                BianLiTree(child);
            }
            if (node.Parent == null)
            {
                return;
            }

            if (node.Children.Count == 0)
            {
                NodeMark(node);
            }
            else if (node.Children.Count >= 2)
            {
                foreach (var child in node.Children)
                {
                    NodeMark(child);
                }
            }
            ThQueryDNService queryDNServiece = new ThQueryDNService();
            //父结点冷水管管径
            var parentCoolPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Parent.Item.CoolFlow);
            //父结点热水管管径
            var parentHotPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Parent.Item.HotFlow);
            //父结点冷凝水管管径
            var parentCondPipe = queryDNServiece.QueryCondPipeDN(node.Parent.Item.CoolCapa);
            //当前结点冷水管管径
            var curCoolPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.CoolFlow);
            //当前结点热水管管径
            var curHotPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.HotFlow);
            //当前结点冷凝水管管径
            var curCondPipe = queryDNServiece.QueryCondPipeDN(node.Item.CoolCapa);

            if (curCondPipe != parentCondPipe)
            {
                if (node.Parent.Parent != null)
                {
                    MarkCondPipe(node.Parent);
                }
                MarkCondPipe(node);
            }

            if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
            {
                if (node.Parent.Item.CoolFlow < node.Parent.Item.HotFlow)
                {
                    if (curHotPipe != parentHotPipe)
                    {
                        if (node.Parent.Parent != null)
                        {
                            MarkCoolPipe(node.Parent);
                        }
                        MarkCoolPipe(node);
                    }
                }
                else
                {
                    if (curCoolPipe != parentCoolPipe)
                    {
                        if (node.Parent.Parent != null)
                        {
                            MarkCoolPipe(node.Parent);
                        }
                        MarkCoolPipe(node);
                    }
                }
            }
            else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
            {
                if ((curCoolPipe != parentCoolPipe) || (curHotPipe != parentHotPipe))
                {
                    if (node.Parent.Parent != null)
                    {
                        MarkCoolPipe(node.Parent);
                    }
                    MarkCoolPipe(node);
                }
            }
        }
        public void NodeMark(ThFanTreeNode<ThFanPointModel> node)
        {
            //查询到直接连接的子结点
            var connectChild = FindConnectNode(node);
            connectChild = connectChild.OrderBy(o => DistanceTo(o, o.Parent)).ToList();
            NodeMark1(connectChild.Last());
            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
            {
                foreach (var n in connectChild)
                {
                    n.Item.IsCoolHotMarked = true;
                    n.Item.IsCondMarked = true;
                }
            }
            else if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
            {
                foreach (var n in connectChild)
                {
                    n.Item.IsCondMarked = true;
                }
            }
        }
        public void NodeMark1(ThFanTreeNode<ThFanPointModel> node)
        {
            ThQueryDNService queryDNServiece = new ThQueryDNService();
            string strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString("f2") + ")";

            var curCoolPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.CoolFlow);//node.Item.CoolFlow.ToString();
            //当前结点热水管管径
            var curHotPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.HotFlow);//node.Item.HotFlow.ToString();
            //当前结点冷凝水管管径
            var curCondPipe = queryDNServiece.QueryCondPipeDN(node.Item.CoolCapa); //node.Item.CoolCapa.ToString();
            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
            {
                //标记冷热水管
                CoolHotMark(node, curCoolPipe, curHotPipe, strMarkHeight);
                //标记冷凝水管
                CondMark(node, curCondPipe);
            }
            else if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
            {
                //标记冷凝水管
                CondMark(node, curCondPipe);
            }
        }
        public void MarkCoolPipe(ThFanTreeNode<ThFanPointModel> node)
        {
            //查询到直接连接的子结点
            var connectChild = FindConnectNode(node);
            connectChild = connectChild.OrderBy(o => DistanceTo(o, o.Parent)).ToList();
            MarkCoolPipe1(connectChild.Last());
            foreach (var n in connectChild)
            {
                n.Item.IsCoolHotMarked = true;
            }
        }
        public double DistanceTo(ThFanTreeNode<ThFanPointModel> node1, ThFanTreeNode<ThFanPointModel> node2)
        {
            if (node2 == null)
            {
                return 0.0;
            }
            return node1.Item.CntPoint.DistanceTo(node2.Item.CntPoint);
        }
        public void MarkCoolPipe1(ThFanTreeNode<ThFanPointModel> node)
        {
            ThQueryDNService queryDNServiece = new ThQueryDNService();
            string strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString("f2") + ")";

            var curCoolPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.CoolFlow);//node.Item.CoolFlow.ToString();
            //当前结点热水管管径
            var curHotPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.HotFlow);//node.Item.HotFlow.ToString();
            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
            {
                //标记冷热水管
                CoolHotMark(node, curCoolPipe, curHotPipe, strMarkHeight);
            }
        }
        public void MarkCondPipe(ThFanTreeNode<ThFanPointModel> node)
        {
            //查询到直接连接的子结点
            var connectChild = FindConnectNode(node);
            connectChild = connectChild.OrderBy(o => DistanceTo(o, o.Parent)).ToList();
            MarkCondPipe1(connectChild.Last());
            foreach (var n in connectChild)
            {
                n.Item.IsCondMarked = true;
            }
        }
        public void MarkCondPipe1(ThFanTreeNode<ThFanPointModel> node)
        {
            ThQueryDNService queryDNServiece = new ThQueryDNService();
            //当前结点冷凝水管管径
            var curCondPipe = queryDNServiece.QueryCondPipeDN(node.Item.CoolCapa); //node.Item.CoolCapa.ToString();
                                                                                   //标记冷凝水管
            CondMark(node, curCondPipe);
        }
        public void UpdateMark(ThPointTreeModel tree, List<Entity> marks)
        {
            if (tree.RootNode.Children.Count == 0)
            {
                return;
            }
            UpdateNodeMark(tree.RootNode.Children.First(), ref marks);
            //遍历树
            BianLiTree(tree.RootNode, ref marks);
        }
        public void BianLiTree(ThFanTreeNode<ThFanPointModel> node, ref List<Entity> marks)
        {
            //遍历子结点
            foreach (var child in node.Children)
            {
                BianLiTree(child, ref marks);
            }
            if (node.Parent == null)
            {
                return;
            }
            if (node.Children.Count == 0)
            {
                UpdateNodeMark(node, ref marks);
            }
            else if (node.Children.Count == 2)
            {
                foreach (var child in node.Children)
                {
                    UpdateNodeMark(child, ref marks);
                }
            }

            ThQueryDNService queryDNServiece = new ThQueryDNService();
            //父结点冷水管管径
            var parentCoolPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Parent.Item.CoolFlow);
            //父结点热水管管径
            var parentHotPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Parent.Item.HotFlow);
            //父结点冷凝水管管径
            var parentCondPipe = queryDNServiece.QueryCondPipeDN(node.Parent.Item.CoolCapa);
            //当前结点冷水管管径
            var curCoolPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.CoolFlow);
            //当前结点热水管管径
            var curHotPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.HotFlow);
            //当前结点冷凝水管管径
            var curCondPipe = queryDNServiece.QueryCondPipeDN(node.Item.CoolCapa);

            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
            {
                string strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString("f2") + ")";
                //标记冷热水管
                if ((curCoolPipe != parentCoolPipe) || (curHotPipe != parentHotPipe))
                {
                    if (node.Parent.Parent != null)
                    {
                        UpdateCoolHotMark(node.Parent, ref marks, parentCoolPipe, parentHotPipe, strMarkHeight);
                    }
                    UpdateCoolHotMark(node, ref marks, curCoolPipe, curHotPipe, strMarkHeight);
                }
                else
                {
                    //判断是否有mark
                    //如果有，删除,否则不处理
                    if (node.Parent.Parent != null)
                    {
                        RemoveCoolHotMark(node.Parent, ref marks);
                    }
                    RemoveCoolHotMark(node, ref marks);
                }

                //标记冷凝水管
                if (curCondPipe != parentCondPipe)
                {
                    if (node.Parent.Parent != null)
                    {
                        UpdateCondMark(node.Parent, ref marks, parentCondPipe);
                    }
                    UpdateCondMark(node, ref marks, curCondPipe);
                }
                else
                {
                    //判断是否有mark
                    //如果有，删除,否则不处理
                    if (node.Parent.Parent != null)
                    {
                        RemoveCondMark(node.Parent, ref marks);
                    }
                    RemoveCondMark(node, ref marks);
                }
            }
            else if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
            {
                //标记冷凝水管
                if (curCondPipe != parentCondPipe)
                {
                    if (node.Parent.Parent != null)
                    {
                        UpdateCondMark(node.Parent, ref marks, parentCondPipe);
                    }
                    UpdateCondMark(node, ref marks, curCondPipe);
                }
                else
                {
                    //判断是否有mark
                    //如果有，删除,否则不处理
                    if (node.Parent.Parent != null)
                    {
                        RemoveCondMark(node.Parent, ref marks);
                    }
                    RemoveCondMark(node, ref marks);
                }
            }
        }
        public void UpdateNodeMark(ThFanTreeNode<ThFanPointModel> node, ref List<Entity> marks)
        {
            ThQueryDNService queryDNServiece = new ThQueryDNService();
            string strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString("f2") + ")";

            var curCoolPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.CoolFlow);
            //当前结点热水管管径
            var curHotPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.HotFlow);
            //当前结点冷凝水管管径
            var curCondPipe = queryDNServiece.QueryCondPipeDN(node.Item.CoolCapa);
            //标记冷热水管
            UpdateCoolHotMark(node, ref marks, curCoolPipe, curHotPipe, strMarkHeight);
            //标记冷凝水管
            UpdateCondMark(node, ref marks, curCondPipe);
        }
        public void UpdateCoolHotMark(ThFanTreeNode<ThFanPointModel> node, ref List<Entity> marks, string coolPipe, string hotPipe, string strMarkHeight)
        {
            if (!ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe || node.Item.IsCoolHotMarked)
            {
                return;
            }
            if (node.Parent == null)
            {
                return;
            }
            //查询到直接连接的子结点
            var connectChild = FindConnectNode(node);
            var coolHotMarkes = new List<Entity>();
            foreach (var n in connectChild)
            {
                if (n.Parent != null)
                {
                    var line = new Line(n.Item.CntPoint, n.Parent.Item.CntPoint);
                    coolHotMarkes.AddRange(FindMarkFromLine(line, ref marks));
                }
            }
            connectChild = connectChild.OrderBy(o => DistanceTo(o, o.Parent)).ToList();
            //判断该结点是否有mark
            if (coolHotMarkes.Count > 0)//如果有，判断是否可以使用，如果不能使用删除，重新生成
            {
                var firstMark = coolHotMarkes.First();
                coolHotMarkes.Remove(firstMark);
                UpdateMark(connectChild.Last(), firstMark as BlockReference, coolPipe, hotPipe, strMarkHeight);
                foreach (var mark in coolHotMarkes)
                {
                    mark.UpgradeOpen();
                    mark.Erase();
                    mark.DowngradeOpen();
                }
            }
            else//如果没有，新生成一个
            {
                NodeMark(node);
            }
            foreach (var n in connectChild)
            {
                n.Item.IsCoolHotMarked = true;
            }
        }
        public void UpdateCondMark(ThFanTreeNode<ThFanPointModel> node, ref List<Entity> marks, string condPipe)
        {
            if (!ConfigInfo.WaterSystemConfigInfo.IsCWPipe || node.Item.IsCondMarked)
            {
                return;
            }
            if (node.Parent == null)
            {
                return;
            }
            //判断该结点是否有text
            var connectChild = FindConnectNode(node);
            var condMarkes = new List<Entity>();
            foreach (var n in connectChild)
            {
                if (n.Parent != null)
                {
                    var line = new Line(n.Item.CntPoint, n.Parent.Item.CntPoint);
                    condMarkes.AddRange(FindTextFromLine(line, ref marks));
                }
            }
            connectChild = connectChild.OrderBy(o => DistanceTo(o, o.Parent)).ToList();
            if (condMarkes.Count > 0)//如果有，判断是否可以使用，如果不能使用删除，重新生成
            {
                var firstMark = condMarkes.First();
                condMarkes.Remove(firstMark);
                UpdateText(firstMark as DBText, condPipe);
                foreach (var mark in condMarkes)
                {
                    mark.UpgradeOpen();
                    mark.Erase();
                    mark.DowngradeOpen();
                }
            }
            else//如果没有，新生成一个
            {
                CondMark(connectChild.Last(), condPipe);
            }
            foreach (var n in connectChild)
            {
                n.Item.IsCondMarked = true;
            }
        }
        public void RemoveCoolHotMark(ThFanTreeNode<ThFanPointModel> node, ref List<Entity> marks)
        {
            if (!ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe || node.Item.IsCoolHotMarked)
            {
                return;
            }
            var line = new Line(node.Item.CntPoint, node.Parent.Item.CntPoint);
            var coolHotMarkes = FindMarkFromLine(line, ref marks);
            foreach (var mark in coolHotMarkes)
            {
                mark.UpgradeOpen();
                mark.Erase();
                mark.DowngradeOpen();
            }
        }
        public void RemoveCondMark(ThFanTreeNode<ThFanPointModel> node, ref List<Entity> marks)
        {
            if (!ConfigInfo.WaterSystemConfigInfo.IsCWPipe || node.Item.IsCondMarked)
            {
                return;
            }
            var line = new Line(node.Item.CntPoint, node.Parent.Item.CntPoint);
            var condMarkes = FindTextFromLine(line, ref marks);
            foreach (var mark in condMarkes)
            {
                mark.UpgradeOpen();
                mark.Erase();
                mark.DowngradeOpen();
            }
        }


        public void CreateMark(ThFanTreeNode<ThFanPointModelNew> node, List<ThFanTreeNode<ThFanPipeModel>> pipeTreeNodes)
        {
            var allNodes = node.GetDecendent();
            var coolHotNodes = new List<ThFanTreeNode<ThFanPointModelNew>>();
            var condNodes = new List<ThFanTreeNode<ThFanPointModelNew>>();

            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
            {
                if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
                {
                    //两管
                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                    {
                        coolHotNodes.AddRange(allNodes.Where(x => x.Item.IsCoolHotMaxChangeMark || x.Item.IsLevelChangeMark));
                    }

                }
                else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
                {
                    //四管
                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                    {
                        coolHotNodes.AddRange(allNodes.Where(x => x.Item.IsCoolHotChangeMark || x.Item.IsLevelChangeMark));
                    }
                }

            }

            if (ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
            {
                condNodes.AddRange(allNodes.Where(x => x.Item.IsCapaChangeMarked || x.Item.IsLevelChangeMark));

            }

            string strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString("f2") + ")";

            foreach (var n in coolHotNodes)
            {
                if (n.Item.IsFlag == -1)
                {
                    var markPt = n.Item.BasePt.GetMidPt(n.Parent.Item.BasePt);
                    n.Item.IsFlag = CheckNodeFlag(markPt, pipeTreeNodes);
                }
                CoolHotMarkNew(n, strMarkHeight);
            }

            foreach (var n in condNodes)
            {
                if (n.Item.IsFlag == -1)
                {
                    var markPt = n.Item.BasePt.GetMidPt(n.Parent.Item.BasePt);
                    n.Item.IsFlag = CheckNodeFlag(markPt, pipeTreeNodes);
                }
                CondMarkNew(n);
            }

        }

        private int CheckNodeFlag(Point3d basePt, List<ThFanTreeNode<ThFanPipeModel>> pipeTreeNodes)
        {
            var flag = 0;

            var selectpipeNode = pipeTreeNodes.Where(x => x.Item.PLine.GetClosestPointTo(basePt, false).DistanceTo(basePt) < ThFanConnectCommon.Tol_SamePoint);
            if (selectpipeNode.Count() > 0)
            {
                flag = selectpipeNode.First().Item.IsFlag == true ? 1 : 0;
            }

            return flag;
        }

        public void CoolHotMarkNew(ThFanTreeNode<ThFanPointModelNew> node, string strMarkHeight)//冷热水管标记
        {
            if (!ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
            {
                return;
            }
            if (node.Parent == null)
            {
                return;
            }
            //标记冷热水管
            ThFanToDBServiece toDbServerviece = new ThFanToDBServiece();
            var vector = node.Parent.Item.BasePt.GetVectorTo(node.Item.BasePt).GetNormal();
            var markAg = ThFanConnectUtils.GetVectorAngle(vector);
            var markPt = node.Item.BasePt.GetMidPt(node.Parent.Item.BasePt);
            var direct = new Vector3d(Math.Cos(markAg + Math.PI / 2.0), Math.Sin(markAg + Math.PI / 2.0), 0.0);

            if (node.Item.IsFlag == 1)
            {
                direct = -direct;
            }

            string blockName = "";
            List<string> property = new List<string>();
            if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
            {
                blockName = "AI-水管多排标注(2排)";
                var dim = node.Item.CoolDim >= node.Item.HotDim ? node.Item.CoolDim : node.Item.HotDim;

                string strchs = "CHS " + "DN" + dim + strMarkHeight;
                string strchr = "CHR " + "DN" + dim + strMarkHeight;
                property.Add(strchs);
                property.Add(strchr);

                if (node.Item.IsFlag == 0)
                {
                    markPt = markPt + direct * 300 * 1;
                }
                else
                {
                    property.Reverse();
                }

            }
            else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
            {
                blockName = "AI-水管多排标注(4排)";
                string strcs = "CS " + "DN" + node.Item.CoolDim + strMarkHeight;
                string strcr = "CR " + "DN" + node.Item.CoolDim + strMarkHeight;
                string strhs = "HS " + "DN" + node.Item.HotDim + strMarkHeight;
                string strhr = "HR " + "DN" + node.Item.HotDim + strMarkHeight;
                property.Add(strcs);
                property.Add(strcr);
                property.Add(strhs);
                property.Add(strhr);
                if (node.Item.IsFlag == 0)
                    markPt = markPt + direct * 300 * 2;
                else
                {
                    markPt = markPt - direct * 300 * 1;
                    property.Reverse();
                }
            }

            var tmpPt1 = node.Parent.Item.BasePt.TransformBy(Active.Editor.WCS2UCS());
            var tmpPt2 = node.Item.BasePt.TransformBy(Active.Editor.WCS2UCS());
            var tmpVector = tmpPt1.GetVectorTo(tmpPt2).GetNormal();
            var tmpAg = ThFanConnectUtils.GetVectorAngle(tmpVector);
            if (tmpAg > Math.PI / 2.0 && tmpAg <= Math.PI * 3.0 / 2.0)
            {
                markAg = markAg + Math.PI;
                property.Reverse();
                if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
                {
                    if (node.Item.IsFlag == 0)
                        markPt = markPt - direct * 300 * 1;
                    else
                        markPt = markPt + direct * 300 * 1;
                }
                else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
                {
                    if (node.Item.IsFlag == 0)
                        markPt = markPt - direct * 300 * 3;
                    else
                        markPt = markPt + direct * 300 * 3;
                }
            }
            toDbServerviece.InsertPipeMark("H-PIPE-DIMS", blockName, markPt, markAg, property);
        }
        public void CondMarkNew(ThFanTreeNode<ThFanPointModelNew> node)//冷凝水管标记
        {
            if (!ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
            {
                return;
            }
            if (node.Parent == null)
            {
                return;
            }
            ThFanToDBServiece toDbServerviece = new ThFanToDBServiece();
            var vector = node.Parent.Item.BasePt.GetVectorTo(node.Item.BasePt).GetNormal();
            var markAg = ThFanConnectUtils.GetVectorAngle(vector);
            var markPt = node.Item.BasePt.GetMidPt(node.Parent.Item.BasePt);
            var direct = new Vector3d(Math.Cos(markAg + Math.PI / 2.0), Math.Sin(markAg + Math.PI / 2.0), 0.0);
            if (node.Item.IsFlag == 1)
            {
                direct = -direct;
            }
            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
            {
                if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)//两管制
                {
                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                    {
                        markPt = markPt - direct * (300.0 * 1 + 140);
                    }
                    else
                    {
                        markPt = markPt - direct * 140;
                    }
                }
                else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)//四管制
                {
                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                    {
                        markPt = markPt - direct * (300.0 * 2 + 140);
                    }
                    else
                    {
                        markPt = markPt - direct * 140;
                    }
                }
            }
            else if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
            {
                if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                {
                    markPt = markPt - direct * (300.0 * 1 + 140);
                }
                else
                {
                    markPt = markPt - direct * 140;
                }
            }

            var tmpPt1 = node.Parent.Item.BasePt.TransformBy(Active.Editor.WCS2UCS());
            var tmpPt2 = node.Item.BasePt.TransformBy(Active.Editor.WCS2UCS());
            var tmpVector = tmpPt1.GetVectorTo(tmpPt2).GetNormal();
            var tmpAg = ThFanConnectUtils.GetVectorAngle(tmpVector);
            if (tmpAg > Math.PI / 2.0 && tmpAg <= Math.PI * 3.0 / 2.0)
            {
                markAg = markAg - Math.PI;
            }
            var strText = "C " + "DN" + node.Item.CoolCapaDim;
            toDbServerviece.InsertText("H-PIPE-DIMS", strText, markPt, markAg);
        }


    }
}
