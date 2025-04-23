using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GDScriptBridge.Types
{
	public class TypeInfoArray : TypeInfo
	{
		TypeInfo arrayTypeInfo;

		public TypeInfoArray() : base(TypeConverterCollection.ARRAY_PURE, "Godot.Collections.Array")
		{
		}

		public TypeInfoArray(TypeInfo arrayTypeInfo) : base($"{TypeConverterCollection.ARRAY_PURE}[{arrayTypeInfo.gdScriptName}]", $"Godot.Collections.Array<{arrayTypeInfo.cSharpName}>")
		{
			this.arrayTypeInfo = arrayTypeInfo;
		}
	}

	public class TypeInfoDictionary : TypeInfo
	{
		TypeInfo arrayTypeKey;
		TypeInfo arrayTypeValue;

		public TypeInfoDictionary() : base(TypeConverterCollection.DICTIONARY_PURE, "Godot.Collections.Dictionary")
		{
		}

		public TypeInfoDictionary(TypeInfo arrayTypeKey, TypeInfo arrayTypeValue) : base($"{TypeConverterCollection.DICTIONARY_PURE}[{arrayTypeKey.gdScriptName},{arrayTypeValue.gdScriptName}]", $"Godot.Collections.Dictionary<{arrayTypeKey.cSharpName},{arrayTypeValue.cSharpName}>")
		{
			this.arrayTypeKey = arrayTypeKey;
			this.arrayTypeValue = arrayTypeValue;
		}
	}

	public class TypeConverterCollection : ITypeConverter
	{
		public const string ARRAY_PURE = "Array";

		const string ARRAY_GROUP_TYPE = "type";
		static readonly Regex ARRAY = new Regex(ARRAY_PURE + @"\s*\[\s*(?<" + ARRAY_GROUP_TYPE + @">.+)\s*\]");

		public const string DICTIONARY_PURE = "Dictionary";

		const string DICTIONARY_GROUP_KEY = "key";
		const string DICTIONARY_GROUP_VALUE = "value";
		static readonly Regex DICTIONARY = new Regex(DICTIONARY_PURE + @"\s*\[\s*(?<" + DICTIONARY_GROUP_KEY + @">.+)\s*(?<"+ DICTIONARY_GROUP_VALUE + @">.+)\s*\]");

		List<ITypeConverter> typeConverters = new List<ITypeConverter>();

		public TypeInfo GetTypeInfo(string gdScriptType)
		{
			Match match;

			if (gdScriptType.Equals(ARRAY_PURE)) return new TypeInfoArray();
			if (gdScriptType.Equals(DICTIONARY_PURE)) return new TypeInfoDictionary();

			match = ARRAY.Match(gdScriptType);
			if (match.Success)
			{
				TypeInfo type = GetConvertedTypeFromList(match.Groups[ARRAY_GROUP_TYPE].Value);

				if (type == null) return null;

				return new TypeInfoArray(type);
			}

			match = DICTIONARY.Match(gdScriptType);
			if (match.Success)
			{
				TypeInfo key = GetConvertedTypeFromList(match.Groups[DICTIONARY_GROUP_KEY].Value);
				TypeInfo value = GetConvertedTypeFromList(match.Groups[DICTIONARY_GROUP_VALUE].Value);

				if (key == null || value == null) return null;

				return new TypeInfoDictionary(key, value);
			}

			return GetConvertedTypeFromList(gdScriptType);
		}

		public TypeInfo GetConvertedTypeFromList(string gdScriptType)
		{
            foreach (ITypeConverter typeConverter in typeConverters)
            {
				TypeInfo ret = typeConverter.GetTypeInfo(gdScriptType);

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
