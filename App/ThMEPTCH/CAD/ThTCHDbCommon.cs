using System.Collections.Generic;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;

namespace ThMEPTCH.CAD
{
    public static class ThTCHDbCommon
    {
        public static readonly Dictionary<string, DoorTypeOperationEnum> DoorTypeOperationMapping =
            new Dictionary<string, DoorTypeOperationEnum>()
            {
                { "$DorLib2D$00000114", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000228", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000116", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000231", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000001", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000002", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000003", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000004", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000009", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000010", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000011", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000012", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000021", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000222", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000223", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000224", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000225", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000226", DoorTypeOperationEnum.SWING },
                { "$DorLib2D$00000127", DoorTypeOperationEnum.SLIDING },
                { "$DorLib2D$00000128", DoorTypeOperationEnum.SLIDING },
                { "$DorLib2D$00000129", DoorTypeOperationEnum.SLIDING },
                { "$DorLib2D$00000130", DoorTypeOperationEnum.SLIDING },
                { "$DorLib2D$00000131", DoorTypeOperationEnum.SLIDING },
                { "$DorLib2D$00000132", DoorTypeOperationEnum.SLIDING },
                { "$DorLib2D$00000134", DoorTypeOperationEnum.SLIDING },
                { "$DorLib2D$00000135", DoorTypeOperationEnum.SLIDING },
                { "$DorLib2D$00000138", DoorTypeOperationEnum.SLIDING },
            };

        public static readonly Dictionary<string, WindowTypeEnum> WindowTypeMapping =
            new Dictionary<string, WindowTypeEnum>()
            {
                { "$WINLIB2D$00000001", WindowTypeEnum.Window },
                { "$WINLIB2D$00000039", WindowTypeEnum.Shutter },
                { "$WINLIB2D$00000004", WindowTypeEnum.Eccentric },
            };
    }
}
