using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace TianHua.Electrical.PDS.UI.Models
{
    internal class GraphNodeAddMessage : ValueChangedMessage<object>
    {
        public GraphNodeAddMessage(object value) : base(value)
        {
        }
    }
}
