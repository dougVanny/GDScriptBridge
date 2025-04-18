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
