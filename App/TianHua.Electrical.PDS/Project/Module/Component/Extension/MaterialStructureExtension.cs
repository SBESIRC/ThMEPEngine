using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Component.Extension
{
    public static class MaterialStructureExtension
    {
        public static ScopeOfApplicationType GetScopeOfApplicationType(this MaterialStructure materialStructure)
        {
            Type type = materialStructure.GetType();
            MemberInfo[] memberInfos = type.GetMember(materialStructure.ToString());
            if(!memberInfos.IsNull() && memberInfos.Length > 0)
            {
                var attrs = memberInfos[0].GetCustomAttributes(typeof(ScopeOfApplicationAttribute),false);
                if (!attrs.IsNull() && attrs.Length > 0)
                {
                    return (attrs[0] as ScopeOfApplicationAttribute).scopeOfApplicationType;
                }
            }
            throw new NotSupportedException();
        }

        public static CableType GetCableTypeType(this MaterialStructure materialStructure)
        {
            Type type = materialStructure.GetType();
            MemberInfo[] memberInfos = type.GetMember(materialStructure.ToString());
            if (!memberInfos.IsNull() && memberInfos.Length > 0)
            {
                var attrs = memberInfos[0].GetCustomAttributes(typeof(CableTypeAttribute), false);
                if (!attrs.IsNull() && attrs.Length > 0)
                {
                    return (attrs[0] as CableTypeAttribute).cableType;
                }
            }
            throw new NotSupportedException();
        }

        public static InsulationType GetInsulationType(this MaterialStructure materialStructure)
        {
            Type type = materialStructure.GetType();
            MemberInfo[] memberInfos = type.GetMember(materialStructure.ToString());
            if (!memberInfos.IsNull() && memberInfos.Length > 0)
            {
                var attrs = memberInfos[0].GetCustomAttributes(typeof(InsulationAttribute), false);
                if (!attrs.IsNull() && attrs.Length > 0)
                {
                    return (attrs[0] as InsulationAttribute).InsulationType;
                }
            }
            throw new NotSupportedException();
        }

        public static MaterialType GetMaterialType(this MaterialStructure materialStructure)
        {
            Type type = materialStructure.GetType();
            MemberInfo[] memberInfos = type.GetMember(materialStructure.ToString());
            if (!memberInfos.IsNull() && memberInfos.Length > 0)
            {
                var attrs = memberInfos[0].GetCustomAttributes(typeof(MaterialAttribute), false);
                if (!attrs.IsNull() && attrs.Length > 0)
                {
                    return (attrs[0] as MaterialAttribute).MaterialType;
                }
            }
            throw new NotSupportedException();
        }

        public static List<MaterialStructure> GetSameMaterialStructureGroup(this ScopeOfApplicationType scopeOfApplicationType)
        {
            var type = typeof(MaterialStructure);
            List<MaterialStructure> result = new List<MaterialStructure>();
            foreach (MaterialStructure item in type.GetEnumValues())
            {
                if(item.GetScopeOfApplicationType().Equals(scopeOfApplicationType))
                {
                    result.Add(item);
                }
            }
            return result.OrderBy(o => o.GetInsulationType()).ThenBy(o => o.GetCableTypeType()).ThenBy(o => o.ToString()).ToList();
        }
    }
}
