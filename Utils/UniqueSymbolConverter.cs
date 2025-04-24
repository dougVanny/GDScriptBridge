using System;
using System.Collections.Generic;
using System.Text;

namespace GDScriptBridge.Utils
{
	public class UniqueSymbolConverter
	{
		Func<string, string> stringConvertion;
		Dictionary<string, string> map = new Dictionary<string, string>();

		public UniqueSymbolConverter(Func<string, string> stringConvertion = null)
		{
			this.stringConvertion = stringConvertion;
		}

		public string Convert(string input)
		{
			if (!map.ContainsKey(input))
			{
				string converted = input;
				
				if(stringConvertion!=null) converted = stringConvertion.Invoke(converted);

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

		public static string ToTitleCase(string name)
		{
			string[] nameParts = name.Split('_');

			for (int i = 0; i < nameParts.Length; i++)
			{
				if (nameParts[i].Length == 0) continue;
				if (nameParts[i].Length == 1)
				{
					nameParts[i] = nameParts[i].ToUpper();
					continue;
				}

				if (nameParts[i].ToUpper().Equals(nameParts[i]))
				{
					nameParts[i] = nameParts[i][0] + nameParts[i].Substring(1).ToLower();
				}
				else
				{
					nameParts[i] = char.ToUpper(nameParts[i][0]) + nameParts[i].Substring(1);
				}
			}

			string newName = string.Join("", nameParts);
			if (name[0] == '_') newName = "_" + newName;
			return newName;
		}
	}
}
