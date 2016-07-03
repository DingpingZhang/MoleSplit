using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MoleSplit.SplitCore
{
    /// <summary>
    /// 化学键识别器
    /// </summary>
    internal class Bond : RecognizerBase
    {
        // ---------------------------------------------------------------------------------
        private List<string> _bondPattern;
        private Dictionary<string, string> _bondTag;
        private char[] _bondType = new char[] { '-', '=', '≡', '#' };
        // ---------------------------------------------------------------------------------
        public override void Load(string text)
        {
            var item = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            this._bondPattern = new List<string>();
            this._bondTag = new Dictionary<string, string>();
            for (int i = 0; i < item.Length; i++)
            {
                var temp = item[i].Split(new char[] { '?' }, StringSplitOptions.RemoveEmptyEntries);
                this._bondPattern.Add(temp[0]);
                if (temp.Length > 1)
                {
                    this._bondTag.Add(temp[0], temp[1]);
                }
            }
        }
        public override void Parse()
        {
            base.DefinedFragment = new Dictionary<string, int>();
            base.UndefinedFragment = new Dictionary<string, int>();

            var r = new Regex(@"\D\D?");
            for (int i = 1; i < base.Molecule.AtomList.Length; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (base.Molecule.AdjMat[i, j] != 0
                    && (base.Molecule.BondState[i, j] == -1 || base.Molecule.BondState[i, j] == 0))
                    {
                        var atom_1 = r.Match(base.Molecule.AtomList[i]).Value;
                        var atom_2 = r.Match(base.Molecule.AtomList[j]).Value;
                        var bondNum = (base.Molecule.AdjMat[i, j] - 1) 
                            + (base.Molecule.Charge[i] != 0 && base.Molecule.Charge[j] != 0 ? 1 : 0); // 如果键两端的原子带电荷，则相当于增加一个键

                        var type_1 = atom_1 + this._bondType[bondNum] + atom_2;
                        var type_2 = atom_2 + this._bondType[bondNum] + atom_1;

                        if (this._bondPattern.Contains(type_2)) { type_1 = type_2; }
                        if (this._bondPattern.Contains(type_1))
                        {
                            if (this._bondTag.ContainsKey(type_1))
                            {
                                type_1 += this.Molecule.BondState[i, j] < 0 ? '_' + this._bondTag[type_1] : "";
                            }
                            if (!base.DefinedFragment.ContainsKey(type_1)) { base.DefinedFragment.Add(type_1, 0); }
                            base.DefinedFragment[type_1]++;
                        }
                        else
                        {
                            if (!base.UndefinedFragment.ContainsKey(type_1)) { base.UndefinedFragment.Add(type_1, 0); }
                            base.UndefinedFragment[type_1]++;
                        }
                    }
                }
            }
            foreach (var item in this.Bond_H())
            {
                if (this._bondPattern.Contains(item.Key))
                {
                    base.DefinedFragment.Add(item.Key, item.Value);
                }
                else
                {
                    base.UndefinedFragment.Add(item.Key, item.Value);
                }
            }
        }
        // ---------------------------------------------------------------------------------
        private Dictionary<string, int> Bond_H()
        {
            var dict = new Dictionary<string, int>() { { "C", 4 }, { "Si", 4 }, { "O", 2 }, { "N", 3 }, { "S", 2 } };

            var recorder = new Dictionary<string, int>();
            var r = new Regex(@"^(\D\D?)(\d+?)_");
            for (int i = 0; i < base.Molecule.AtomList.Length; i++)
            {
                Match m = r.Match(base.Molecule.AtomList[i]);
                if (!dict.ContainsKey(m.Groups[1].Value)) { continue; }
                int temp = dict[m.Groups[1].Value] + base.Molecule.Charge[i];
                string bond = m.Groups[2].Value;
                for (int j = 0; j < bond.Length; j++)
                {
                    temp -= (bond[j] - 48);
                }
                if (temp > 0)
                {
                    var bond_H_Name = m.Groups[1].Value + "-H";
                    if (!recorder.ContainsKey(bond_H_Name)) { recorder.Add(bond_H_Name, 0); }
                    recorder[bond_H_Name] += temp;
                }
            }
            return recorder;
        }
    }
}
