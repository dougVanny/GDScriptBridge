using GDScriptBridge.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace GDScriptBridge.Bundler
{
	public class GDBridgeWrapperBundle : BaseCodeBundle
	{
		public override string GetClassName()
		{
			return "GDBridgeWrapper";
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
				sb.Append("public static class GDBridgeWrapper");
				using (CodeBlock.Brackets(sb))
				{
					sb.Append("public static T AsGDBridge<T>(this GodotObject godotObject) where T : BaseGDBridge, new()");
					using (CodeBlock.Brackets(sb))
					{
						sb.Append("GDScript classScript = typeof(T).GetCustomAttribute<GDScriptBridge.Bundled.ScriptPathAttribute>().LoadGDScript();");

						sb.Append("if (classScript.Equals((GDScript)godotObject.GetScript()))");
						using (CodeBlock.Brackets(sb))
						{
							sb.Append("return new T() { godotObject = godotObject };");
						}
						sb.Append("return null;");
					}
				}
			}

			return sb.ToString();
		}
	}
}
