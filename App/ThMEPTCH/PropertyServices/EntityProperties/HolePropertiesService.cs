using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using System.Linq;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.EntityProperties
{
    class HolePropertiesService : PropertyServiceBase
    {
        protected override PropertyBase DefaultProperties(ObjectId objectId)
        {
            var property = new HoleProperty(objectId)
            {
                ShowDimension = false,
                Hidden = false,
                BottomHeight = 1000.0,
                HoleHeight = 800.0,
                NumberPrefix = "C",
                NumberPostfix = "",
                ElevationDisplay = true,
            };
            return property;
        }
        protected override TypedValueList PropertyToXDataValue(PropertyBase property)
        {
            var slabHoleProp = property as HoleProperty;
            TypedValueList valueList = new TypedValueList
            {

            };
            return valueList;
        }
        protected override PropertyBase XDataProperties(ObjectId objectId, TypedValueList typedValues)
        {
            var property = new HoleProperty(objectId);
            //读取XData（第0个是AppName，不需要读取）,这边读数据的顺序要和写入数据的顺序一致
            for (int i = 1; i < typedValues.Count; i++)
            {
                var strData = typedValues.ElementAt(i).Value.ToString();
                switch (i)
                {

                }
            }
            return property;
        }
    }
}
