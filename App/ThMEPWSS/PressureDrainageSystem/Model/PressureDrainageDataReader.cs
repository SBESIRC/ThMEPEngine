using System.Collections.Generic;
using System.Windows;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.PressureDrainageSystem.Model;
using ThMEPWSS.PressureDrainageSystem.Service;
namespace ThMEPWSS.PressureDrainage.Model
{
    public class PressureDrainageDataReader
    {
        /// <summary>
        /// 从viewmodel中获取图形数据，将数据按楼层整合到数据结构modeldata中
        /// </summary>
        /// <param name="viewmodel"></param>
        /// <returns></returns>
        public PressureDrainageModelData GetPressureDrainageModelData(PressureDrainageSystemDiagramVieModel viewmodel)
        {
            if (viewmodel.FloorAreaList == null || viewmodel.FloorNumList == null || viewmodel.SelectedArea == null || viewmodel.UndpdsFloorListDatas == null)
            {
                MessageBox.Show("未读取到有效楼层数据");
                return null;
            }
            var modeldatas = new PressureDrainageModelData();
            modeldatas.FloorListDatas = viewmodel.UndpdsFloorListDatas;
            modeldatas.FloorLineSpace = viewmodel.UndpdsFloorLineSpace;
            modeldatas.FloorAreaList = viewmodel.FloorAreaList;
            modeldatas.FloorNumList = viewmodel.FloorNumList;
            var pressDrainageDCollSvs = new PressureDrainageDataCollectionService(viewmodel);
            pressDrainageDCollSvs.InitData();
            pressDrainageDCollSvs.CollectData();
            modeldatas.FloorDict = new Dictionary<string, PressureDrainageSystemDiagramStorey>();
            for (int i = 0; i < modeldatas.FloorListDatas.Count; i++)
            {
                if (modeldatas.FloorDict.ContainsKey(modeldatas.FloorListDatas[i]))
                {
                    MessageBox.Show("楼层框定表中出现重复的编号，请修改后再试。");
                    return null;
                }
                PressureDrainageSystemDiagramStorey floorData = new PressureDrainageSystemDiagramStorey();
                modeldatas.FloorDict.Add(modeldatas.FloorListDatas[i], floorData);
            }
            var dataClassifyByStoriesSvs = new PressureDrainageDataClassifyByStoriesService();
            dataClassifyByStoriesSvs.CollectDataService = pressDrainageDCollSvs;
            dataClassifyByStoriesSvs.Viewmodel = viewmodel;
            dataClassifyByStoriesSvs.Modeldatas = modeldatas;
            dataClassifyByStoriesSvs.ClassifyDataByStories();
            return modeldatas;
        }
    }
}