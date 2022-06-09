using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStallProgramDisplay.ViewModel
{
    public class ParkingStallDisplayViewModel
    {
        private string _BasementNums = "1";//地库总数量
        public string BasementNums
        {
            get { return _BasementNums; }
            set { _BasementNums = value; }
        }

        private string _Process = "32";//进程数
        public string Process
        {
            get { return _Process; }
            set { _Process = value; }
        }

        private string _ProgressRate = "1/1";//计算进度
        public string ProgressRate
        {
            get { return _ProgressRate; }
            set { _ProgressRate = value; }
        }

        private string _TimeCost = "0";//用时
        public string TimeCost
        {
            get { return _TimeCost; }
            set { _TimeCost = value; }
        }

        private string _BlockName = "unknown";//块名
        public string BlockName
        {
            get { return _BlockName; }
            set { _BlockName = value; }
        }

        private string _Iterations;//预计代数
        public string Iterations
        {
            get { return _Iterations; }
            set { _Iterations = value; }
        }

        private string _Populations;//种群数量
        public string Populations
        {
            get { return _Populations; }
            set { _Populations = value; }
        }

        private string _CurIterations;//当前代数
        public string CurIterations
        {
            get { return _CurIterations; }
            set { _CurIterations = value; }
        }

        private string _CurCars;//当前车位数
        public string CurCars
        {
            get { return _CurCars; }
            set { _CurCars = value; }
        }

        private string _AveArea;//车均面积
        public string AveArea
        {
            get { return _AveArea; }
            set { _AveArea = value; }
        }

        private List<string> _Results;//运行结束输出
        public List<string> Results
        {
            get { return _Results; }
            set { _Results = value; }
        }
    }
}
