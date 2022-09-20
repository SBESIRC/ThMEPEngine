using System.Collections.Generic;

namespace ThPlatform3D.Data
{
    internal interface ISetContainer
    {
        List<string> Containers { get; }
        void SetContainers(List<string> containers);
    }
}
