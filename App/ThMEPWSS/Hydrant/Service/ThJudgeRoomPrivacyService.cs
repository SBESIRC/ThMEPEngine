using System.Linq;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPWSS.Hydrant.Service
{
    public class ThJudgeRoomPrivacyService:IRoomPrivacy
    {
        private List<string> PrivateRoomNames { get; set; }
        private List<string> PublicRoomNames { get; set; }

        public ThJudgeRoomPrivacyService()
        {
            PublicRoomNames = new List<string>();
            PrivateRoomNames = new List<string> { "商铺", "餐饮", "厨房", "观众厅", "主力店" }; //持续丰富
        }
        public Privacy Judge(ThIfcRoom room)
        {
            var tags = new List<string>();
            tags.Add(room.Name);
            tags.AddRange(room.Tags);
            foreach (var tag in tags)
            {
                if (PrivateRoomNames.Where(o => tag.Contains(o)).Any())
                {
                    return Privacy.Private;
                }
            }
            return Privacy.Public;
        }
    }
}
