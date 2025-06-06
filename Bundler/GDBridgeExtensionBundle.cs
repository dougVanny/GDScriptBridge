﻿using GDScriptBridge.Generator.Bridge;
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
						sb.Append("BaseGDBridge ret = godotObject.AsGDBridge();");
						
						sb.Append("if(ret == null) return null;");
						
						sb.Append("return ret as T;");
					}
				}
			}

			return sb.ToString();
		}
	}
}
