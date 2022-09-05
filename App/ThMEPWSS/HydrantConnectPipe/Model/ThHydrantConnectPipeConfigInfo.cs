using ThMEPWSS.ViewModel;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThHydrantConnectPipeConfigInfo
    {
        public string strMapScale = "1:150";//出图比例
        public bool isSetupValve = true;//设置支管蝶阀
        public bool isMarkSpecif = true;//标注支管规格
        public bool isCoveredGraph = false;//覆盖已绘图元
        public OutputType isTchPipe = OutputType.CAD;//生成天正水管
    }
}
