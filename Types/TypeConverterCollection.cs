using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GDScriptBridge.Types
{
	public class TypeConverterCollection : ITypeConverter
	{
		const string ARRAY_GROUP_TYPE = "type";
		static readonly Regex ARRAY = new Regex(@"Array\s*\[\s*(?="+ ARRAY_GROUP_TYPE + @")\s*\]");

		const string DICTIONARY_GROUP_KEY = "key";
		const string DICTIONARY_GROUP_VALUE = "value";
		static readonly Regex DICTIONARY = new Regex(@"Dictionary\s*\[\s*(?="+ DICTIONARY_GROUP_KEY + @")\s*(?="+ DICTIONARY_GROUP_VALUE + @")\s*\]");

		List<ITypeConverter> typeConverters = new List<ITypeConverter>();

		public string GetConvertedType(string gdScriptType)
		{
			Match match;

			match = ARRAY.Match(gdScriptType);
			if (match != null)
			{
				string type = GetConvertedTypeFromList(match.Groups[ARRAY_GROUP_TYPE].Value);

				if (type == null) return null;

				return $"Godot.Collections.Array<{type}>";
			}

			match = DICTIONARY.Match(gdScriptType);
			if (match != null)
			{
				string key = GetConvertedTypeFromList(match.Groups[DICTIONARY_GROUP_KEY].Value);
				string value = GetConvertedTypeFromList(match.Groups[DICTIONARY_GROUP_VALUE].Value);

				if (key == null || value == null) return null;

				return $"Godot.Collections.Dictionary<{key},{value}>";
			}

			return GetConvertedTypeFromList(gdScriptType);
		}

		public string GetConvertedTypeFromList(string gdScriptType)
		{
            foreach (ITypeConverter typeConverter in typeConverters)
            {
				string ret = typeConverter.GetConvertedType(gdScriptType);

				if (ret != null) return ret;
			}

			return null;
        }

		public void Add(ITypeConverter typeConverter)
		{
			typeConverters.Add(typeConverter);
		}
	}
}
