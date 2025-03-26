using System;
using System.Collections.Generic;
using System.Text;

namespace GDScriptBridge.Utils
{
	public class UniqueSymbolConverter
	{
		Func<string, string> stringConvertion;
		Dictionary<string, string> map = new Dictionary<string, string>();

		public UniqueSymbolConverter(Func<string, string> stringConvertion)
		{
			this.stringConvertion = stringConvertion;
		}

		public string Convert(string input)
		{
			if (!map.ContainsKey(input))
			{
				string converted = stringConvertion.Invoke(input);

				if (map.ContainsValue(converted))
				{
					int counter = 0;

					while (map.ContainsValue(converted + ++counter)) ;

					converted = converted + counter;
				}

				map.Add(input, converted);
			}

			return map[input];
		}
	}
}
