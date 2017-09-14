using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ModernUI.Repository
{
    class MethodRepository
    {
        public static Dictionary<string, string> FetchData()
        {
            var tempMethodNameList = Directory.GetFiles(@"DefineFiles");
            var methodDict = new Dictionary<string, string>();
            var r_GetMethodName = new Regex(@"\\(.+?)(_\d)?.mdef", RegexOptions.RightToLeft);

            for (int i = 0; i < tempMethodNameList.Length; i++)
            {
                var methodName = r_GetMethodName.Match(tempMethodNameList[i]).Groups[1].Value;

                if (!methodDict.ContainsKey(methodName))
                    methodDict.Add(methodName, tempMethodNameList[i]);
            }
            return methodDict;
        }
    }
}
