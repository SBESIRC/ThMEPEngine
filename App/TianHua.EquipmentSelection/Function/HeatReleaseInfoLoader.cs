using System.Collections.Generic;
using TianHua.Publics.BaseCode;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.Function
{
    public class HeatReleaseInfoLoader : ThModelLoader
    {
        public List<HeatReleaseInfo> HeatReleases { get; private set; }
        public HeatReleaseInfoLoader()
        {
            HeatReleases = new List<HeatReleaseInfo>();
        }

        public override void LoadFromFile(string path)
        {
            HeatReleases = FuncJson.Deserialize<List<HeatReleaseInfo>>(ReadTxt(path));
        }
    }
}
