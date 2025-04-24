using System;
using System.Collections.Generic;
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

	public class TypeInfoClass : TypeInfo, ITypeInfoClass
	{
		Dictionary<string,TypeInfo> subTypes = new Dictionary<string, TypeInfo>();

		public TypeInfoClass(string name) : base(name)
		{
		}

		public TypeInfoClass(string gdScriptName, string cSharpName) : base(gdScriptName, cSharpName)
		{
		}

		public void AddSubType(string subTypeName, TypeInfo subTypeInfo)
		{
			subTypes.Add(subTypeName, subTypeInfo);
		}

		public TypeInfo GetSubType(string subType)
		{
			if (!subTypes.ContainsKey(subType)) return null;

			return subTypes[subType];
		}
	}

	public class TypeInfoEnum : TypeInfo
	{
		public List<string> options = new List<string>();

		public TypeInfoEnum(string name) : base(name)
		{
			isVariantCompatible = false;
		}

		public TypeInfoEnum(string gdScriptName, string cSharpName) : base(gdScriptName, cSharpName)
		{
			isVariantCompatible = false;
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
