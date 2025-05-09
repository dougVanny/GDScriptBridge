using GDScriptBridge.Bundler;
using GDScriptBridge.Generator.Bridge;
using GDScriptBridge.Types;
using GDScriptBridge.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GDScriptBridge.Generator.Export
{
	public class ExportPropertiesGenerator : BaseCodeBundle
	{
		ExportPropertiesLookup.UserClass userClass;

		public ExportPropertiesGenerator(ExportPropertiesLookup.UserClass userClass)
		{
			this.userClass = userClass;
		}

		public override string GetClassName()
		{
			return userClass.classDeclaration.Identifier.Text;
		}

		public override string Generate()
		{
			StringBuilder sb = new StringBuilder();

			List<string> nameSpaceParts = new List<string>();
			ExportPropertiesLookup.NameSpace nameSpace = userClass.nameSpace;

			while (nameSpace != null)
			{
				if (nameSpace.name != null) nameSpaceParts.Insert(0, nameSpace.name);

				nameSpace = nameSpace.parent;
			}

			sb.Append("using Godot;");
			sb.Append("using GDScriptBridge.Bundled;");

			if (nameSpaceParts.Count > 0) sb.Append($"namespace {string.Join(".", nameSpaceParts)};");

			sb.Append($"{string.Join(" ", userClass.classDeclaration.Modifiers.ToList().ConvertAll(m => m.Text))} class {GetClassName()}");
			using (CodeBlock.Brackets(sb))
			{
                foreach (ExportPropertiesLookup.UserClass.Property property in userClass.properties)
                {
					GDScriptClass.BaseType propertyBaseType = property.type.FindBaseType();

					if (propertyBaseType == GDScriptClass.BaseType.UNKNOWN) continue;

					sb.Append($"{string.Join(" ",property.modifiers)} {property.type.fullCSharpName} {property.name}");
					using (CodeBlock.Brackets(sb))
					{
						sb.Append("get");
						using (CodeBlock.Brackets(sb))
						{
							sb.Append($"if (!HasMeta(nameof({property.name}))) return null;");

							sb.Append($"Variant meta = GetMeta(nameof({property.name}));");

							if (propertyBaseType == GDScriptClass.BaseType.NODE)
							{
								sb.Append($"if (meta.VariantType != Variant.Type.NodePath) return null;");

								sb.Append($"Node node = GetNodeOrNull(meta.AsNodePath());");

								sb.Append($"if (node == null) return null;");

								sb.Append($"return node.AsGDBridge<{property.type.fullCSharpName}>();");
							}
							else if (propertyBaseType == GDScriptClass.BaseType.RESOURCE)
							{
								sb.Append($"if (meta.VariantType != Variant.Type.Object) return null;");

								sb.Append($"GodotObject obj = meta.AsGodotObject();");

								sb.Append($"if (obj == null) return null;");

								sb.Append($"return obj.AsGDBridge<{property.type.fullCSharpName}>();");
							}
						}

						sb.Append("set");
						using (CodeBlock.Brackets(sb))
						{
							sb.Append($"SetMeta(nameof({property.name}), value.godotObject);");
						}
					}
				}
            }

			return sb.ToString();
		}
	}
}
