using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Model;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.Pipe;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.WaterWellPumpLayout.Model;
using ThMEPWSS.WaterWellPumpLayout.Service;

namespace ThMEPWSS.Command
{
    public class FormItem
    {
        public string StrNumber;//编号
        public string StrLength;//长
        public string StrWidth;//宽
        public string StrPumpCount;//井内泵台数
        public string StrWellCount;//井数目
        public string StrPumpConfig;//可见性，水泵配置
    }
    public class ThCreateWithdrawalFormCmd : ThMEPBaseCommand, IDisposable
    {
        WaterWellPumpConfigInfo configInfo;//配置信息
        public ObservableCollection<ThWaterWellConfigInfo> WellConfigInfo { set; get; }

        public void Dispose()
        {

        }
        public string WaterWellBlockFilePath
        {
            get
            {
                var path = ThCADCommon.WSSDwgPath();
                return path;
            }
        }
        WaterwellPumpParamsViewModel _vm;
        public ThCreateWithdrawalFormCmd(WaterwellPumpParamsViewModel vm)
        {
            _vm = vm;
            CommandName = "THSJSB";
            ActionName = "生成提资表";
            configInfo = vm.GetConfigInfo();
        }

        private void ImportBlockFile()
        {
            //导入一个块
            using (AcadDatabase blockDb = AcadDatabase.Open(WaterWellBlockFilePath, DwgOpenMode.ReadOnly, false))//引用模块的位置
            using (var locked = Active.Document.LockDocument())
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterWellBlockNames.WaterWellTableHeader), true);
                acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterWellBlockNames.WaterWellTableBody), true);
                acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("W-NOTE"), true);
            }
        }

        public override void SubExecute()
        {
            try
            {
                ThMEPWSS.Common.Utils.FocusMainWindow();
                ImportBlockFile();

                //获取选择区域
                var point = Active.Editor.GetPoint("\n选择要插入的基点位置");
                if (point.Status != PromptStatus.OK)
                {
                    return;
                }

                //整理数据，合并，统计等操作
                List<FormItem> formItmes = new List<FormItem>();

                var wellList = WellConfigInfo.SelectMany(x => x.WellModelList).ToList();

                var localWellConfig = ThWaterWellPumpUtils.MergeWellList(wellList, false);

                foreach (var wellConfig in localWellConfig)
                {
                    if (wellConfig.PumpCount == "0")
                    {
                        continue;
                    }
                    var tmpItem = new FormItem();
                    tmpItem.StrNumber = wellConfig.PumpNumber;
                    tmpItem.StrLength = (wellConfig.WellModelList.FirstOrDefault().Length - 100).ToString();
                    tmpItem.StrWidth = (wellConfig.WellModelList.FirstOrDefault().Width - 100).ToString();
                    tmpItem.StrPumpCount = wellConfig.PumpCount;
                    tmpItem.StrWellCount = wellConfig.WellCount.ToString();

                    if (tmpItem.StrPumpCount == "1")
                    {
                        tmpItem.StrPumpConfig = "一用";
                    }
                    else if (tmpItem.StrPumpCount == "2")
                    {
                        tmpItem.StrPumpConfig = "一用一备";
                    }
                    else if (tmpItem.StrPumpCount == "3")
                    {
                        tmpItem.StrPumpConfig = "两用一备";
                    }
                    else if (tmpItem.StrPumpCount == "4")
                    {
                        tmpItem.StrPumpConfig = "三用一备";
                    }
                    else
                    {
                        tmpItem.StrPumpConfig = "一用";
                    }

                    formItmes.Add(tmpItem);
                }

                //选择一个点，再生成提资表
                using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (AcadDatabase acadDb = AcadDatabase.Active())
                {
                    //插入表头
                    Point3d position = point.Value.TransformBy(Active.Editor.UCS2WCS());
                    acadDb.ModelSpace.ObjectId.InsertBlockReference("W-NOTE", WaterWellBlockNames.WaterWellTableHeader, position, new Scale3d(1, 1, 1), 0);
                    //插入表身
                    Vector3d vector = new Vector3d(0, -1, 0);
                    position += vector * 2000;
                    for (int i = 0; i < formItmes.Count; i++)
                    {
                        var item = formItmes[i];
                        var dic = new Dictionary<string, string>();
                        dic.Add("集水井编号", item.StrNumber);
                        dic.Add("位置", "普通车库");
                        dic.Add("长", item.StrLength);
                        dic.Add("宽", item.StrWidth);
                        dic.Add("深", "1500");
                        dic.Add("流量", "xx");
                        dic.Add("扬程", "xx");
                        dic.Add("电量", "xx");
                        dic.Add("井内水泵台数", item.StrPumpCount);
                        dic.Add("数量统计", item.StrWellCount);
                        var bodyId = acadDb.ModelSpace.ObjectId.InsertBlockReference("W-NOTE", WaterWellBlockNames.WaterWellTableBody, position, new Scale3d(1, 1, 1), 0, dic);
                        bodyId.SetDynBlockValue("水泵配置", item.StrPumpConfig);
                        position += vector * 1000;
                    }
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }
    }
}
