using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using log4net;

namespace AuthME
{
	public class Lang
	{
		private ILog Log = LogManager.GetLogger(typeof(Lang));

		public Dictionary<string, string> lines = new Dictionary<string, string>();

		public Lang (string name, string path)
		{
			//FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
			var lineas = File.ReadLines(path);
			foreach(string line in lineas){
                Console.WriteLine(line);
				string[] str = line.Split('=');
				string In = str[0];
				string Out = str [1];
				lines.Add (In, Out);
			}
		}

		public string getString(string line){
			string value;
			if (lines.TryGetValue (line, out value)) {
				return value;
			} else {
				return "Error #4";
			}
		}
	}
}

