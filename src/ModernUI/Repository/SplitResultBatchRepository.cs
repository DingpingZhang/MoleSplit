using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ModernUI.Repository
{
    class SplitResultBatchRepository
    {
        private static object _lockObject = new object();
        public static void WriteData(string filePath, string CAS, Dictionary<string, int> splitResult)
        {
            lock (_lockObject)
            {
                string data = CAS + "\t";
                if (splitResult == null || splitResult.Count == 0)
                {
                    data += "It is indivisible in this method.";
                }
                else
                {
                    foreach (var radical in splitResult)
                    {
                        data += (radical.Key + "\t" + radical.Value + "\t");
                    }
                }
                File.AppendAllText(filePath, data + "\r\n");
            }
        }
    }
}
