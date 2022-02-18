using System;
using System.Windows.Controls;

namespace TianHua.Electrical.PDS.UI.Models
{
    class UTableItem
    {
        public string ItemUid { get; }
        public string Title { get; }
        public UserControl ShowUserControl { get;}
        public UTableItem(string title, UserControl userControl) 
        {
            this.Title = title;
            this.ItemUid = Guid.NewGuid().ToString();
            this.ShowUserControl = userControl;
        }
    }
}
