using System.Collections.Generic;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;

namespace ThMEPTCH.CAD
{
    public static class ThTCHDbCommon
    {
        public static readonly Dictionary<string, DoorTypeOperationEnum> DoorTypeOperationMapping =
            new Dictionary<string, DoorTypeOperationEnum>()
            {
                { "$DorLib2D$00000001", DoorTypeOperationEnum.SWING0001 },
                { "$DorLib2D$00000002", DoorTypeOperationEnum.SWING0002 },
                { "$DorLib2D$00000003", DoorTypeOperationEnum.SWING0003 },
                { "$DorLib2D$00000004", DoorTypeOperationEnum.SWING0004 },
                { "$DorLib2D$00000009", DoorTypeOperationEnum.SWING0009 },
                { "$DorLib2D$00000010", DoorTypeOperationEnum.SWING0010 },
                { "$DorLib2D$00000011", DoorTypeOperationEnum.SWING0011 },
                { "$DorLib2D$00000012", DoorTypeOperationEnum.SWING0012 },
                { "$DorLib2D$00000021", DoorTypeOperationEnum.SWING0021 },
                { "$DorLib2D$00000114", DoorTypeOperationEnum.SWING0114 },
                { "$DorLib2D$00000116", DoorTypeOperationEnum.SWING0116 },
                { "$DorLib2D$00000222", DoorTypeOperationEnum.SWING0222 },
                { "$DorLib2D$00000223", DoorTypeOperationEnum.SWING0223 },
                { "$DorLib2D$00000224", DoorTypeOperationEnum.SWING0224 },
                { "$DorLib2D$00000225", DoorTypeOperationEnum.SWING0225 },
                { "$DorLib2D$00000226", DoorTypeOperationEnum.SWING0226 },
                { "$DorLib2D$00000228", DoorTypeOperationEnum.SWING0228 },
                { "$DorLib2D$00000231", DoorTypeOperationEnum.SWING0231 },
                { "$DorLib2D$00000127", DoorTypeOperationEnum.SLIDING0127 },
                { "$DorLib2D$00000128", DoorTypeOperationEnum.SLIDING0128 },
                { "$DorLib2D$00000129", DoorTypeOperationEnum.SLIDING0129 },
                { "$DorLib2D$00000130", DoorTypeOperationEnum.SLIDING0130 },
                { "$DorLib2D$00000131", DoorTypeOperationEnum.SLIDING0131 },
                { "$DorLib2D$00000132", DoorTypeOperationEnum.SLIDING0132 },
                { "$DorLib2D$00000134", DoorTypeOperationEnum.SLIDING0134 },
                { "$DorLib2D$00000135", DoorTypeOperationEnum.SLIDING0135 },
                { "$DorLib2D$00000138", DoorTypeOperationEnum.SLIDING0138 },
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
