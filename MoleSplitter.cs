using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;

using MoleSplit.SplitCore;

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

        /// <summary>
        /// 解析器组
        /// </summary>
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
            if (this._molecule != null) { this._molecule.AtomState = new int[this._molecule.AtomState.Length]; }

            if (!(File.Exists(filePath) && new Regex(".mdef$").IsMatch(filePath))) { return; }
            string[] temp;
            using (StreamReader sr = new StreamReader(filePath))
            {
                temp = sr.ReadToEnd().Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
            }
            this._recognizer = new List<ARecognizer>();
            for (int i = 0; i < temp.Length; i += 2)
            {
                //var tempObj = (ARecognizer)Activator.CreateInstance(Type.GetType("MoleSplit." + temp[i]));
                //tempObj.Load(temp[i + 1]);
                this._recognizer.Add(this.ProductParser(temp[i], temp[i + 1]));
            }
        }

        /// <summary>
        /// 创建识别器的实例（放弃Activator.CreateInstance()创建而选择switch-case创建，是为了解决使用代码混淆器加密后，类名被替换的问题）
        /// </summary>
        /// <param name="className">识别器名称</param>
        /// <param name="param">识别器所需的参数</param>
        /// <returns>一个识别器实例</returns>
        private ARecognizer ProductParser(string className, string param)
        {
            ARecognizer recognizer;
            switch (className)
            {
                case "Radical": recognizer = new Radical();
                    break;
                case "Ring": recognizer = new Ring();
                    break;
                case "Atom": recognizer = new Atom();
                    break;
                case "Bond": recognizer = new Bond();
                    break;
                case "Element": recognizer = new Element();
                    break;
                default: throw new MemberAccessException("程序集中不存在名称为" + className + "的识别器。");
            }
            recognizer.Load(param);
            return recognizer;
        }

        /// <summary>
        /// 启动解析
        /// </summary>
        public void Parse()
        {
            if (this._recognizer == null || this._molecule == null) { return; }

            // 1.进行解析
            for (int i = 0; i < this._recognizer.Count; i++)
            {
                this._recognizer[i].Molecule = this._molecule;
                this._recognizer[i].Parse();
            }
            // 2.结算结果
            this.DefinedFragment = new Dictionary<string, int>();
            this.UndefineFragment = new Dictionary<string, int>();
            for (int i = 0; i < this._recognizer.Count; i++)
            {
                if (this._recognizer[i].DefinedFragment != null && this._recognizer[i].DefinedFragment.Count != 0)
                {
                    foreach (var item in this._recognizer[i].DefinedFragment)
                    {
                        if (!this.DefinedFragment.ContainsKey(item.Key))
                        {
                            this.DefinedFragment.Add(item.Key, item.Value);
                        }
                    }
                }
                if (this._recognizer[i].UndefinedFragment != null && this._recognizer[i].UndefinedFragment.Count != 0)
                {
                    foreach (var item in this._recognizer[i].UndefinedFragment)
                    {
                        if (!this.UndefineFragment.ContainsKey(item.Key))
                        {
                            this.UndefineFragment.Add(item.Key, item.Value);
                        }
                    }
                }
            }
        }

    }
}
