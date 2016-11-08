using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace AuthME
{
	public class LangManager
	{
		private Dictionary<string, Lang> Languages = new Dictionary<string, Lang>();

		public LangManager ()
		{
		}

		public void addLang(string name, Lang lang){
			Languages.Add(name, lang);
		}

		public Lang getLang(string name){
			Lang value;
			if (Languages.TryGetValue (name, out value)) {
				return value;
			} else {
				return Languages ["eng"];
			}
		}
	}
}

