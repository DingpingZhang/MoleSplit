using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoleSplit.SplitCore
{
    /// <summary>
    /// 识别器抽象类（所有识别器继承自该类）
    /// </summary>
    abstract class ARecognizer
    {
        /// <summary>
        /// 定义的碎片
        /// </summary>
        public Dictionary<string, int> DefinedFragment { get; protected set; }
        /// <summary>
        /// 未定义的碎片
        /// </summary>
        public Dictionary<string, int> UndefinedFragment { get; protected set; }
        /// <summary>
        /// 待解析的分子
        /// </summary>
        public MoleInfo Molecule { get; set; }
        /// <summary>
        /// 载入解析依据
        /// </summary>
        /// <param name="text">定义文件中的字段</param>
        public virtual void Load(string text) { return; }
        /// <summary>
        /// 解析数据
        /// </summary>
        public virtual void Parse() { return; }
    }
}
