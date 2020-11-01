using Catel.Messaging;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;

namespace TianHua.FanSelection.Messaging
{
    public class ThModelCopyMessageArgs : ThModelMessageArgs
    {
        public Dictionary<string, string> ModelMapping { get; set; }
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
