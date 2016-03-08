using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoleSplit
{
    interface IParser
    {
        /// <summary>
        /// 定义的碎片
        /// </summary>
        Dictionary<string, int> DefinedFragment { get; set; }
        /// <summary>
        /// 未定义的碎片
        /// </summary>
        Dictionary<string, int> UndefinedFragment { get; set; }
        /// <summary>
        /// 载入解析依据
        /// </summary>
        /// <param name="text">定义文件中的字段</param>
        void Load(string text);
        /// <summary>
        /// 解析数据
        /// </summary>
        void Parse();
    }
}
