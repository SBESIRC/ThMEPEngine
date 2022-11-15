using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ThMEPWSS.PumpSectionalView.Model;
using ThMEPWSS.PumpSectionalView;
using ThControlLibraryWPF.CustomControl;
using System.Text.RegularExpressions;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.WaterWellPumpLayout.Command;
using Linq2Acad;
using NetTopologySuite.Algorithm;
using System.ComponentModel;
//using System.Web.UI.WebControls;
using static DotNetARX.Preferences;
using System.Windows.Media;
using NetTopologySuite.Noding;
using NPOI.SS.Formula.Functions;
using System.IO;
using System.Windows.Controls.Primitives;
using ThMEPEngineCore.IO.ExcelService;
using ThCADExtension;
using System.Globalization;
using Match = System.Text.RegularExpressions.Match;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiPumpSectionalView.xaml 的交互逻辑
    /// </summary>
    public partial class uiPumpSectionalView : ThCustomWindow
    {

        PumpSectionalViewModel pumpSectionalViewModel;

        public uiPumpSectionalView()
        {
            InitializeComponent();
            pumpSectionalViewModel = new PumpSectionalViewModel();
            DataContext = pumpSectionalViewModel;
        }

        //数据验证-输入前
        private void NotNegativeDecimals_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex re = new Regex("[^0-9.]+");//只能输入非负小数，小数点
            e.Handled = re.IsMatch(e.Text);
        }
        private void AllowNegativeDecimals_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //Regex re = new Regex("[^0-9.]+");//只能输入小数，小数点
            //Regex re = new Regex("[^0-9.]-");//只能输入非负小数，小数点
            //e.Handled = re.IsMatch(e.Text);
            double a;
            bool b= double.TryParse(e.Text,out a);
            if (b||e.Text=="."||e.Text=="-")
            {
                e.Handled = false;//正确输入，不阻止
            }
            else
                e.Handled = true;
        }

        private void NotNegativeInteger_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex re = new Regex("[^0-9]+");//只能输入非负整数，小数点
            e.Handled = re.IsMatch(e.Text);
        }

        private void NotNullString_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            bool flag = e.Text == null || string.IsNullOrEmpty(e.Text.Trim());
            e.Handled = flag ;
        }

        //数据验证-失去焦点
        private void Length_LostFocus(object sender, RoutedEventArgs e)
        {
            Regex re = new Regex("^[1-9]\\d*\\.[5]$|0\\.[5]$|^[1-9]\\d*$");//判断是否是0.5倍数
            if (!re.IsMatch(Length_TextBox.Text)&& Length_TextBox.Text!="0")
            {
                MessageBox.Show("长输入必须为0.5的倍数");
                
            }
        }
        
        private void Width_LostFocus(object sender, RoutedEventArgs e)
        {
            Regex re = new Regex("^[1-9]\\d*\\.[5]$|0\\.[5]$|^[1-9]\\d*$");//判断是否是0.5倍数
            if (!re.IsMatch(Width_TextBox.Text) && Width_TextBox.Text != "0")
            {
                MessageBox.Show("宽输入必须为0.5的倍数");
               
            }
        }
     
        private void High_LostFocus(object sender, RoutedEventArgs e)
        {
            Regex re = new Regex("^[1-9]\\d*\\.[5]$|0\\.[5]$|^[1-9]\\d*$");//判断是否是0.5倍数
            if (!re.IsMatch(High_TextBox.Text) && High_TextBox.Text != "0")
            {
                MessageBox.Show("高输入必须为0.5的倍数");
              
            }
        }
 

        private void TextBaseHigh_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }
        

        private void ThCustomWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        { 
            e.Cancel = true;
            Hide();
        }
       
        //窗体加载事件
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           
        }

       

        //数字转换为中文
        private string getCountRefundInfoInChanese(string inputNum)
        {
            string[] intArr = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", };
            string[] strArr = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", };
            string[] Chinese = { "", "十", "百", "千", "万", "十", "百", "千", "亿" };
            //金额
            //string[] Chinese = { "元", "十", "百", "千", "万", "十", "百", "千", "亿" };
            char[] tmpArr = inputNum.ToString().ToArray();
            string tmpVal = "";
            for (int i = 0; i < tmpArr.Length; i++)
            {
                tmpVal += strArr[tmpArr[i] - 48];//ASCII编码 0为48
                tmpVal += Chinese[tmpArr.Length - 1 - i];//根据对应的位数插入对应的单位
            }

            return tmpVal;
        }

        private void dataGrid_Pump_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while ((dep != null) && !(dep is DataGridCell) && !(dep is DataGridColumnHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            if (dep == null|| dep is DataGridColumnHeader)
                return;
            if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;
                //获取列索引
                int ColumnIndex = cell.Column.DisplayIndex;

                if (ColumnIndex != 5)//没有点到下拉框
                {
                    return;
                }
                while ((dep != null) && !(dep is DataGridRow))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }
                DataGridRow row = dep as DataGridRow;
                //  Console.WriteLine("cell: ({0},{1})", , ColumnIndex.ToString());
                //string RowStr = FindRowIndex(row).ToString();
                //string ColumnStr = ColumnIndex.ToString();
                //MessageBox.Show("Row:Column is  " + RowStr + ":" + ColumnStr);
                int RowIndex = FindRowIndex(row);
                var i = pumpSectionalViewModel.LifePumpInfoList[RowIndex];
                if (i.Num > 1)
                {
                    string s1 = getCountRefundInfoInChanese((i.Num - 1).ToString());
                    string s2 = getCountRefundInfoInChanese(i.Num.ToString());
                    if(i.NoteList.Count!=0)
                        i.NoteList.Clear();
                    i.NoteList.Add(s2 + "用");
                    i.NoteList.Add(s1 + "用一备");
                }
                else if (i.Num == 1)
                {
                    if (i.NoteList.Count != 0)
                        i.NoteList.Clear();
                    i.NoteList.Add( "一用");
                }

            }
        }

        //获取行索引
        private int FindRowIndex(DataGridRow row)
        {
            DataGrid dataGrid = ItemsControl.ItemsControlFromItemContainer(row) as DataGrid;

            int index = dataGrid.ItemContainerGenerator.IndexFromContainer(row);

            return index;
        }


        private void dataGrid_Fire_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while ((dep != null) && !(dep is DataGridCell) && !(dep is DataGridColumnHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            if (dep == null || dep is DataGridColumnHeader)
                return;
            if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;
                //获取列索引
                int ColumnIndex = cell.Column.DisplayIndex;

                if (ColumnIndex != 6)//没有点到下拉框
                {
                    return;
                }
                while ((dep != null) && !(dep is DataGridRow))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }
                DataGridRow row = dep as DataGridRow;
               
                int RowIndex = FindRowIndex(row);
                var i = pumpSectionalViewModel.FirePumpInfoList[RowIndex];
                if (i.Num > 1)
                {
                    string s1 = getCountRefundInfoInChanese((i.Num - 1).ToString());
                    string s2 = getCountRefundInfoInChanese(i.Num.ToString());
                    if (i.NumList.Count != 0)
                        i.NumList.Clear();

                    i.NumList.Add(s2 + "用");
                    i.NumList.Add(s1 + "用一备");
                }
                else if (i.Num == 1)
                {
                    if (i.NumList.Count != 0)
                        i.NumList.Clear();

                    i.NumList.Add("一用");
                }

            }
        }

 
    }


    /// <summary>
    /// 允许textbox输入null
    /// </summary>
    public class CountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(value.ToString()))
            {
                return null;
            }
            return value;
        }
    }

}
