using GDScriptBridge.Generator.Bridge;
using GDScriptBridge.Types;
using GDScriptBridge.Utils;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static GDScriptBridge.Generator.Bridge.GDScriptClass;

namespace GDScriptBridge.Bundler
{
    public class GDBridgeExtensionBundle : BaseCodeBundle
	{
		GDScriptFolder gdScriptFolder;

		public GDBridgeExtensionBundle(GDScriptFolder gdScriptFolder)
		{
			this.gdScriptFolder = gdScriptFolder;
		}

		public override string GetClassName()
		{
			return "GDBridgeExtension";
		}

		public override string Generate()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("using Godot;");
			sb.Append("using System;");
			sb.Append("using System.Reflection;");
			sb.Append("using System.Collections.Generic;");

			sb.Append("namespace GDScriptBridge.Bundled");
			using (CodeBlock.Brackets(sb))
			{
				sb.Append($"public static class {GetClassName()}");
				using (CodeBlock.Brackets(sb))
				{
					sb.Append("private static Dictionary<string, Type> knownBridgeTypes = new Dictionary<string, Type>()");
					using (CodeBlock.Brackets(sb))
					{
                        foreach (GDScriptClassFile file in StringBuilderIterable.Comma(sb,gdScriptFolder.GetFiles()))
                        {
							sb.Append($"{{@\"{file.godotScriptPath}\",typeof({file.gdScriptClass.fullCSharpName})}}");
                        }
                    }
					sb.Append(";");

					sb.Append("private static Dictionary<GodotObject, Tuple<GDScript,BaseGDBridge>> objectCache = new Dictionary<GodotObject, Tuple<GDScript,BaseGDBridge>>();");

					sb.Append("public static BaseGDBridge AsGDBridge(this GodotObject godotObject)");
					using (CodeBlock.Brackets(sb))
					{
						sb.Append("Script script = (Script)godotObject.GetScript();");

						sb.Append("if(script==null || script is not GDScript) return null;");

						sb.Append("if(!knownBridgeTypes.ContainsKey(script.ResourcePath)) return null;");

						sb.Append("GDScript gdScript = (GDScript)script;");

						sb.Append("if(objectCache.ContainsKey(godotObject) && objectCache[godotObject].Item1 != gdScript)");
						using (CodeBlock.Brackets(sb))
						{
							sb.Append("objectCache.Remove(godotObject);");
						}

						sb.Append("if(!objectCache.ContainsKey(godotObject))");
						using (CodeBlock.Brackets(sb))
						{
							sb.Append("BaseGDBridge newBridge = (BaseGDBridge)knownBridgeTypes[script.ResourcePath].GetConstructor(Type.EmptyTypes).Invoke(null);");

							sb.Append("newBridge.godotObject = godotObject;");

							sb.Append("objectCache.Add(godotObject, new Tuple<GDScript,BaseGDBridge>(gdScript, newBridge));");
						}

						sb.Append("return objectCache[godotObject].Item2;");
					}

					sb.Append("public static T AsGDBridge<T>(this GodotObject godotObject) where T : BaseGDBridge");
					using (CodeBlock.Brackets(sb))
					{
						sb.Append("return godotObject.AsGDBridge() as T;");
					}

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
