using ThMEPWSS.Hydrant.Model;
using System.Collections.Generic;
using System.Linq;

namespace ThMEPWSS.Hydrant.Service
{
    public class ThFireTypeDataManager
    {
        public List<string> DangerLevels { get; set; }
        public List<string> FireTypes { get; set; }
        public List<ThFireExtinguisherMaxProtectDis> Datas { get; set; }
        public ThFireTypeDataManager()
        {
            DangerLevels = new List<string> { "严重危险级", "中危险级", "轻危险级" };
            FireTypes = new List<string> { "A类火灾", "B类火灾", "C类火灾"};
            Build();
        }

        private void Build()
        {
            Datas = new List<ThFireExtinguisherMaxProtectDis>();
            Datas.Add(new ThFireExtinguisherMaxProtectDis {FireType = "A类火灾", DangerLevel = "严重危险级",Name="手提式灭火器",Distance =15 });
            Datas.Add(new ThFireExtinguisherMaxProtectDis { FireType = "A类火灾", DangerLevel = "严重危险级", Name = "推车式灭火器", Distance = 30 });

            Datas.Add(new ThFireExtinguisherMaxProtectDis { FireType = "A类火灾", DangerLevel = "中危险级", Name = "手提式灭火器", Distance = 20 });
            Datas.Add(new ThFireExtinguisherMaxProtectDis { FireType = "A类火灾", DangerLevel = "中危险级", Name = "推车式灭火器", Distance = 40 });

            Datas.Add(new ThFireExtinguisherMaxProtectDis { FireType = "A类火灾", DangerLevel = "轻危险级", Name = "手提式灭火器", Distance = 25 });
            Datas.Add(new ThFireExtinguisherMaxProtectDis { FireType = "A类火灾", DangerLevel = "轻危险级", Name = "推车式灭火器", Distance = 50 });


            Datas.Add(new ThFireExtinguisherMaxProtectDis { FireType = "B类火灾", DangerLevel = "严重危险级", Name = "手提式灭火器", Distance = 9 });
            Datas.Add(new ThFireExtinguisherMaxProtectDis { FireType = "B类火灾", DangerLevel = "严重危险级", Name = "推车式灭火器", Distance = 18 });

            Datas.Add(new ThFireExtinguisherMaxProtectDis { FireType = "B类火灾", DangerLevel = "中危险级", Name = "手提式灭火器", Distance = 12 });
            Datas.Add(new ThFireExtinguisherMaxProtectDis { FireType = "B类火灾", DangerLevel = "中危险级", Name = "推车式灭火器", Distance = 24 });

            Datas.Add(new ThFireExtinguisherMaxProtectDis { FireType = "B类火灾", DangerLevel = "轻危险级", Name = "手提式灭火器", Distance = 15 });
            Datas.Add(new ThFireExtinguisherMaxProtectDis { FireType = "B类火灾", DangerLevel = "轻危险级", Name = "推车式灭火器", Distance = 30 });

            Datas.Add(new ThFireExtinguisherMaxProtectDis { FireType = "C类火灾", DangerLevel = "严重危险级", Name = "手提式灭火器", Distance = 9 });
            Datas.Add(new ThFireExtinguisherMaxProtectDis { FireType = "C类火灾", DangerLevel = "严重危险级", Name = "推车式灭火器", Distance = 18 });

            Datas.Add(new ThFireExtinguisherMaxProtectDis { FireType = "C类火灾", DangerLevel = "中危险级", Name = "手提式灭火器", Distance = 12 });
            Datas.Add(new ThFireExtinguisherMaxProtectDis { FireType = "C类火灾", DangerLevel = "中危险级", Name = "推车式灭火器", Distance = 24 });

            Datas.Add(new ThFireExtinguisherMaxProtectDis { FireType = "C类火灾", DangerLevel = "轻危险级", Name = "手提式灭火器", Distance = 15 });
            Datas.Add(new ThFireExtinguisherMaxProtectDis { FireType = "C类火灾", DangerLevel = "轻危险级", Name = "推车式灭火器", Distance = 30 });
        }
        /// <summary>
        /// 查询最大保护距离
        /// </summary>
        /// <param name="dangerLevel">危险等级</param>
        /// <param name="fireType">火灾种类</param>
        /// <param name="name">灭火器名称</param>
        /// <returns></returns>
        public double Query(string fireType, string dangerLevel,string name)
        {
            var results = Datas.Where(o => o.FireType == fireType && o.DangerLevel == dangerLevel && o.Name == name);
            if(results.Count() >0 )
            {
                return results.First().Distance;
            }
            return 0.0;
        }
    }
}
