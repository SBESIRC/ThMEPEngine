using Catel.Messaging;
using System.Collections.Generic;

namespace TianHua.FanSelection.Messaging
{
    public class ThModelDeleteMessageArgs : ThModelMessageArgs
    {
        public Dictionary<string, bool> Models { get; set; }
    }

    public class ThModelDeleteMessage : MessageBase<ThModelDeleteMessage, ThModelDeleteMessageArgs>
    {
        public ThModelDeleteMessage()
        {
        }

        public ThModelDeleteMessage(ThModelDeleteMessageArgs args)
            : base(args)
        {
        }
    }
}
