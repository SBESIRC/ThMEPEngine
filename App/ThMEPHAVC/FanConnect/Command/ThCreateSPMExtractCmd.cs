using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanConnect.Service;
using ThMEPHVAC.FanConnect.ViewModel;

namespace ThMEPHVAC.FanConnect.Command
{
    public class ThCreateSPMExtractCmd : ThMEPBaseCommand, IDisposable
    {
        public ThWaterPipeConfigInfo ConfigInfo { set; get; }//界面输入信息
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        public void ImportBlockFile()
        {
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))//引用模块的位置
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                if (blockDb.Blocks.Contains("AI-水管多排标注(4排)"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-水管多排标注(4排)"));
                }
                if (blockDb.Blocks.Contains("AI-水管多排标注(2排)"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-水管多排标注(2排)"));
                }
                if (blockDb.Blocks.Contains("AI-分歧管"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-分歧管"));
                }
                if (blockDb.Layers.Contains("H-PIPE-DIMS"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-DIMS"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-CS"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-CS"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-CR"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-CR"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-HS"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-HS"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-HR"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-HR"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-C"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-C"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-CHS"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-CHS"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-CHR"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-CHR"), true);
                }
                if (blockDb.Layers.Contains("H-PIPE-R"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-PIPE-R"), true);
                }
            }
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                DbHelper.EnsureLayerOn("H-PIPE-DIMS");
                DbHelper.EnsureLayerOn("H-PIPE-CS");
                DbHelper.EnsureLayerOn("H-PIPE-CR");
                DbHelper.EnsureLayerOn("H-PIPE-HS");
                DbHelper.EnsureLayerOn("H-PIPE-HR");
                DbHelper.EnsureLayerOn("H-PIPE-C");
                DbHelper.EnsureLayerOn("H-PIPE-CHS");
                DbHelper.EnsureLayerOn("H-PIPE-CHR");
                DbHelper.EnsureLayerOn("H-PIPE-R");
            }
        }
        public override void SubExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var database = AcadDatabase.Active())
            {
                ImportBlockFile();
                //选择起点
                var startPt = ThFanConnectUtils.SelectPoint();
                //提取水管路由
                var pipes = ThEquipElementExtractServiece.GetFanPipes();
                //提取水管连接点
                var fcus = ThEquipElementExtractServiece.GetFCUModels();
                //处理pipes 1.清除重复线段 ；2.将同线的线段连接起来；
                var lines = ThFanConnectUtils.CleanLaneLines(pipes);
                double space = 200.0;
                if(ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
                {
                    space = 100.0;
                }
                //构建Tree
                ThFanTreeModel treeModel = new ThFanTreeModel(startPt, lines, space);
                if (treeModel.RootNode == null)
                {
                    return;
                }
                //标记4通结点
                FindFourWay(treeModel.RootNode);
                //
                foreach (var fcu in fcus)
                {
                    FindFcuNode(treeModel.RootNode, fcu.FanPoint);
                }
                //扩展管路
                ThWaterPipeExtendServiece pipeExtendServiece = new ThWaterPipeExtendServiece();
                pipeExtendServiece.ConfigInfo = ConfigInfo;
                pipeExtendServiece.PipeExtend(treeModel);
                //计算流量
                ThPointTreeModel pointTreeModel = new ThPointTreeModel(treeModel.RootNode, fcus);
                if(pointTreeModel.RootNode == null)
                {
                    return;
                }
                //标记流量
                ThWaterPipeMarkServiece pipeMarkServiece = new ThWaterPipeMarkServiece();
                pipeMarkServiece.ConfigInfo = ConfigInfo;
                pipeMarkServiece.PipeMark(pointTreeModel);
            }
        }
        public void FindFourWay(ThFanTreeNode<ThFanPipeModel> node)
        {
            foreach (var item in node.Children)
            {
                FindFourWay(item);
            }

            if(node.Children.Count <= 1)
            {
                return;
            }
            var connectChild = node.Children.Where(o => o.Item.IsConnect).ToList();
            var nonConnectChild = node.Children.Where(o => !o.Item.IsConnect).ToList();
            if (connectChild.Count == 2)
            {
                connectChild[0].Item.WayCount = 3;
                connectChild[0].Item.BrotherItem = connectChild[1].Item;
                connectChild[1].Item.WayCount = 3;
                connectChild[1].Item.BrotherItem = connectChild[0].Item;
            }
            for(int i = 0; i < nonConnectChild.Count;i++)
            {
                for (int j = 0; j < nonConnectChild.Count; j++)
                {
                    if(i != j)
                    {
                        if(nonConnectChild[i].Item.PLine.StartPoint.IsEqualTo(nonConnectChild[j].Item.PLine.StartPoint))
                        {
                            nonConnectChild[i].Item.BrotherItem = nonConnectChild[j].Item;
                            nonConnectChild[i].Item.WayCount = 4;
                        }
                    }
                }
            }

        }
        public void FindFcuNode(ThFanTreeNode<ThFanPipeModel> node, Point3d pt)
        {
            var box = node.Item.PLine.ExtendLine(10).Buffer(10);

            if(box.Contains(pt))
            {
                node.Item.PipeWidth = 100.0;
                node.Item.PipeLevel = PIPELEVEL.LEVEL3;
                return;
            }

            foreach(var item in node.Children)
            {
                FindFcuNode(item, pt);
            }
        }
    }
}
