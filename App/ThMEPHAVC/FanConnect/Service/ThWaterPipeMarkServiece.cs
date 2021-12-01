using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public void PipeMark(ThPointTreeModel tree)
        {
            ThQueryDNServiece queryDNServiece = new ThQueryDNServiece();
            ThFanToDBServiece toDbServerviece = new ThFanToDBServiece();
            if(tree.RootNode.Children.Count == 0)
            {
                return;
            }
            string strMarkHeight = " (h+" + ConfigInfo.WaterSystemConfigInfo.MarkHeigth.ToString() + ")";
            var node = tree.RootNode.Children.First();
            var curCoolPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.CoolFlow);
            //当前结点热水管管径
            var curHotPipe = queryDNServiece.QuerySupplyPipeDN(ConfigInfo.WaterSystemConfigInfo.FrictionCoeff, node.Item.HotFlow);
            //当前结点冷凝水管管径
            var curCondPipe = queryDNServiece.QueryCondPipeDN(node.Item.CoolCapa);
            if (ConfigInfo.WaterSystemConfigInfo.SystemType == 0)//水系统
            {
                var vector = node.Parent.Item.CntPoint.GetVectorTo(node.Item.CntPoint).GetNormal();
                var markAg = ThFanConnectUtils.GetVectorAngle(vector);
                //标记冷热水管
                if (node.Item.IsCoolHotMarked != true)
                {
                    var markPt = node.Item.CntPoint.GetMidPt(node.Parent.Item.CntPoint);
                    var direct = new Vector3d(Math.Cos(markAg + Math.PI / 2.0), Math.Sin(markAg + Math.PI / 2.0), 0.0);
                    string blockName = "";
                    List<string> property = new List<string>();
                    if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
                    {
                        markPt = markPt + direct * 200 * 1;
                        blockName = "AI-水管多排标注(2排)";
                        string strchs = "CHS " + curCoolPipe + strMarkHeight;
                        string strchr = "CHR " + curHotPipe + strMarkHeight;
                        property.Add(strchs);
                        property.Add(strchr);
                    }
                    else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
                    {
                        markPt = markPt + direct * 200 * 2;
                        blockName = "AI-水管多排标注(4排)";
                        string strcs = "CS " + curCoolPipe + strMarkHeight;
                        string strcr = "CR " + curCoolPipe + strMarkHeight;
                        string strhs = "HS " + curHotPipe + strMarkHeight;
                        string strhr = "HR " + curHotPipe + strMarkHeight;
                        property.Add(strcs);
                        property.Add(strcr);
                        property.Add(strhs);
                        property.Add(strhr);
                    }
                    toDbServerviece.InsertPipeMark("H-PIPE-DIMS", blockName, markPt, markAg, property);
                    node.Item.IsCoolHotMarked = true;
                }
                //标记冷凝水管
                if (node.Item.IsCondMarked != true)
                {
                    var markPt = node.Item.CntPoint.GetMidPt(node.Parent.Item.CntPoint);
                    var direct = new Vector3d(Math.Cos(markAg - Math.PI / 2.0), Math.Sin(markAg - Math.PI / 2.0), 0.0);
                    if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
                    {
                        markPt = markPt + direct * (200 * 1 + 120);
                    }
                    else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
                    {
                        markPt = markPt + direct * (200 * 2 + 120);
                    }
                    var strText = "C " + curCondPipe;
                    toDbServerviece.InsertText("H-PIPE-DIMS", strText, markPt, markAg);
                    node.Item.IsCondMarked = true;
                }
                
            }
            else if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
            {
                //标记冷凝水管
                var vector = node.Parent.Item.CntPoint.GetVectorTo(node.Item.CntPoint).GetNormal();
                var markAg = ThFanConnectUtils.GetVectorAngle(vector);
                if (node.Item.IsCondMarked != true)
                {
                    var markPt = node.Item.CntPoint.GetMidPt(node.Parent.Item.CntPoint);
                    var direct = new Vector3d(Math.Cos(markAg - Math.PI / 2.0), Math.Sin(markAg - Math.PI / 2.0), 0.0);
                    markPt = markPt + direct * (200 * 1 + 120);
                    var strText = "C " + curCondPipe;
                    toDbServerviece.InsertText("H-PIPE-DIMS", strText, markPt, markAg);
                    node.Item.IsCondMarked = true;
                }
            }

            //遍历树
            BianLiTree(tree.RootNode);
        }
        public void BianLiTree(ThFanTreeNode<ThFanPointModel> node)
        {
            foreach (var child in node.Children)
            {
                BianLiTree(child);
            }
            if (node.Parent == null)
            {
                return;
            }
            if(node.Children.Count <= 1)
            {
                return;
            }
            //遍历子结点

            ThQueryDNServiece queryDNServiece = new ThQueryDNServiece();
            ThFanToDBServiece toDbServerviece = new ThFanToDBServiece();

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
                var vector = node.Parent.Item.CntPoint.GetVectorTo(node.Item.CntPoint).GetNormal();
                var markAg = ThFanConnectUtils.GetVectorAngle(vector);
                //标记冷热水管
                if ((curCoolPipe != parentCoolPipe) || (curHotPipe != parentHotPipe))
                {
                    if (node.Parent.Parent != null)
                    {
                        if (!node.Parent.Item.IsCoolHotMarked)
                        {
                            var grandVector = node.Parent.Parent.Item.CntPoint.GetVectorTo(node.Parent.Item.CntPoint).GetNormal();
                            var grandMarkAg = ThFanConnectUtils.GetVectorAngle(grandVector);
                            var grandMarkPt = node.Parent.Item.CntPoint.GetMidPt(node.Parent.Parent.Item.CntPoint);
                            var grandDirect = new Vector3d(Math.Cos(grandMarkAg + Math.PI / 2.0), Math.Sin(grandMarkAg + Math.PI / 2.0), 0.0);
                            string grandBlockName = "";
                            List<string> property = new List<string>();
                            if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
                            {
                                grandMarkPt = grandMarkPt + grandDirect * 200 * 1;
                                grandBlockName = "AI-水管多排标注(2排)";
                                
                                string strchs = "CHS " + parentCoolPipe + strMarkHeight;
                                string strchr = "CHR " + parentHotPipe + strMarkHeight;
                                property.Add(strchs);
                                property.Add(strchr);
                            }
                            else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
                            {
                                grandMarkPt = grandMarkPt + grandDirect * 200 * 2;
                                grandBlockName = "AI-水管多排标注(4排)";
                                string strcs = "CS " + parentCoolPipe + strMarkHeight;
                                string strcr = "CR " + parentCoolPipe + strMarkHeight;
                                string strhs = "HS " + parentHotPipe + strMarkHeight;
                                string strhr = "HR " + parentHotPipe + strMarkHeight;
                                property.Add(strcs);
                                property.Add(strcr);
                                property.Add(strhs);
                                property.Add(strhr);
                            }
                            
                            toDbServerviece.InsertPipeMark("H-PIPE-DIMS", grandBlockName, grandMarkPt, grandMarkAg, property);
                            node.Parent.Item.IsCoolHotMarked = true;
                        }
                    }

                    if (node.Item.IsCoolHotMarked != true)
                    {
                        var markPt = node.Item.CntPoint.GetMidPt(node.Parent.Item.CntPoint);
                        var direct = new Vector3d(Math.Cos(markAg + Math.PI / 2.0), Math.Sin(markAg + Math.PI / 2.0), 0.0);
                        string blockName = "";
                        List<string> property = new List<string>();
                        if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
                        {
                            markPt = markPt + direct * 200 * 1;
                            blockName = "AI-水管多排标注(2排)";
                            string strchs = "CHS " + curCoolPipe + strMarkHeight;
                            string strchr = "CHR " + curHotPipe + strMarkHeight;
                            property.Add(strchs);
                            property.Add(strchr);
                        }
                        else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
                        {
                            markPt = markPt + direct * 200 * 2;
                            blockName = "AI-水管多排标注(4排)";
                            string strcs = "CS " + curCoolPipe + strMarkHeight;
                            string strcr = "CR " + curCoolPipe + strMarkHeight;
                            string strhs = "HS " + curHotPipe + strMarkHeight;
                            string strhr = "HR " + curHotPipe + strMarkHeight;
                            property.Add(strcs);
                            property.Add(strcr);
                            property.Add(strhs);
                            property.Add(strhr);
                        }
                        toDbServerviece.InsertPipeMark("H-PIPE-DIMS", blockName, markPt, markAg, property);
                        node.Item.IsCoolHotMarked = true;
                    }
                }

                //标记冷凝水管
                if(curCondPipe != parentCondPipe)
                {
                    if (node.Parent.Parent != null)
                    {
                        if (!node.Parent.Item.IsCondMarked)
                        {
                            var grandVector = node.Parent.Parent.Item.CntPoint.GetVectorTo(node.Parent.Item.CntPoint).GetNormal();
                            var grandMarkAg = ThFanConnectUtils.GetVectorAngle(grandVector);
                            var grandMarkPt = node.Parent.Item.CntPoint.GetMidPt(node.Parent.Parent.Item.CntPoint);
                            var grandDirect = new Vector3d(Math.Cos(grandMarkAg - Math.PI / 2.0), Math.Sin(grandMarkAg - Math.PI / 2.0), 0.0);
                            if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
                            {
                                grandMarkPt = grandMarkPt + grandDirect * (200 * 1 + 120);
                            }
                            else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
                            {
                                grandMarkPt = grandMarkPt + grandDirect * (200 * 2 + 120);
                            }
                            var strText = "C " + parentCondPipe;
                            toDbServerviece.InsertText("H-PIPE-DIMS", strText, grandMarkPt, markAg);
                            node.Parent.Item.IsCondMarked = true;
                        }
                    }
                    if (node.Item.IsCondMarked != true)
                    {
                        var markPt = node.Item.CntPoint.GetMidPt(node.Parent.Item.CntPoint);
                        var direct = new Vector3d(Math.Cos(markAg - Math.PI / 2.0), Math.Sin(markAg - Math.PI / 2.0), 0.0);
                        if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 0)
                        {
                            markPt = markPt + direct * (200 * 1 + 120);
                        }
                        else if (ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
                        {
                            markPt = markPt + direct * (200 * 2 + 120);
                        }
                        var strText = "C " + curCondPipe;
                        toDbServerviece.InsertText("H-PIPE-DIMS", strText, markPt, markAg);
                        node.Item.IsCondMarked = true;
                    }
                }
            }
            else if(ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
            {
                //标记冷凝水管
                if (curCondPipe != parentCondPipe)
                {
                    var vector = node.Parent.Item.CntPoint.GetVectorTo(node.Item.CntPoint).GetNormal();
                    var markAg = ThFanConnectUtils.GetVectorAngle(vector);
                    if (node.Parent.Parent != null)
                    {
                        if (!node.Parent.Item.IsCondMarked)
                        {
                            var grandVector = node.Parent.Parent.Item.CntPoint.GetVectorTo(node.Parent.Item.CntPoint).GetNormal();
                            var grandMarkAg = ThFanConnectUtils.GetVectorAngle(grandVector);
                            var grandMarkPt = node.Parent.Item.CntPoint.GetMidPt(node.Parent.Parent.Item.CntPoint);
                            var grandDirect = new Vector3d(Math.Cos(grandMarkAg - Math.PI / 2.0), Math.Sin(grandMarkAg - Math.PI / 2.0), 0.0);
                            grandMarkPt = grandMarkPt + grandDirect * (200 * 1 + 120);
                            var strText = "C " + parentCondPipe;
                            toDbServerviece.InsertText("H-PIPE-DIMS", strText, grandMarkPt, markAg);
                            node.Parent.Item.IsCondMarked = true;
                        }
                    }
                    if (node.Item.IsCondMarked != true)
                    {
                        var markPt = node.Item.CntPoint.GetMidPt(node.Parent.Item.CntPoint);
                        var direct = new Vector3d(Math.Cos(markAg - Math.PI / 2.0), Math.Sin(markAg - Math.PI / 2.0), 0.0);
                        markPt = markPt + direct * (200 * 1 + 120);
                        var strText = "C " + curCondPipe;
                        toDbServerviece.InsertText("H-PIPE-DIMS", strText, markPt, markAg);
                        node.Item.IsCondMarked = true;
                    }
                }
            }
        }
    }
}
