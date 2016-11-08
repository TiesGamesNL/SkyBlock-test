using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using log4net;
using FileSystem;

namespace LangM
{
    public class Lang
    {
        private ILog Log = LogManager.GetLogger(typeof(Lang));

        public Dictionary<string, string> lines = new Dictionary<string, string>();

        public Lang(string name, string path)
        {
            try
            {
                var lineas = File.ReadLines(path);
                foreach (string line in lineas)
                {
                    var line2 = line.Replace("\\n", "\n");
                    Console.WriteLine(line2);
                    string[] str = line2.Split('=');
                    string In = str[0];
                    string Out = str[1];
                    lines.Add(In, Out);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public string getString(string line)
        {
            string value;
            if (lines.TryGetValue(line, out value))
            {
                return value;
            }
            else
            {
                return "Error #4";
            }
        }
    }
}

