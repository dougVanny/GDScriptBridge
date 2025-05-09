using GDScriptBridge.Generator.Bridge;
using GDScriptBridge.Utils;
using System.Reflection;
using System.Text;

namespace GDScriptBridge.Bundler
{
	public class BaseGDBridgeBundle : BaseCodeBundle
	{
		public override string GetClassName()
		{
			return "BaseGDBridge";
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
				sb.Append("public enum BaseType");
				using (CodeBlock.Brackets(sb))
				{
					sb.Append($"{nameof(GDScriptClass.BaseType.UNKNOWN)},");
					sb.Append($"{nameof(GDScriptClass.BaseType.NODE)},");
					sb.Append($"{nameof(GDScriptClass.BaseType.RESOURCE)},");
				}

				sb.Append("[AttributeUsage(AttributeTargets.Class, Inherited = false)]");
				sb.Append("public class GDScriptPathAttribute : Attribute");
				using (CodeBlock.Brackets(sb))
				{
					sb.Append("public string godotPath;");
					sb.Append("public string gdClassName;");
					sb.Append("public BaseType baseType;");

					sb.Append($"public GDScriptPathAttribute(string godotPath, BaseType baseType = BaseType.{nameof(GDScriptClass.BaseType.UNKNOWN)})");
					using (CodeBlock.Brackets(sb))
					{
						sb.Append("this.godotPath = godotPath;");
						sb.Append("this.baseType = baseType;");
					}

					sb.Append($"public GDScriptPathAttribute(string godotPath, string gdClassName, BaseType baseType = BaseType.{nameof(GDScriptClass.BaseType.UNKNOWN)})");
					using (CodeBlock.Brackets(sb))
					{
						sb.Append("this.godotPath = godotPath;");
						sb.Append("this.gdClassName = gdClassName;");
						sb.Append("this.baseType = baseType;");
					}

					sb.Append("public GDScript LoadGDScript()");
					using (CodeBlock.Brackets(sb))
					{
						sb.Append("return GD.Load<GDScript>(godotPath);");
					}
				}

				sb.Append("public abstract class BaseGDBridge");
				using (CodeBlock.Brackets(sb))
				{
					sb.Append("public GodotObject godotObject;");
				}
			}

			return sb.ToString();
		}
	}
}
