using System;
using System.Collections.Generic;
using System.Text;

namespace GDScriptBridge.Types
{
	public class TypeInfo
	{
		public string gdScriptName;
		public string cSharpName;

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
		}

		public TypeInfoEnum(string gdScriptName, string cSharpName) : base(gdScriptName, cSharpName)
		{
		}
	}
}
