using System.Collections.Generic;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Engine
{
    class ThRoomBuilderEngineWSS: ThRoomBuilderEngine
    {
        public ThRoomBuilderEngineWSS() 
            :base()
        { }
        public override void Build(List<ThIfcRoom> rooms, List<ThIfcTextNote> marks, bool isWithHole = true)
        {
            base.SpaceMatchText(BuildTextContainers(marks, rooms));
        }
    }
}
