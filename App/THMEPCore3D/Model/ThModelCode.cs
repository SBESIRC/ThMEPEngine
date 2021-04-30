using System.Collections.Generic;

namespace THMEPCore3D.Model
{
    public class ThModelCode: ThModelBase
    {
        public ThModelCode() :base()
        {
        }
        public ThModelCode(Dictionary<string, string> properties):base(properties)
        {
            SetValue();
        }
        public string ModelId
        {
            get;
            set;
        }
        public string ModelSubEntryId
        {
            get;
            set;
        }

        protected override void SetValue()
        {
            foreach(var item in Properties)
            {
                string key = item.Key.ToUpper();
                if(key == "ModelId".ToUpper())
                {
                    this.ModelId = item.Value;
                }
                else if(key == "ModelSubEntryId".ToUpper())
                {
                    this.ModelSubEntryId = item.Value;
                }
                else
                {
                    continue;
                }
            }
        }
    }
}
