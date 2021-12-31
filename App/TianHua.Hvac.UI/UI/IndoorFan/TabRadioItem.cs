using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace TianHua.Hvac.UI.UI.IndoorFan
{
    public class TabRadioItem : NotifyPropertyChangedBase
    {
        TabRadioButton dynamicTabRadio;
        public TabRadioItem(TabRadioButton tabRadioButton)
        {
            this.dynamicTabRadio = tabRadioButton;
        }
        public bool InEdit
        {
            get
            {
                if (this.CanEdit)
                    return dynamicTabRadio.InEdit;
                return false;
            }
            set
            {
                dynamicTabRadio.InEdit = value;
                this.RaisePropertyChanged();
            }
        }
        public string Id
        {
            get { return dynamicTabRadio.Id; }
        }
        public string Content
        {
            get { return dynamicTabRadio.Content; }
            set
            {
                dynamicTabRadio.Content = value;
                this.RaisePropertyChanged();
                InEdit = false;
            }
        }
        public string GroupName
        {
            get { return dynamicTabRadio.GroupName; }
            set
            {
                dynamicTabRadio.GroupName = value;
                this.RaisePropertyChanged();
            }
        }
        public object DynTag
        {
            get { return dynamicTabRadio.DynTag; }
            set
            {
                dynamicTabRadio.DynTag = value;
                this.RaisePropertyChanged();
            }
        }
        public bool CanEdit
        {
            get { return dynamicTabRadio.CanEdit; }
            set
            {
                dynamicTabRadio.CanEdit = value;
                this.RaisePropertyChanged();
            }
        }
        public bool CanDelete
        {
            get { return dynamicTabRadio.CanDelete; }
            set
            {
                dynamicTabRadio.CanDelete = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsAddBtn
        {
            get { return dynamicTabRadio.IsAddBtn; }
            set
            {
                dynamicTabRadio.IsAddBtn = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
