using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoleSplit
{
    public class MoleculeFragment
    {
        public MoleculeFragment(string name, int count)
        {
            Name = name;
            Count = count;
        }

        public string Name { get; }
        public int Count { get; set; }
    }
}
