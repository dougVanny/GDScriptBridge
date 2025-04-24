using GDScriptBridge.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDScriptBridge.Types
{
	public class TypeInfo
	{
		public string gdScriptName;
		public string cSharpName;
		public bool isVariantCompatible = true;

		public TypeInfo(string name)
		{
			gdScriptName = name;
			cSharpName = name;
		}

		public TypeInfo(string gdScriptName, string cSharpName)
		{
			this.gdScriptName = gdScriptName;
			this.cSharpName = cSharpName;
		}

		public virtual string CastFromVariant(string variantSymbol)
		{
			return $"({cSharpName}){variantSymbol}";
		}

		public virtual string CastToVariant(string symbol)
		{
			return $"{symbol}";
		}
	}

	public class TypeInfoVariant : TypeInfo
	{
		public TypeInfoVariant() : base("Godot.Variant")
		{
		}

		public override string CastFromVariant(string variantSymbol)
		{
			return $"{variantSymbol}";
		}
	}

	public interface ITypeInfoClass
	{
		public TypeInfo GetSubType(string subType);
	}

	public class TypeInfoEnum : TypeInfo
	{
		public class OptionInfo
		{
			public string name;
			public string cSharpName;
			public OperationEvaluation value;
		}

		UniqueSymbolConverter uniqueOptionSymbolConverter;

		List<string> orderedOptions = new List<string>();
		Dictionary<string, OptionInfo> optionInfo = new Dictionary<string, OptionInfo>();


		public TypeInfoEnum(string name) : base(name)
		{
			isVariantCompatible = false;
		}

		public TypeInfoEnum(string gdScriptName, string cSharpName) : base(gdScriptName, cSharpName)
		{
			isVariantCompatible = false;
		}

		public void SetUniqueOptionSymbolConverter(UniqueSymbolConverter uniqueOptionSymbolConverter)
		{
			this.uniqueOptionSymbolConverter = uniqueOptionSymbolConverter;
		}

		public void AddOption(string optionName, string optionCSharpName = null)
		{
			orderedOptions.Add(optionName);

			if (optionCSharpName == null) optionCSharpName = uniqueOptionSymbolConverter == null ? optionName : uniqueOptionSymbolConverter.Convert(optionName);

			optionInfo[optionName] = new OptionInfo
			{
				name = optionName,
				cSharpName = optionCSharpName
			};
		}

		public void SetOptionValue(string optionName, OperationEvaluation optionValue)
		{
			optionInfo[optionName].value = optionValue;
		}

		public bool IsValidEnum
		{
			get
			{
				return optionInfo.Values.All(option => option.value == null || option.value.type == OperationEvaluation.Type.Long);
			}
		}

		public IEnumerable<(OptionInfo optionInfo, long optionValue)> Options
		{
			get
			{
				long value = 0;

                foreach (string optionName in orderedOptions)
                {
					if (optionInfo[optionName].value != null)
					{
						value = optionInfo[optionName].value.longValue;
					}

					yield return (optionInfo[optionName], value);

					value++;
				}
			}
		}

		public override string CastFromVariant(string variantSymbol)
		{
			return $"({cSharpName})({variantSymbol}.AsInt32())";
		}

		public override string CastToVariant(string symbol)
		{
			return $"Variant.CreateFrom((int){symbol})";
		}
	}
}
