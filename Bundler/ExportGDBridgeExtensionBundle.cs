using GDScriptBridge.Generator.Bridge;
using GDScriptBridge.Types;
using GDScriptBridge.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace GDScriptBridge.Bundler
{
	internal class ExportGDBridgeExtensionBundle : BaseCodeBundle
	{
		public override string GetClassName()
		{
			return "ExportGDBridgeExtension";
		}

		public override string Generate()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("using Godot;");
			sb.Append("using System;");
			sb.Append("using System.Reflection;");
			sb.Append("using System.Collections.Generic;");

			sb.Append("namespace GDScriptBridge.Bundled.Export");
			using (CodeBlock.Brackets(sb))
			{
				sb.Append($"public static class {GetClassName()}");
				using (CodeBlock.Brackets(sb))
				{
					sb.Append("public static Godot.Collections.Array<Godot.Collections.Dictionary> GetGDBridgePropertyList(this GodotObject godotObject)");
					using (CodeBlock.Brackets(sb))
					{
						sb.Append("Godot.Collections.Array<Godot.Collections.Dictionary> properties = [];");

						sb.Append("foreach (PropertyInfo property in godotObject.GetType().GetProperties())");
						using (CodeBlock.Brackets(sb))
						{
							sb.Append("ExportGDBridge exportGDBridge = property.GetCustomAttribute<ExportGDBridge>();");
							sb.Append("if (exportGDBridge == null) continue;");

							sb.Append("GDScriptPathAttribute scriptPathAttribute = property.PropertyType.GetCustomAttribute<GDScriptPathAttribute>();");
							sb.Append("if (scriptPathAttribute == null) continue;");
							sb.Append("if (scriptPathAttribute.baseType == BaseType.UNKNOWN) continue;");

							sb.Append("Godot.Collections.Dictionary propertyDict = new Godot.Collections.Dictionary();");

							sb.Append("propertyDict[\"name\"] = property.Name;");

							sb.Append("if (scriptPathAttribute.baseType == BaseType.NODE)");
							using (CodeBlock.Brackets(sb))
							{
								sb.Append("propertyDict[\"type\"] = (int)Variant.Type.NodePath;");

								sb.Append("if (scriptPathAttribute.gdClassName != null)");
								using (CodeBlock.Brackets(sb))
								{
									sb.Append("propertyDict[\"hint\"] = (int)PropertyHint.NodePathValidTypes;");
									sb.Append("propertyDict[\"hint_string\"] = scriptPathAttribute.gdClassName;");
								}
							}
							sb.Append("else");
							using (CodeBlock.Brackets(sb))
							{
								sb.Append("propertyDict[\"type\"] = (int)Variant.Type.Object;");
								sb.Append("propertyDict[\"hint\"] = (int)PropertyHint.ResourceType;");

								sb.Append("if (scriptPathAttribute.gdClassName != null)");
								using (CodeBlock.Brackets(sb))
								{
									sb.Append("propertyDict[\"hint_string\"] = scriptPathAttribute.gdClassName;");
								}
							}

							sb.Append("properties.Add(propertyDict);");
						}

						sb.Append("return properties;");
					}

					sb.Append("public static bool IsExportGDBridgeProperty(this GodotObject godotObject, StringName propertyName)");
					using (CodeBlock.Brackets(sb))
					{
						sb.Append("foreach (PropertyInfo property in godotObject.GetType().GetProperties())");
						using (CodeBlock.Brackets(sb))
						{
							sb.Append("ExportGDBridge exportGDBridge = property.GetCustomAttribute<ExportGDBridge>();");
							sb.Append("if (exportGDBridge == null) continue;");

							sb.Append("if (property.Name.Equals(propertyName)) return true;");
						}

						sb.Append("return false;");
					}
				}
			}

			return sb.ToString();
		}
	}
}
