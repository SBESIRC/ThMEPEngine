using System;
using System.Collections.Generic;

namespace TianHua.Electrical.PDS.Project.Module
{
    public static class ThPDSProjectGraphExtension
    {
        /// <summary>
        /// 拼装成图
        /// </summary>
        /// <param name="forest"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static ThPDSProjectGraph Assemble(this List<ThPDSProjectGraph> forest)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 拆解成树
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        public static List<ThPDSProjectGraph> Disassemble(this ThPDSProjectGraph graph)
        {
            throw new NotImplementedException();
        }
    }
}
