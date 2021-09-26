using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace HELLIONHarmony
{
    public static class Logger
    {
        public static void Write(params object[] objs)
        {
            string[] strs = new string[objs.Length];
            foreach (object obj in objs)
            {
                strs.Append(obj.ToString());
            }
            WriteLine(strs);
        }

        public static void Write(params string[] strs)
        {
            foreach (string str in strs)
            {
                Debug.Write(str + " ");
            }
            Debug.WriteLine("");
        }

        public static void WriteLine(params object[] objs)
        {
            string[] strs = new string[objs.Length];
            foreach(object obj in objs)
            {
                strs.Append(obj.ToString());
            }
            WriteLine(strs);
        }

        public static void WriteLine(params string[] strs)
        {
            foreach(string str in strs)
            {
                Debug.WriteLine(str);
            }
        }
    }
}
