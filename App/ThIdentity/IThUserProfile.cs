using System;

namespace ThIdentity
{
    public interface IThUserProfile : IDisposable
    {
        // 姓名
        string Name { get;  }

        // 职称
        string Title { get; }

        // 公司名
        string Company { get; }

        // 部门
        string Department { get; }

        // 邮箱
        string Mail { get; }

        // 账号
        string Accountname { get; }

        bool IsDomainUser();
    }
}
