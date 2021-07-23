using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDTreeNode
    {
        public Point3d Node { get; set; }

        public ThDrainageSDTreeNode Parent { get; set; }

        public List<ThDrainageSDTreeNode> Child { get; set; }

        public ThDrainageSDTreeNode(Point3d pt)
        {
            Node = pt;
            Child = new List<ThDrainageSDTreeNode>();

        }

        public List<ThDrainageSDTreeNode> getSibling()
        {
            var sibling = new List<ThDrainageSDTreeNode>();
            if (Parent != null)
            {
                sibling = Parent.Child.Where(x => x != this).ToList();
            }

            return sibling;
        }

        public List<ThDrainageSDTreeNode> getDescendant()
        {
            var descendant = new List<ThDrainageSDTreeNode>();

            var nextChild = Child;

            if (nextChild.Count != 0)
            {
                descendant.AddRange(nextChild);

                foreach (var c in nextChild)
                {
                    descendant.AddRange(c.getDescendant());
                }
            }

            return descendant;
        }

        public int getLeafCount()
        {
            int i = 0;

            foreach (var c in Child)
            {
                i = i + c.getLeafCount();
            }

            if (Child.Count() == 0)
            {
                i = i + 1;
            }

            return i;
        }

        public List<ThDrainageSDTreeNode> getLeaf()
        {
            var leaf = new List<ThDrainageSDTreeNode>();

            foreach (var c in Child)
            {
                leaf.AddRange(c.getLeaf());
            }

            if (Child.Count() == 0)
            {
                leaf.Add(this);
            }

            return leaf;
        }

        public bool isDescendant(ThDrainageSDTreeNode node)
        {
            bool bReturn = false;

            foreach (var c in node.Child)
            {
               
                bReturn= isDescendant(c);

                if (bReturn == true)
                {
                    break;
                }

            }

            if (node == this)
            {
                bReturn = true;
            }

            return bReturn;
        }

        public int getDepth()
        {
            int i = 0;
            var bParent = Parent;

            while (bParent != null)
            {
                i = i + 1;
                bParent = bParent.Parent;
            }

            return i;
        }
    }
}
