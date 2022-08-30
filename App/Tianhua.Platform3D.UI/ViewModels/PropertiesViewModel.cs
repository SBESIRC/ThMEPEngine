using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ThControlLibraryWPF.ControlUtils;
using Tianhua.Platform3D.UI.PropertyServices;

namespace Tianhua.Platform3D.UI.ViewModels
{
    class PropertiesViewModel : NotifyPropertyChangedBase
    {
        private PropertyService propertyService;
        public static readonly PropertiesViewModel Instacne = new PropertiesViewModel();
        private List<EntityProperties> EntityProperties;
        PropertiesViewModel() 
        {
            selectIds = new List<ObjectId>();
            propertyService = new PropertyService();
            Properties = new ObservableCollection<THProperties>();
            EntityProperties = new List<EntityProperties>();
            ClearHisData();
        }
        public void SelectIds(List<ObjectId> selectIds) 
        {
            SelectChanged(selectIds);
        }
        private List<ObjectId> selectIds { get; }
        private bool isMultipleType { get; set; }
        public bool IsMultipleType 
        {
            get { return isMultipleType; }
            set 
            {
                isMultipleType = value;
                this.RaisePropertyChanged();
            }
        }
        public string ShowTypeName
        {
            get { return string.Format("{0}({1})",TypeName,Count); }
            set 
            {
                this.RaisePropertyChanged();
            }
        }

        private string typeName { get; set; }
        public string TypeName
        {
            get { return typeName; }
            set
            {
                typeName = value;
                this.RaisePropertyChanged();
                RaisePropertyChanged("ShowTypeName");
            }
        }
        private int count { get; set; }
        public int Count 
        {
            get { return count; }
            set 
            {
                count = value;
                this.RaisePropertyChanged();
                RaisePropertyChanged("ShowTypeName");
            }
        }

        private ObservableCollection<THProperties> properties { get; set; }
        public ObservableCollection<THProperties> Properties 
        {
            get { return properties; }
            set 
            {
                properties = value;
                this.RaisePropertyChanged();
            }
        }
        private void SelectChanged(List<ObjectId> objIds) 
        {
            ClearHisData();
            if (null == objIds || objIds.Count < 1)
                return;
            List<string> allTypes = new List<string>();
            foreach (var item in objIds) 
            {
                var typeName = propertyService.GetShowTypeProperties(item, out Dictionary<string, object> properties);
                if (string.IsNullOrEmpty(typeName))
                    continue;
                var tempProp = new EntityProperties(item, typeName);
                foreach (var keyValue in properties) 
                {
                    var prop = new THProperties(keyValue.Key, keyValue.Value, false, false);
                    tempProp.Properties.Add(prop);
                }
                EntityProperties.Add(tempProp);
            }
            if (EntityProperties.Count < 1)
                return;
            var types = EntityProperties.Select(c => c.TypeName).Distinct().ToList();
            Count = EntityProperties.Count;
            IsMultipleType = types.Count > 1;
            TypeName = IsMultipleType? "多类别": types.First();
            Properties.Add(GetFirstRowData());
            if (!isMultipleType) 
            {
                foreach (var item in EntityProperties.First().Properties)
                {
                    Properties.Add(item);
                }
            }
        }
        private void ClearHisData() 
        {
            selectIds.Clear();
            EntityProperties.Clear();
            IsMultipleType = false;
            TypeName = "未选择";
            Properties.Clear();
            Count = 0;
        }
        private THProperties GetFirstRowData() 
        {
            var prop = new THProperties("构件类型", string.Format("{0}({1})", TypeName, Count),true,false);
            return prop;
        }
    }
    class EntityProperties 
    {
        public ObjectId EntityId { get; }
        public string TypeName { get; set; }
        public List<THProperties> Properties { get; }
        public EntityProperties(ObjectId objectId, string type) 
        {
            EntityId = objectId;
            TypeName = type;
            Properties = new List<THProperties>();
        }
    }
    class THProperties 
    {
        public string Name { get; }
        public object Value { get; set; }
        public bool? IsReadOnly { get; set; }
        public bool IsMultipleValue { get; set; }
        public THProperties(string name,object value,bool? isReadOnly,bool isMultiValue) 
        {
            Name = name;
            Value = value;
            IsReadOnly = isReadOnly;
            IsMultipleValue = isMultiValue;
        }
    }
}
