using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.UI.Models
{
    internal class CurrentChangedMessage : ValueChangedMessage<object>
    {
        public CurrentChangedMessage(object value) : base(value)
        {
        }
    }
}
