using Autodesk.AutoCAD.DatabaseServices;
using Catel.Messaging;
using System.Collections.Generic;

namespace TianHua.FanSelection.Messaging
{
    public class ThModelCopyMessageArgs : ThModelMessageArgs
    {
        public Dictionary<string, string> ModelSystemMapping { get; set; }
    }

    public class ThModelCopyMessage : MessageBase<ThModelCopyMessage, ThModelCopyMessageArgs>
    {
        public ThModelCopyMessage()
        {
        }

        public ThModelCopyMessage(ThModelCopyMessageArgs args)
            : base(args)
        {
        }
    }
}
