using Catel.Messaging;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;

namespace TianHua.FanSelection.Messaging
{
    public class ThModelSyncMessageArgs : ThModelMessageArgs
    {
        public List<string> Models { get; set; }
    }

    public class ThModelSyncMessage : MessageBase<ThModelSyncMessage, ThModelSyncMessageArgs>
    {
        public ThModelSyncMessage()
        {
        }

        public ThModelSyncMessage(ThModelSyncMessageArgs args)
            : base(args)
        {
        }
    }
}
