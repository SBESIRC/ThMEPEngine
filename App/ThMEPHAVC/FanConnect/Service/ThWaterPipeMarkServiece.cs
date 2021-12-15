using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
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
    public class ThWaterPipeMarkServiece
    {
        public ThWaterPipeConfigInfo ConfigInfo { set; get; }//界面输入信息
        //冷热水管标记
        public void CoolHotMark(ThFanTreeNode<ThFanPointModel> node, string coolPipe,string hotPipe, string strMarkHeight)
        {
            if (!ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
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

            //标记冷热水管
            if (!node.Item.IsCoolHotMarked)
            {
                var markPt = node.Item.CntPoint.GetMidPt(node.Parent.Item.CntPoint);
                
                var direct = new Vector3d(Math.Cos(markAg + Math.PI / 2.0), Math.Sin(markAg + Math.PI / 2.0), 0.0);
                if(node.Item.IsFlag)
                {
                    direct = new Vector3d(Math.Cos(markAg - Math.PI / 2.0), Math.Sin(markAg - Math.PI / 2.0), 0.0);
                }
                string blockName = "";
                List<string> property = new List<string>();
                if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
                {

                    blockName = "AI-水管多排标注(2排)";
                    string strchs = "CHS " + coolPipe + strMarkHeight;
                    string strchr = "CHR " + hotPipe + strMarkHeight;
                    property.Add(strchs);
                    property.Add(strchr);
                    if (!node.Item.IsFlag)
                    {
                        markPt = markPt + direct * 200 * 1;
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
                        markPt = markPt + direct * 200 * 2;
                    else
                    {
                        markPt = markPt - direct * 200 * 1;
                        property.Reverse();
                    }
                }

                if ( (markAg > Math.PI / 2.0 && markAg <= Math.PI) || (markAg > Math.PI && markAg <= Math.PI * 3.0 / 2.0))
                {
                    markAg = markAg + Math.PI;
                    property.Reverse();
                    if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
                    {
                        if (!node.Item.IsFlag)
                            markPt = markPt - direct * 200 * 1;
                        else
                            markPt = markPt + direct * 200 * 1;
                    }
                    else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
                    {
                        if (!node.Item.IsFlag)
                            markPt = markPt - direct * 200 * 3;
                        else
                            markPt = markPt + direct * 200 * 3;
                    }
                }
                toDbServerviece.InsertPipeMark("H-PIPE-DIMS", blockName, markPt, markAg, property);
                SetIsCoolHotMarked1(node);
                SetIsCoolHotMarked2(node);
            }
        }
        //冷凝水管标记
        public void CondMark(ThFanTreeNode<ThFanPointModel> node, string condPipe)
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
            var vector = node.Parent.Item.CntPoint.GetVectorTo(node.Item.CntPoint).GetNormal();
            var markAg = ThFanConnectUtils.GetVectorAngle(vector);
            if (markAg > Math.PI / 2.0 && markAg <= Math.PI)
            {
                markAg = markAg + Math.PI;
            }
            else if (markAg > Math.PI && markAg <= Math.PI * 3.0 / 2.0)
            {
                markAg = markAg - Math.PI;
            }
            if (node.Item.IsCondMarked != true)
            {
                var markPt = node.Item.CntPoint.GetMidPt(node.Parent.Item.CntPoint);
                var direct = new Vector3d(Math.Cos(markAg + Math.PI / 2.0), Math.Sin(markAg + Math.PI / 2.0), 0.0);
                if(ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
                {
                    if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)//两管制
                    {
                        if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                        {
                            markPt = markPt + direct * (200 * 1 + 120);
                        }
                        else
                        {
                            markPt = markPt + direct * 120;
                        }
                    }
                    else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)//四管制
                    {
                        if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                        {
                            markPt = markPt + direct * (200 * 2 + 120);
                        }
                        else
                        {
                            markPt = markPt + direct * 120;
                        }
                    }
                }
                else if(ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
                {
                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                    {
                        markPt = markPt + direct * (200 * 1 + 120);
                    }
                    else
                    {
                        markPt = markPt + direct * 120;
                    }
                }

                var strText = "C " + condPipe;
                toDbServerviece.InsertText("H-PIPE-DIMS", strText, markPt, markAg);
                SetIsCondMarked1(node);
                SetIsCondMarked2(node);
            }
        }
        public void NodeMark(ThFanTreeNode<ThFanPointModel> node)
        {
            ThQueryDNServiece queryDNServiece = new ThQueryDNServiece();
            string strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString() + ")";

            var curCoolPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.CoolFlow);
            //当前结点热水管管径
            var curHotPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.HotFlow);
            //当前结点冷凝水管管径
            var curCondPipe = queryDNServiece.QueryCondPipeDN(node.Item.CoolCapa);
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
        public void PipeMark(ThPointTreeModel tree)
        {
            if(tree.RootNode.Children.Count == 0)
            {
                return;
            }
            NodeMark(tree.RootNode.Children.First());
            //遍历树
            BianLiTree(tree.RootNode);
        }
        public void BianLiTree(ThFanTreeNode<ThFanPointModel> node)
        {
            if (node.Item.Level == PIPELEVEL.LEVEL3 || node.Item.Level == PIPELEVEL.LEVEL4)
            {
                return;
            }
            //遍历子结点
            foreach (var child in node.Children)
            {
                BianLiTree(child);
            }
            if (node.Parent == null)
            {
                return;
            }
            //if(node.Children.Count <= 1)
            //{
            //    return;
            //}
            ThQueryDNServiece queryDNServiece = new ThQueryDNServiece();
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
                string strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString() + ")";
                //标记冷热水管
                if ((curCoolPipe != parentCoolPipe) || (curHotPipe != parentHotPipe))
                {
                    if (node.Parent.Parent != null)
                    {
                        CoolHotMark(node.Parent, parentCoolPipe, parentHotPipe, strMarkHeight);
                    }
                    CoolHotMark(node, curCoolPipe, curHotPipe, strMarkHeight);
                }
                //标记冷凝水管
                if (curCondPipe != parentCondPipe)
                {
                    if (node.Parent.Parent != null)
                    {
                        CondMark(node.Parent, parentCondPipe);
                    }
                    CondMark(node, curCondPipe);
                }
            }
            else if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
            {
                //标记冷凝水管
                if (curCondPipe != parentCondPipe)
                {
                    if (node.Parent.Parent != null)
                    {
                        CondMark(node.Parent, parentCondPipe);
                    }
                    CondMark(node, curCondPipe);
                }
            }
        }
        public void UpdateMark(ThPointTreeModel tree,List<Entity> marks)
        {
            if (tree.RootNode.Children.Count == 0)
            {
                return;
            }
            NodeMark(tree.RootNode.Children.First(),ref marks);
            //遍历树
            BianLiTree(tree.RootNode,ref marks);
        }
        public void NodeMark(ThFanTreeNode<ThFanPointModel> node,ref List<Entity> marks)
        {
            ThQueryDNServiece queryDNServiece = new ThQueryDNServiece();
            string strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString() + ")";

            var curCoolPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.CoolFlow);
            //当前结点热水管管径
            var curHotPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.HotFlow);
            //当前结点冷凝水管管径
            var curCondPipe = queryDNServiece.QueryCondPipeDN(node.Item.CoolCapa);
            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
            {
                //标记冷热水管
                CoolHotMark(node, ref marks, curCoolPipe, curHotPipe, strMarkHeight);
                //标记冷凝水管
                CondMark(node, ref marks, curCondPipe);
            }
            else if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
            {
                //标记冷凝水管
                CondMark(node, ref marks, curCondPipe);
            }
        }
        public void BianLiTree(ThFanTreeNode<ThFanPointModel> node,ref List<Entity> marks)
        {
            //遍历子结点
            foreach (var child in node.Children)
            {
                BianLiTree(child ,ref marks);
            }
            if (node.Parent == null)
            {
                return;
            }
            if (node.Children.Count <= 1)
            {
                return;
            }
            ThQueryDNServiece queryDNServiece = new ThQueryDNServiece();
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
                string strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString() + ")";
                //标记冷热水管
                if ((curCoolPipe != parentCoolPipe) || (curHotPipe != parentHotPipe))
                {
                    if (node.Parent.Parent != null)
                    {
                        CoolHotMark(node.Parent,ref marks, parentCoolPipe, parentHotPipe, strMarkHeight);
                    }
                    CoolHotMark(node, ref marks, curCoolPipe, curHotPipe, strMarkHeight);
                }
                else
                {
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
                        CondMark(node.Parent, ref marks, parentCondPipe);
                    }
                    CondMark(node, ref marks, curCondPipe);
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
                        CondMark(node.Parent, ref marks, parentCondPipe);
                    }
                    CondMark(node, ref marks, curCondPipe);
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
        public void CoolHotMark(ThFanTreeNode<ThFanPointModel> node,ref List<Entity> marks, string coolPipe, string hotPipe, string strMarkHeight)
        {
            if (!ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
            {
                return;
            }
            if (node.Parent == null)
            {
                return;
            }
            //判断该结点是否有mark
            var line = new Line(node.Item.CntPoint, node.Parent.Item.CntPoint);
            var coolHotMarkes = FindMarkFromLine(line,ref marks);
            if (coolHotMarkes.Count > 0)//如果有，判断是否可以使用，如果不能使用删除，重新生成
            {
                var firstMark = coolHotMarkes.First();
                coolHotMarkes.Remove(firstMark);
                UpdateMark(node,firstMark as BlockReference, coolPipe, hotPipe, strMarkHeight);
                foreach (var mark in coolHotMarkes)
                {
                    mark.UpgradeOpen();
                    mark.Erase();
                    mark.DowngradeOpen();
                }
            }
            else//如果没有，新生成一个
            {
                CoolHotMark(node, coolPipe, hotPipe, strMarkHeight);
            }
            node.Item.IsCoolHotMarked = true;
        }
        public void CondMark(ThFanTreeNode<ThFanPointModel> node,ref List<Entity> marks, string condPipe)
        {
            if (!ConfigInfo.WaterSystemConfigInfo.IsCWPipe)
            {
                return;
            }
            if (node.Parent == null)
            {
                return;
            }
            //判断该结点是否有text
            var line = new Line(node.Item.CntPoint, node.Parent.Item.CntPoint);
            var condMarkes = FindTextFromLine(line,ref marks);
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
                CondMark(node, condPipe);
            }
            node.Item.IsCondMarked = true;
        }
        public void RemoveCoolHotMark(ThFanTreeNode<ThFanPointModel> node,ref List<Entity> marks)
        {
            var line = new Line(node.Item.CntPoint, node.Parent.Item.CntPoint);
            var coolHotMarkes = FindMarkFromLine(line,ref marks);
            foreach (var mark in coolHotMarkes)
            {
                mark.UpgradeOpen();
                mark.Erase();
                mark.DowngradeOpen();
            }
        }
        public void RemoveCondMark(ThFanTreeNode<ThFanPointModel> node,ref List<Entity> marks)
        {
            var line = new Line(node.Item.CntPoint, node.Parent.Item.CntPoint);
            var condMarkes = FindTextFromLine(line,ref marks);
            foreach (var mark in condMarkes)
            {
                mark.UpgradeOpen();
                mark.Erase();
                mark.DowngradeOpen();
            }
        }
        public List<Entity> FindMarkFromLine(Line line ,ref List<Entity> marks)
        {
            var box = line.Buffer(450);
            var retMark = new List<Entity>();
            foreach(var mark in marks)
            {
                if(mark is BlockReference)
                {
                    var blk = mark as BlockReference;
                    if(box.Contains(blk.Position))
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
        public List<Entity> FindTextFromLine(Line line,ref List<Entity> marks)
        {
            var box = line.Buffer(600);
            var retText = new List<Entity>();
            foreach (var mark in marks)
            {
                if (mark is DBText)
                {
                    var text = mark as DBText;
                    if (box.Contains(text.Position))
                    {
                        retText.Add(mark);
                    }
                }
            }
            marks = marks.Except(retText).ToList();
            return retText;
        }
        public void UpdateMark(ThFanTreeNode<ThFanPointModel> node, BlockReference blk, string coolPipe, string hotPipe, string strMarkHeight)
        {
            var effName = blk.GetEffectiveName();
            if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
            {
                if(effName == "AI-水管多排标注(2排)")
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
        public void UpdateText(DBText text, string condPipe)
        {
            var strText = "C " + condPipe;
            text.UpgradeOpen();
            text.TextString = strText;
            text.DowngradeOpen();
        }
        public void PipeMarkTest(ThPointTreeModel tree)
        {
            if (tree.RootNode.Children.Count == 0)
            {
                return;
            }
            NodeMarkTest(tree.RootNode.Children.First());
            //遍历树
            BianLiTreeTest(tree.RootNode);
        }
        public void CondMarkTest(ThFanTreeNode<ThFanPointModel> node, string condPipe)
        {
            ThFanToDBServiece toDbServerviece = new ThFanToDBServiece();
            var vector = node.Parent.Item.CntPoint.GetVectorTo(node.Item.CntPoint).GetNormal();
            var markAg = ThFanConnectUtils.GetVectorAngle(vector);
            if (markAg > Math.PI / 2.0 && markAg <= Math.PI)
            {
                markAg = markAg + Math.PI;
            }
            else if (markAg > Math.PI && markAg <= Math.PI * 3.0 / 2.0)
            {
                markAg = markAg - Math.PI;
            }
            var markPt = node.Item.CntPoint.GetMidPt(node.Parent.Item.CntPoint);
            var strText = condPipe;
            toDbServerviece.InsertText("H-PIPE-DIMS", strText, markPt, markAg);
        }
        public void CoolHotMarkTest(ThFanTreeNode<ThFanPointModel> node, string coolPipe, string hotPipe, string strMarkHeight)
        {
            if(node.Item.IsCoolHotMarked)
            {
                return;
            }
            ThFanToDBServiece toDbServerviece = new ThFanToDBServiece();
            var vector = node.Parent.Item.CntPoint.GetVectorTo(node.Item.CntPoint).GetNormal();
            var markAg = ThFanConnectUtils.GetVectorAngle(vector);
            if (markAg > Math.PI / 2.0 && markAg <= Math.PI)
            {
                markAg = markAg + Math.PI;
            }
            else if (markAg > Math.PI && markAg <= Math.PI * 3.0 / 2.0)
            {
                markAg = markAg - Math.PI;
            }
            var markPt = node.Item.CntPoint.GetMidPt(node.Parent.Item.CntPoint);
            var strText ="冷水:"+ coolPipe + ";热水:"+ hotPipe;
            toDbServerviece.InsertText("H-PIPE-DIMS", strText, markPt, markAg);
            SetIsCoolHotMarked1(node);
            SetIsCoolHotMarked2(node);
        }
        public void NodeMarkTest(ThFanTreeNode<ThFanPointModel> node)
        {
            ThQueryDNServiece queryDNServiece = new ThQueryDNServiece();
            string strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString() + ")";

            var curCoolPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.CoolFlow);//node.Item.CoolFlow.ToString();
            //当前结点热水管管径
            var curHotPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.HotFlow);//node.Item.HotFlow.ToString();
            //当前结点冷凝水管管径
            var curCondPipe = queryDNServiece.QueryCondPipeDN(node.Item.CoolCapa); //node.Item.CoolCapa.ToString();
            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
            {
                //标记冷热水管
                CoolHotMarkTest(node, curCoolPipe, curHotPipe, strMarkHeight);
                //标记冷凝水管
            //    CondMarkTest(node, curCondPipe);
            }
            else if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
            {
                //标记冷凝水管
                CondMarkTest(node, curCondPipe);
            }
            
        }
        public void BianLiTreeTest(ThFanTreeNode<ThFanPointModel> node)
        {
            //遍历子结点
            foreach (var child in node.Children)
            {
                BianLiTreeTest(child);
            }
            if (node.Parent == null)
            {
                return;
            }
            ThQueryDNServiece queryDNServiece = new ThQueryDNServiece();
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

            if ((curCoolPipe != parentCoolPipe) || (curHotPipe != parentHotPipe))
            {
                if (node.Parent.Parent != null)
                {
                    NodeMarkTest(node.Parent);
                }
                NodeMarkTest(node);
            }
        }

        public void SetIsCoolHotMarked1(ThFanTreeNode<ThFanPointModel> node)
        {
            node.Item.IsCoolHotMarked = true;
            if(node.Children.Count == 1)
            {
                SetIsCoolHotMarked1(node.Children[0]);
            }

        }
        public void SetIsCoolHotMarked2(ThFanTreeNode<ThFanPointModel> node)
        {
            node.Item.IsCoolHotMarked = true;
            if (node.Parent != null)
            {
                if (node.Parent.Children.Count == 1)
                {
                    SetIsCoolHotMarked2(node.Parent);
                }
            }
        }

        public void SetIsCondMarked1(ThFanTreeNode<ThFanPointModel> node)
        {
            node.Item.IsCondMarked = true;
            if (node.Children.Count == 1)
            {
                SetIsCondMarked1(node.Children[0]);
            }
        }
        public void SetIsCondMarked2(ThFanTreeNode<ThFanPointModel> node)
        {
            node.Item.IsCondMarked = true;
            if (node.Parent != null)
            {
                if (node.Parent.Children.Count == 1)
                {
                    SetIsCondMarked2(node.Parent);
                }
            }
        }
    }
}
