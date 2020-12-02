using Catel.Messaging;
using System.Collections.Generic;

namespace TianHua.FanSelection.Messaging
{
    public class ThModelUndoMessageArgs : ThModelMessageArgs
    {
        public List<string> UnappendedModels { get; set; }
        public List<string> ReappendedModels { get; set; }
    }

    public class ThModelUndoMessage : MessageBase<ThModelUndoMessage, ThModelUndoMessageArgs>
    {
        public ThModelUndoMessage()
        {
        }

        public ThModelUndoMessage(ThModelUndoMessageArgs args)
            : base(args)
        {
        }
    }
}
