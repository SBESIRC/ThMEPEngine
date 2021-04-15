using Catel.Messaging;

namespace TianHua.FanSelection.Messaging
{
    public class ThModelBeginSaveMessageArgs : ThModelMessageArgs
    {
        public string FileName { get; set; }
    }


    public class ThModelBeginSaveMessage : MessageBase<ThModelBeginSaveMessage, ThModelBeginSaveMessageArgs>
    {
        public ThModelBeginSaveMessage()
        {
        }

        public ThModelBeginSaveMessage(ThModelBeginSaveMessageArgs args)
            : base(args)
        {
        }
    }
}
