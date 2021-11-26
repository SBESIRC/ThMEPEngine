using Autodesk.AutoCAD.Geometry;
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

        public override void SubExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var database = AcadDatabase.Active())
            {
                //选择起点
                var startPt = ThFanConnectUtils.SelectPoint();
                //提取水管路由
                var pipes = ThEquipElementExtractServiece.GetFanPipes();
                //提取水管连接点
                var fcus = ThEquipElementExtractServiece.GetFCUModels();
                //处理pipes 1.清除重复线段 ；2.将同线的线段连接起来；
                var lines = ThFanConnectUtils.CleanLaneLines(pipes);
                //构建Tree
                ThFanTreeModel treeModel = new ThFanTreeModel(startPt, lines);
                if (treeModel.RootNode == null)
                {
                    return;
                }
                //
                foreach(var fcu in fcus)
                {
                    FindFcuNode(treeModel.RootNode,fcu.FanPoint);
                }
                //扩展管路
                ThWaterPipeExtendServiece pipeExtendServiece = new ThWaterPipeExtendServiece();
                pipeExtendServiece.ConfigInfo = ConfigInfo;
                pipeExtendServiece.PipeExtend(treeModel);
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
