using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using MoleSplit;

namespace MoleSplit
{
    class Lydersen : ARecognizer, IAddAttribute
    {
        public void AddAttribute()
        {
            var prr = new Ring();
            prr.Molecule = base.Molecule;
            var rings = prr.GetEachRing(); // 得到所有的环索引
            var record = new Dictionary<int, int>();
            for (int i = 0; i < rings.Length; i++)
            {
                int k = 0;
                for (; k < rings[i].Length; k++) // 检测该环是否是饱和环
                {
                    if (!new Regex("^\\w1").IsMatch(base.Molecule.AtomList[rings[i][k]])) { break; }
                }
                if (k < rings[i].Length) { continue; }
                for (int j = 0; j < rings[i].Length; j++)
                {
                    if (new Regex("^C111_").IsMatch(base.Molecule.AtomList[rings[i][j]])) // 记录C111索引的频次
                    {
                        if (!record.ContainsKey(rings[i][j])) { record.Add(rings[i][j], 0); }
                        record[rings[i][j]]++;
                    }
                }
            }
            foreach (var item in record)
            {
                if (item.Value > 1) { base.Molecule.AtomList[item.Key] += "_SPEC"; }
            }
        }
    }
}
