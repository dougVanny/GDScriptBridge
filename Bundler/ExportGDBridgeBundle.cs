using GDScriptBridge.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace GDScriptBridge.Bundler
{
	public class ExportGDBridgeBundle : BaseCodeBundle
	{
		public override string GetClassName()
		{
			return "ExportGDBridge";
		}

		public override string Generate()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("using Godot;");
			sb.Append("using System;");
			sb.Append("using System.Reflection;");

			sb.Append("namespace GDScriptBridge.Bundled");
			using (CodeBlock.Brackets(sb))
			{
				sb.Append("[AttributeUsage(AttributeTargets.Property)]");
				sb.Append("public class ExportGDBridge : Attribute");
				using (CodeBlock.Brackets(sb))
				{
				}

				sb.Append("[AttributeUsage(AttributeTargets.Class, Inherited = true)]");
				sb.Append("public class GDBridgeSkipPropertyOverride : Attribute");
				using (CodeBlock.Brackets(sb))
				{
				}
			}

			return sb.ToString();
		}
	}
}
