using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.ViewModel;
using System.Collections.Generic;
using ThControlLibraryWPF.CustomControl;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class uiBlockNameConfig : ThCustomWindow
    {
        private BlockConfigViewModel viewModel;
        public static uiBlockNameConfig staticUIBlockName =new uiBlockNameConfig();

        private uiBlockNameConfig()
        {
            InitializeComponent();
            viewModel = new BlockConfigViewModel();
            DataContext = viewModel;
            staticUIBlockName = this;
        }

        public Dictionary<string, List<string>> GetBlockNameList()
        {
            var dic = new Dictionary<string, List<string>>();
            foreach (var key in this.viewModel.BlockNameConfigList.Keys)
            {
                dic.Add(key, viewModel.BlockNameConfigList[key].First());
                dic[key].AddRange(viewModel.BlockNameConfigList[key].Last());
            }
            foreach(var key in this.viewModel.BlockNameList.Keys)
            {
                dic[key].AddRange(viewModel.BlockNameList[key].Select(e=>e.layerName).ToList());
            }
            return dic;
        }

        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            var oldViewModel = viewModel.SetViewModel?.Clone();
            System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
            var blockName = btn.Tag.ToString();
            viewModel.SetViewModel.BlockName = blockName;
            viewModel.SetViewModel.ConfigList = viewModel.BlockNameList[blockName];
            uiBlockNameConfigSet systemSet = new uiBlockNameConfigSet(viewModel.SetViewModel);
            systemSet.Owner = this;
            var ret = systemSet.ShowDialog();
            if (ret == false)//用户取消了操作
            {
                viewModel.SetViewModel = oldViewModel;
                viewModel.BlockNameList[blockName] = viewModel.SetViewModel.ConfigList;
                return;
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
                    if(!viewModel.BlockNameConfigList[block].Last().Contains(config.layerName))
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
            e.Cancel = true;
            Hide();
        }
    }
}
