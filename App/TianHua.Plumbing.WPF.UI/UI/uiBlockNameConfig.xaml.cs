using AcHelper;
using Autodesk.AutoCAD.PlottingServices;
using DotNetARX;
using ICSharpCode.SharpZipLib.Zip;
using Linq2Acad;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.BlockNameConfig;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class uiBlockNameConfig : ThCustomWindow
    {
        private BlockConfigViewModel viewModel => BlockConfigService.Instance;
        public static uiBlockNameConfig staticUIBlockName = new uiBlockNameConfig();
        private uiBlockNameConfig()
        {
            InitializeComponent();
            DataContext = viewModel;
            staticUIBlockName = this;
        }

        public Dictionary<string, List<string>> GetBlockNameList()
        {
            return BlockConfigService.GetBlockNameListDict();
        }

        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                SetFocusToDwgView();
                System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
                var blockName = btn.Tag.ToString();

                viewModel.SetViewModel.BlockName = blockName;
                viewModel.SetViewModel.ConfigList = viewModel.BlockNameList[blockName];

                var oldViewModel = viewModel.SetViewModel?.Clone();

                uiBlockNameConfigSet systemSet = new uiBlockNameConfigSet(viewModel.SetViewModel);
                systemSet.Owner = this;
                var ret = systemSet.ShowDialog();
                if (ret == false)//用户取消了操作
                {
                    viewModel.SetViewModel = oldViewModel;
                    viewModel.BlockNameList[blockName] = viewModel.SetViewModel.ConfigList;
                }
            }
        }

        /// <summary>
        /// 采用目标检测对整张图片进行识别
        /// </summary>
        private async void Cloud_Configuration2(object sender, RoutedEventArgs e)
        {
            var cad2Pic = new Cad2Pic();
            var picInfo = new PicInfo();
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            var picName = cad2Pic.THDETECT(picInfo);
            stopwatch.Stop();
            Active.Editor.WriteLine(PlotFactory.ProcessPlotState.ToString());

            string[] strs = new string[1] { picName };
            while (true)
            {
                if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
                {
                    break;
                }
                Thread.Sleep(1000);
            }
            Active.Editor.WriteLine(stopwatch.Elapsed.Minutes + "分" + stopwatch.Elapsed.Seconds + "秒");

            await Program.Run(strs);

            var json2Cad = new Json2Cad();
            json2Cad.DrawRect(picInfo);
        }

        /// <summary>
        /// 采用目标识别，对每个块进行打分
        /// </summary>
        private async void Cloud_Configuration(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
                var zipFile = Block2Pic.GenerateBlockPic(out Dictionary<string, List<double>> blockSizeDic);
                stopwatch.Stop();
                Active.Editor.WriteMessage("生成图片用时: " + stopwatch.Elapsed.Seconds + " s\n");
                stopwatch.Reset();
                stopwatch.Start();
                await Program.Run(new string[1] { zipFile });
                stopwatch.Stop();
                Active.Editor.WriteMessage("分类用时: " + stopwatch.Elapsed.Seconds + " s\n");
                stopwatch.Reset();
                stopwatch.Start();
                UpdateBlockList(zipFile, blockSizeDic);
                stopwatch.Stop();
                Active.Editor.WriteMessage("写入用时: " + stopwatch.Elapsed.Seconds + " s\n");
            }
            catch
            {
            }
        }

        public void UpdateBlockList(string zipFile,Dictionary<string, List<double>> blockSizeDic)
        {
            //var dict = new Dictionary<int, string>() { 
            //    { 3, "洗脸盆" }, { 4, "洗涤槽" }, { 5, "拖把池" }, 
            //    { 6, "洗衣机" }, { 8, "淋浴房" }, { 9, "转角淋浴房" }, 
            //    { 10, "浴缸" }, { 11, "喷头" }, { 0, "坐便器" }, { 1, "小便器" }, 
            //    { 2, "蹲便器" }, { 7, "地漏" } };
            foreach(var key in viewModel.BlockNameList.Keys)
            {
                viewModel.BlockNameList[key].Clear();
            }

            var dict = new Dictionary<int, string>() {
                { 0, "坐便器" }, { 1, "小便器" }, { 2, "蹲便器" },
                { 3, "单盆洗手台" }, { 4, "厨房洗涤盆" }, { 5, "拖把池" },
                { 6, "洗衣机" }, { 7, "地漏"   }, { 8, "浴缸" }, 
                { 9, "喷头" }, { 10, "其他" }};

            var lines = File.ReadAllLines(zipFile + ".csv").Where(x => !string.IsNullOrWhiteSpace(x));
            foreach (var line in lines)
            {
                if (line == "error")
                    break;
                var arr = line.Split(',');
                if (!int.TryParse(arr[1], out var typeId)) continue;
                if (!dict.ContainsKey(typeId)) continue;
                
                var blkName = arr[0].Replace(".jpg", "");
                if (blkName.Contains("Kitchen-4"))
                    ;
                if (typeId == 7)
                {
                    if (blockSizeDic.ContainsKey(blkName))
                    {
                        if (blockSizeDic[blkName].First() >= 200 || blockSizeDic[blkName].First() >= 200)
                        {
                            continue;
                        }
                    }
                }
                if (viewModel.BlockNameList.ContainsKey(dict[typeId]))
                {
                    viewModel.BlockNameList[dict[typeId]].Add(new BlockNameConfigViewModel(blkName));
                }
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "jason files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;
                    var fileStream = openFileDialog.OpenFile();
                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        fileContent = reader.ReadToEnd();
                    }
                }
            }
            var configList = fileContent.FromJson<Dictionary<string, List<List<string>>>>();
            if (!(configList is null))
            {
                viewModel.BlockNameConfigList = configList;
            }

            foreach (var key in viewModel.BlockNameList.Keys)
            {
                viewModel.BlockNameList[key].Clear();
            }
            foreach (string key in viewModel.BlockNameConfigList.Keys)
            {
                var names = viewModel.BlockNameConfigList[key].Last();
                foreach (string name in names)
                {
                    viewModel.BlockNameList[key].Add(new BlockNameConfigViewModel(name));
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            foreach (string block in viewModel.BlockNameList.Keys)
            {
                foreach (BlockNameConfigViewModel config in viewModel.BlockNameList[block])
                {
                    if (!viewModel.BlockNameConfigList[block].Last().Contains(config.layerName))
                    {
                        viewModel.BlockNameConfigList[block].Last().Add(config.layerName);
                    }
                }
            }
            var blockDic = viewModel.BlockNameConfigList;
            string json = blockDic.ToJson();
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "jason files (*.json)|*.json|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog1.FileName, json);
            }
        }

        private void ThCustomWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                viewModel.ClearTransientGraphics();
                SetFocusToDwgView();
            }
            e.Cancel = true;
            Hide();
        }

        private void show__Click(object sender, RoutedEventArgs e)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                viewModel.ClearTransientGraphics();
                System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
                var blockName = btn.Tag.ToString();
                viewModel.Show(blockName, acadDatabase.Database);
                viewModel.AddToTransient(blockName);
                SetFocusToDwgView();
            }
        }
        private void SetFocusToDwgView()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
        #if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
        #else
            Active.Document.Window.Focus();
        #endif
        }

        private void btnSet_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                viewModel.ClearTransientGraphics();
                SetFocusToDwgView();
            }
        }
    }
}
