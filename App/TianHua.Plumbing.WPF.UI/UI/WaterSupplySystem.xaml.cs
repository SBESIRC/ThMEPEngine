using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using GeometryExtensions;
using System.Windows;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.WaterSupplyPipeSystem.Command;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class WaterSupplySystem : ThCustomWindow
    {
        static WaterSupplyVM viewModel;


        public WaterSupplySystem()
        {
            InitializeComponent();
            //给水系统图相关
            if (null == viewModel)
                viewModel = new WaterSupplyVM();
            this.DataContext = viewModel;
        }


        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            var oldViewModel = viewModel.SetViewModel?.Clone(viewModel.MaxFloor);
            WaterSupplySystemSet systemSet = new WaterSupplySystemSet(viewModel.SetViewModel,viewModel.MaxFloor);
            systemSet.Owner = this;
            var ret = systemSet.ShowDialog();
            if (ret == false)
            {
                //用户取消了操作
                viewModel.SetViewModel = oldViewModel;
                return;
            }
        }


        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var blockConfig = uiBlockNameConfig.staticUIBlockName.GetBlockNameList();
                using (var cmd = new ThWaterSuplySystemDiagramCmd(viewModel, blockConfig))
                {
                    var insertOpt = new PromptPointOptions("\n指定图纸的插入点");
                    var optRes = Active.Editor.GetPoint(insertOpt);
                    if (optRes.Status == PromptStatus.OK)
                    {
                        viewModel.InsertPt = optRes.Value.TransformBy(Active.Editor.UCS2WCS());
                        cmd.Execute();
                    }
                }
            }
            catch{}
        }


        private void btnSelectFloor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                viewModel.CreateFloorFraming();
            }
            catch{}
        }


        private void btnReadStoreys_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                viewModel.InitListDatas();
            }
            catch{}
        }


        private void ThCustomWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var cachedArea = CadCache.TryGetRange();
            if (cachedArea != null)
            {
                viewModel.InitListDatasByArea(cachedArea, false);
            }
        }
    }
}
