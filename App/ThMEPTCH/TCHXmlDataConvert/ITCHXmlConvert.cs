using System;
using System.Collections.Generic;
using ThMEPTCH.TCHXmlModels.TCHEntityModels;

namespace ThMEPTCH.TCHXmlDataConvert
{
    interface ITCHXmlConvert
    {
        /// <summary>
        /// 根据哪些天正实体进行运算，如果需要多种组合，传入多个
        /// </summary>
        List<Type> AcceptTCHEntityTypes { get; }
        /// <summary>
        /// 根据过滤条件作为后面运算的天正元素
        /// </summary>
        List<TCHXmlEntity> TCHXmlEntities { get; }
        /// <summary>
        /// 初始化基础数据
        /// </summary>
        void InitData(List<TCHXmlEntity> tchXmlEntities);
        /// <summary>
        /// 转换数据
        /// </summary>
        /// <returns></returns>
        List<object> ConvertToBuidingElement();
    }
}
