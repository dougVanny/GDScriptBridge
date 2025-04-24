using GDScriptBridge.Generator;
using GDScriptBridge.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GDScriptBridge.Types
{
	public class TypeInfoArray : TypeInfo
		{
			public TypeInfo arrayTypeInfo;

			public TypeInfoArray() : base(TypeConverterCollection.ARRAY_PURE, "Godot.Collections.Array")
			{
			}

			public TypeInfoArray(TypeInfo arrayTypeInfo) : base(
				$"{TypeConverterCollection.ARRAY_PURE}[{arrayTypeInfo.gdScriptName}]",
				arrayTypeInfo.isVariantCompatible ? $"Godot.Collections.Array<{arrayTypeInfo.cSharpName}>" : $"System.Collections.Generic.List<{arrayTypeInfo.cSharpName}>"
				)
			{
				this.arrayTypeInfo = arrayTypeInfo;
				isVariantCompatible = arrayTypeInfo.isVariantCompatible;
			}

			public override string CastFromVariant(string variantSymbol)
			{
				if (isVariantCompatible) return base.CastFromVariant(variantSymbol);

				StringBuilder sb = new StringBuilder();

				sb.Append($"{variantSymbol}.AsGodotArray().ToList().ConvertAll");
				using (CodeBlock.Parenthesis(sb))
				{
					sb.Append($"__variant => {arrayTypeInfo.CastFromVariant("__variant")}");
				}

				return sb.ToString();
			}

			public override string CastToVariant(string symbol)
			{
				if (isVariantCompatible) return base.CastFromVariant(symbol);

				StringBuilder sb = new StringBuilder();

				sb.Append("new Godot.Collections.Array");
				using (CodeBlock.Parenthesis(sb))
				{
					sb.Append($"{symbol}.ConvertAll");
					using (CodeBlock.Parenthesis(sb))
					{
						sb.Append($"__item => {arrayTypeInfo.CastToVariant("__item")}");
					}
				}

				return sb.ToString();
			}
		}

	public class TypeInfoDictionary : TypeInfo
	{
		public TypeInfo dictTypeKey;
		public TypeInfo dictTypeValue;

		public TypeInfoDictionary() : base(TypeConverterCollection.DICTIONARY_PURE, "Godot.Collections.Dictionary")
		{
		}

		public TypeInfoDictionary(TypeInfo arrayTypeKey, TypeInfo arrayTypeValue) : base(
			$"{TypeConverterCollection.DICTIONARY_PURE}[{arrayTypeKey.gdScriptName},{arrayTypeValue.gdScriptName}]",
			(arrayTypeKey.isVariantCompatible && arrayTypeValue.isVariantCompatible) ? $"Godot.Collections.Dictionary<{arrayTypeKey.cSharpName},{arrayTypeValue.cSharpName}>" : $"System.Collections.Generic.Dictionary<{arrayTypeKey.cSharpName},{arrayTypeValue.cSharpName}>"
			)
		{
			this.dictTypeKey = arrayTypeKey;
			this.dictTypeValue = arrayTypeValue;

			isVariantCompatible = arrayTypeKey.isVariantCompatible && arrayTypeValue.isVariantCompatible;
		}



		public override string CastFromVariant(string variantSymbol)
		{
			if (isVariantCompatible) return base.CastFromVariant(variantSymbol);

			StringBuilder sb = new StringBuilder();

			sb.Append($"{variantSymbol}.AsGodotDictionary().ToDictionary");
			using (CodeBlock.Parenthesis(sb))
			{
				sb.Append($"__kv => {dictTypeKey.CastFromVariant("__kv.Key")}");
				sb.Append(",");
				sb.Append($"__kv => {dictTypeKey.CastFromVariant("__kv.Value")}");
			}

			return sb.ToString();
		}

		public override string CastToVariant(string symbol)
		{
			if (isVariantCompatible) return base.CastToVariant(symbol);

			StringBuilder sb = new StringBuilder();

			sb.Append("new Godot.Collections.Dictionary<Variant,Variant>");
			using (CodeBlock.Parenthesis(sb))
			{
				sb.Append($"{symbol}.ToDictionary");
				using (CodeBlock.Parenthesis(sb))
				{
					sb.Append($"__kv => {dictTypeKey.CastToVariant("__kv.Key")}");
					sb.Append(",");
					sb.Append($"__kv => {dictTypeKey.CastToVariant("__kv.Value")}");
				}
			}

			return sb.ToString();
		}
	}

	public class TypeConverterCollection : ITypeConverter
	{
		public const string ARRAY_PURE = "Array";

		const string ARRAY_GROUP_TYPE = "type";
		static readonly Regex ARRAY = new Regex(ARRAY_PURE + @"\s*\[\s*(?<" + ARRAY_GROUP_TYPE + @">[^\s]+)\s*\]");

		public const string DICTIONARY_PURE = "Dictionary";

		const string DICTIONARY_GROUP_KEY = "key";
		const string DICTIONARY_GROUP_VALUE = "value";
		static readonly Regex DICTIONARY = new Regex(DICTIONARY_PURE + @"\s*\[\s*(?<" + DICTIONARY_GROUP_KEY + @">[^\s]+)\s*(?<" + DICTIONARY_GROUP_VALUE + @">[^\s]+)\s*\]");

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
