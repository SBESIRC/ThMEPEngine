using System;
using System.Collections.Generic;
using ThMEPTCH.TCHXmlModels.TCHEntityModels;

namespace ThMEPTCH.TCHXmlDataConvert
{
    abstract class TCHConvertBase: ITCHXmlConvert
    {
        public List<TCHXmlEntity> TCHXmlEntities { get; }
        public List<Type> AcceptTCHEntityTypes { get; }
        public TCHConvertBase()
        {
            TCHXmlEntities = new List<TCHXmlEntity>();
            AcceptTCHEntityTypes = new List<Type>();
        }
        public virtual void InitData(List<TCHXmlEntity> tchXmlEntities)
        {
            TCHXmlEntities.Clear();
            if (null == tchXmlEntities || tchXmlEntities.Count < 1)
                return;
            foreach (var item in tchXmlEntities) 
            {
                if (item == null)
                    continue;
                TCHXmlEntities.Add(item);
            }
        }
        public abstract List<object> ConvertToBuidingElement();
    }
}
