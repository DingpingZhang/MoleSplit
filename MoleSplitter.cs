using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;

using MoleSplit;

namespace MoleSplit
{
    public class MoleSplitter
    {
        /// <summary>
        /// 定义的分子片段
        /// </summary>
        public Dictionary<string, int> DefinedFragment { get; protected set; }
        /// <summary>
        /// 未定义的分子片段
        /// </summary>
        public Dictionary<string, int> UndefineFragment { get; protected set; }
        /// <summary>
        /// 待解析的分子
        /// </summary>
        private MoleInfo _molecule;
        private List<ARecognizer> _recognizer;
        /// <summary>
        /// 加载mol文件
        /// </summary>
        /// <param name="filePath"></param>
        public void LoadMolFile(string filePath)
        {
            if (!(File.Exists(filePath) && new Regex(".mol$").IsMatch(filePath))) { return; }
            using (StreamReader sr = new StreamReader(filePath))
            {
                this._molecule = new MoleInfo(sr.ReadToEnd());
            }
        }
        /// <summary>
        /// 加载定义文件
        /// </summary>
        /// <param name="filePath"></param>
        public void LoadDefineFile(string filePath)
        {
            if (this._molecule != null) { this._molecule.State = new int[this._molecule.State.Length]; }

            if (!(File.Exists(filePath) && new Regex(".mdef$").IsMatch(filePath))) { return; }
            string[] temp;
            using (StreamReader sr = new StreamReader(filePath))
            {
                temp = sr.ReadToEnd().Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
            }
            this._recognizer = new List<ARecognizer>();
            for (int i = 0; i < temp.Length; i += 2)
            {
                var tempObj = (ARecognizer)Activator.CreateInstance(Type.GetType("MoleSplit." + temp[i]));
                tempObj.Load(temp[i + 1]);
                this._recognizer.Add(tempObj);
            }
        }
        /// <summary>
        /// 启动解析
        /// </summary>
        public void Parse()
        {
            // 1.添加属性
            for (int i = 0; i < this._recognizer.Count; i++)
            {
                this._recognizer[i].Molecule = this._molecule;
                if (this._recognizer[i] is IAddAttribute)
                {
                    ((IAddAttribute)this._recognizer[i]).AddAttribute();
                }
            }
            // 2.进行解析
            for (int i = 0; i < this._recognizer.Count; i++)
            {
                this._recognizer[i].Parse();
            }
            // 3.结算结果
            this.DefinedFragment = new Dictionary<string, int>();
            this.UndefineFragment = new Dictionary<string, int>();
            for (int i = 0; i < this._recognizer.Count; i++)
            {
                if (this._recognizer[i].DefinedFragment != null && this._recognizer[i].DefinedFragment.Count != 0)
                {
                    foreach (var item in this._recognizer[i].DefinedFragment)
                    {
                        this.DefinedFragment.Add(item.Key, item.Value);
                    }
                }
                if (this._recognizer[i].UndefinedFragment != null && this._recognizer[i].UndefinedFragment.Count != 0)
                {
                    foreach (var item in this._recognizer[i].UndefinedFragment)
                    {
                        this.UndefineFragment.Add(item.Key, item.Value);
                    }
                }
            }
        }
    }
}
