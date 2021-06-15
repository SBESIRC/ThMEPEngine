using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.Pipe;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Command
{
    public class FormItem
    {
        public string StrNumber;//编号
        public string StrFloor;//楼层
        public string StrPostion;//位置
        public string StrParameter;//参数
        public string StrPumpCount;//井内泵台数
        public string StrPumpSum;//泵总数
    }
    public class ThCreateWithdrawalFormCmd : IAcadCommand, IDisposable
    {
        WaterWellPumpConfigInfo configInfo;//配置信息
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
            configInfo = vm.GetConfigInfo();
        }
        public void ImportBlockFile()
        {
            //导入一个块
            using (AcadDatabase blockDb = AcadDatabase.Open(WaterWellBlockFilePath, DwgOpenMode.ReadOnly, false))//引用模块的位置
            using(var locked = Active.Document.LockDocument())
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                if (!acadDb.Blocks.Contains(WaterWellBlockNames.WaterWellTableHeader) && blockDb.Blocks.Contains(WaterWellBlockNames.WaterWellTableHeader))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterWellBlockNames.WaterWellTableHeader));
                }
                if (!acadDb.Blocks.Contains(WaterWellBlockNames.WaterWellTableBody) && blockDb.Blocks.Contains(WaterWellBlockNames.WaterWellTableBody))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(WaterWellBlockNames.WaterWellTableBody));
                }
                if(!acadDb.Layers.Contains("W-NOTE") && blockDb.Layers.Contains("W-NOTE"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("W-NOTE"));
                }
            }
        }
        public List<ThWWaterWell> GetWaterWellEntityList(Tuple<Point3d, Point3d> input)
        {
            List<ThWWaterWell> waterWellList = new List<ThWWaterWell>();
            using (var database = AcadDatabase.Active())
            using (var waterwellEngine = new ThWWaterWellRecognitionEngine(configInfo.WaterWellInfo.identifyInfo))
            {
                var range = new Point3dCollection();
                range.Add(input.Item1);
                range.Add(new Point3d(input.Item1.X, input.Item2.Y, 0));
                range.Add(input.Item2);
                range.Add(new Point3d(input.Item2.X, input.Item1.Y, 0));
                waterwellEngine.Recognize(database.Database, range);
                foreach (var element in waterwellEngine.Datas)
                {
                    ThWWaterWell waterWell = ThWWaterWell.Create(element);
                    waterWell.Init();
                    waterWellList.Add(waterWell);
                }
            }
            return waterWellList;
        }
        public List<ThWDeepWellPump> GetDeepWellPumpList()
        {
            List<ThWDeepWellPump> deepWellPump = new List<ThWDeepWellPump>();
            using (var database = AcadDatabase.Active())
            using (var engine = new ThWDeepWellPumpEngine())
            {
                var range = new Point3dCollection();
                engine.RecognizeMS(database.Database, range);
                foreach (ThIfcDistributionFlowElement element in engine.Elements)
                {
                    ThWDeepWellPump pump = ThWDeepWellPump.Create(element.Outline.ObjectId);
                    deepWellPump.Add(pump);
                }
            }
            return deepWellPump;
        }
        public void Execute()
        {
            try
            {
                ThMEPWSS.Common.Utils.FocusMainWindow();
                ImportBlockFile();
                //获取选择区域
                var input = ThWGeUtils.SelectPoints();
                if (input.Item1.IsEqualTo(input.Item2))
                {
                    return;
                }
                //获取集水井
                var water_well_entity_list = GetWaterWellEntityList(input);
                if (water_well_entity_list.Count == 0)
                {
                    //命令栏提示“未选中集水井”
                    //退出本次布置动作
                    return;
                }
                //获取潜水泵
                List<ThWDeepWellPump> pumpList = GetDeepWellPumpList();

                var pumpDictionary = new Dictionary<string, List<ThWDeepWellPump>>();
                foreach (ThWWaterWell waterWell in water_well_entity_list)
                {
                    //计算集水井是否包含潜水泵
                    foreach (ThWDeepWellPump pump in pumpList)
                    {
                        if (waterWell.ContainPump(pump))
                        {
                            string key = pump.GetName();
                            if (pumpDictionary.ContainsKey(key))
                            {
                                pumpDictionary[key].Add(pump);
                            }
                            else
                            {
                                List<ThWDeepWellPump> tmpPump = new List<ThWDeepWellPump>();
                                tmpPump.Add(pump);
                                pumpDictionary.Add(key, tmpPump);
                            }
                            break;
                        }
                    }
                }
                //整理数据，合并，统计等操作
                List<FormItem> formItmes = new List<FormItem>();
                pumpDictionary.ForEach(o =>
                {
                    var group = o.Value.Select(v => v.GetPumpCount()).GroupBy(v => v);
                    foreach (var g in group)
                    {
                        FormItem tmpItem = new FormItem();
                        tmpItem.StrNumber = o.Key;
                        if (tmpItem.StrNumber.Contains("1"))
                        {
                            tmpItem.StrFloor = "B1F";
                        }
                        else if (tmpItem.StrNumber.Contains("2"))
                        {
                            tmpItem.StrFloor = "B2F";
                        }
                        else if (tmpItem.StrNumber.Contains("3"))
                        {
                            tmpItem.StrFloor = "B3F";
                        }
                        else if (tmpItem.StrNumber.Contains("4"))
                        {
                            tmpItem.StrFloor = "B4F";
                        }
                        tmpItem.StrPumpCount = g.Key.ToString();
                        tmpItem.StrPumpSum = g.ToList<int>().Count().ToString();
                        formItmes.Add(tmpItem);
                    }
                });
                //选择一个点，再生成提资表
                using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (AcadDatabase acadDb = AcadDatabase.Active())
                {
                    var point = Active.Editor.GetPoint("\n选择要插入的基点位置");
                    if (point.Status != PromptStatus.OK)
                    {
                        return;
                    }
                    //插入表头
                    Point3d position = point.Value;
                    acadDb.ModelSpace.ObjectId.InsertBlockReference("W-NOTE", WaterWellBlockNames.WaterWellTableHeader, position, new Scale3d(1, 1, 1), 0);
                    //插入表身
                    Vector3d vector = new Vector3d(0, -1, 0);
                    position += vector * 2000;
                    for (int i = 0; i < formItmes.Count; i++)
                    {
                        var item = formItmes[i];
                        var dic = new Dictionary<string, string>();
                        dic.Add("集水井编号", item.StrNumber);
                        dic.Add("楼层编号", item.StrFloor);
                        dic.Add("位置", "普通车库");
                        dic.Add("流量", "xx");
                        dic.Add("扬程", "xx");
                        dic.Add("电量", "xx");
                        dic.Add("井内水泵台数", item.StrPumpCount);
                        dic.Add("数量统计", item.StrPumpSum);
                        var bodyId = acadDb.ModelSpace.ObjectId.InsertBlockReference("W-NOTE", WaterWellBlockNames.WaterWellTableBody, position, new Scale3d(1, 1, 1), 0, dic);
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
