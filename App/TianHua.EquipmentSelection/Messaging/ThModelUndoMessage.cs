using Catel.Messaging;
using System.Collections.Generic;

namespace TianHua.FanSelection.Messaging
{
    public class ThModelUndoMessageArgs : ThModelMessageArgs
    {
        public Dictionary<string, List<int>> UnappendedModels { get; set; }
        public Dictionary<string, List<int>> ReappendedModels { get; set; }
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
