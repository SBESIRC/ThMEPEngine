using System.Collections.Generic;

namespace ThMEPWSS.Pipe.Service
{
    public class ThCleanToolsManager
    {
        private Dictionary<string, List<string>> BlockConfig { set; get; }
        private Dictionary<string, int> BlockIndexDic { set; get; }

        public ThCleanToolsManager()
        {
            BlockConfig = new Dictionary<string, List<string>>();
            //BlockConfig.Add("拖把池", new List<string>() { "A-Kitchen-9" });
            //BlockConfig.Add("洗衣机", new List<string>() { "A-Toilet-9" });
            //BlockConfig.Add("厨房洗涤盆", new List<string>() { "A-Kitchen-4" });
            //BlockConfig.Add("坐便器", new List<string>() { "A-Toilet-5" });
            //BlockConfig.Add("单盆洗手台", new List<string>() { "A-Toilet-1", "A-Toilet-3", "A-Toilet-4" });
            //BlockConfig.Add("厨房洗涤盆", new List<string>() { "A-Kitchen-4" });
            //BlockConfig.Add("双盆洗手台", new List<string>() { "A-Toilet-2" });
            //BlockConfig.Add("淋浴器", new List<string>() { "A-Toilet-6", "A-Toilet-7" });
            //BlockConfig.Add("浴缸", new List<string>() { "A-Toilet-8" });
        }
        public ThCleanToolsManager(Dictionary<string, List<string>> blockConfig)
        {
            BlockConfig = blockConfig;
            BlockIndexDic = new Dictionary<string, int>();
            BlockIndexDic.Add("坐便器", 0);
            BlockIndexDic.Add("单盆洗手台", 1);
            BlockIndexDic.Add("双盆洗手台", 2);
            BlockIndexDic.Add("厨房洗涤盆", 3);
            BlockIndexDic.Add("淋浴器", 4);
            BlockIndexDic.Add("洗衣机", 5);
            BlockIndexDic.Add("阳台洗手盆", 6);
            BlockIndexDic.Add("浴缸", 7);
        }
        public bool IsCleanToolBlockName(string name)
        {
            if(BlockConfig.Count == 0)
            {
                return true;
            }
            foreach(var key in BlockConfig.Keys)
            {
                foreach(var block in BlockConfig[key])
                {
                    if(name.ToLower().Contains(block.ToLower()))
                    {
                        return true;
                    }
                }
            }
            return false; 
        }

        public int CleanToolIndex(string name)
        {
            foreach (var key in BlockConfig.Keys)
            {
                foreach (var block in BlockConfig[key])
                {
                    if (name.ToLower().Contains(block.ToLower()))
                    {
                        if(BlockIndexDic.ContainsKey(key))
                        {
                            return BlockIndexDic[key];
                        }
                    }
                }
            }
            return -1;
        }
    }
}
