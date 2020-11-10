using System.Collections.Generic;
using TianHua.Publics.BaseCode;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.Function
{
    public class ExhaustModelLoader : ThModelLoader
    {
        public List<ExhaustSpaceInfo> Spaces { get; private set; }

        public ExhaustModelLoader()
        {
            Spaces = new List<ExhaustSpaceInfo>();
        }

        public override void LoadFromFile(string path)
        {
            Spaces = FuncJson.Deserialize<List<ExhaustSpaceInfo>>(ReadTxt(path));
        }
    }
}