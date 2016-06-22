using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoleSplit.SplitCore
{
    /// <summary>
    /// 原子识别器
    /// </summary>
    class Atom : RecognizerBase
    {
        // ---------------------------------------------------------------------------------
        private Dictionary<string, string> _atomPattern;
        private Dictionary<string, string[]> _atomTag;
        // ---------------------------------------------------------------------------------
        public override void Load(string text)
        {
            this._atomPattern = new Dictionary<string, string>();
            this._atomTag = new Dictionary<string, string[]>();
            string[] item = text.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < item.Length; i++)
            {
                string[] atom = item[i].Split(new char[] { '\n', '\r', ':' }, StringSplitOptions.RemoveEmptyEntries);
                var temp = atom[0].Split(new char[] { '?' }, StringSplitOptions.RemoveEmptyEntries);
                this._atomPattern.Add(atom[1], temp[0]);
                if (temp.Length > 1)
                {
                    var tags = new string[temp.Length - 1];
                    for (int j = 0; j < tags.Length; j++)
                    {
                        tags[j] = temp[j + 1];
                    }
                    this._atomTag.Add(atom[1], tags);
                }
            }
        }

        public override void Parse()
        {
            base.DefinedFragment = new Dictionary<string, int>();
            base.UndefinedFragment = new Dictionary<string, int>();

            for (int i = 0; i < base.Molecule.AtomList.Length; i++)
            {
                if (this.Molecule.AtomState[i] != 0) { continue; }
                string atomCode = base.Molecule.AtomList[i].Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries)[0];
                if (this._atomPattern.ContainsKey(atomCode))
                {
                    var trueName = this._atomPattern[atomCode];
                    if (this._atomTag.ContainsKey(atomCode))
                    {
                        trueName += this.RecAttribute(this._atomTag[atomCode], i);
                    }
                    if (!base.DefinedFragment.ContainsKey(trueName)) { base.DefinedFragment.Add(trueName, 0); }
                    base.DefinedFragment[trueName]++;
                }
                else
                {
                    if (!base.UndefinedFragment.ContainsKey(atomCode)) { base.UndefinedFragment.Add(atomCode, 0); }
                    base.UndefinedFragment[atomCode]++;
                }
            }
        }
        // ---------------------------------------------------------------------------------
        private string RecAttribute(string[] attributeTag, int index)
        {
            string attribute = "";
            for (int i = 0; i < attributeTag.Length; i++)
            {
                if (attributeTag[i][0] != '-')
                {
                    attribute = base.Molecule.AtomList[index].Contains(attributeTag[i]) ? '_' + attributeTag[i] : "";
                }
                else
                {
                    string tempTag = attributeTag[i].Remove(0, 1);
                    if (base.Molecule.AtomList[index].Contains(tempTag)) { continue; } // 1.自己不能含有该属性
                    for (int j = 0; j < base.Molecule.AtomList.Length; j++)
                    {
                        if (this.Molecule.AdjMat[index, j] != 0
                         && base.Molecule.AtomList[j].Contains(tempTag)) // 2.自己所连的原子中，具有该属性
                        {
                            attribute = '_' + attributeTag[i];
                            break;
                        }
                    }
                }
                if (attribute != "") { return attribute; }
            }
            return "";
        }
    }
}
