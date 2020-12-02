using Catel.Messaging;

namespace TianHua.FanSelection.Messaging
{
    public class ThModelSaveMessageArgs : ThModelMessageArgs
    {
        public string FileName { get; set; }
    }

    public class ThModelSaveMessage : MessageBase<ThModelSaveMessage, ThModelSaveMessageArgs>
    {
        public ThModelSaveMessage()
        {
        }

        public ThModelSaveMessage(ThModelSaveMessageArgs args)
            : base(args)
        {
        }
    }
}
