using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using GDScriptBridge.Utils;

namespace GDScriptBridge.Types
{
	public class GodotTypes : ITypeConverter
	{
		const string GODOT_NAMESPACE = "Godot";
		const string GODOT_OBJECT_NAME = "GodotObject";
		const string ENUM_SUFFIX = "Enum";

		Dictionary<string, TypeInfo> knownTypes = new Dictionary<string, TypeInfo>();

		public GodotTypes(GeneratorExecutionContext context)
		{
			foreach (IAssemblySymbol assemblySymbol in context.Compilation.SourceModule.ReferencedAssemblySymbols)
            {
				foreach (INamespaceSymbol namespaceSymbol in assemblySymbol.GlobalNamespace.GetAllNamespaces())
				{
					if (GODOT_NAMESPACE.Equals(namespaceSymbol.Name))
					{
						INamedTypeSymbol godotObject = null;
						foreach (INamedTypeSymbol typeMember in namespaceSymbol.GetTypeMembers())
						{
							if (GODOT_OBJECT_NAME.Equals(typeMember.Name))
							{
								godotObject = typeMember;

								break;
							}
						}

						if (godotObject == null) continue;

						foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
						{
							TypeInfo typeInfo = GenerateTypeInfo(type, godotObject);

							if (typeInfo != null)
							{
								typeInfo.cSharpName = $"{GODOT_NAMESPACE}.{typeInfo.cSharpName}";

								knownTypes.Add(type.Name.ToLower(), typeInfo);
							}
						}
					}
				}
            }
		}

		static TypeInfo GenerateTypeInfo(INamedTypeSymbol type, INamedTypeSymbol godotObject)
		{
			if (type.Name.StartsWith("<"))
			{
				return null;
			}
			else if (type.TypeKind == TypeKind.Class)
			{
				if (!type.InheritsFrom(godotObject)) return null;

				TypeInfoGodotClass typeInfoClass = new TypeInfoGodotClass(type);

				foreach (var childType in type.GetTypeMembers())
				{
					TypeInfo childTypeInfo = GenerateTypeInfo(childType, godotObject);

					if (childTypeInfo == null) continue;

					string childBaseName = childTypeInfo.gdScriptName.Split('.')[0];

					childTypeInfo.gdScriptName = typeInfoClass.gdScriptName + "." + childTypeInfo.gdScriptName;
					childTypeInfo.cSharpName = typeInfoClass.cSharpName + "." + childTypeInfo.cSharpName;

					typeInfoClass.AddSubType(childBaseName.ToLower(), childTypeInfo);
				}

				return typeInfoClass;
			}
			else if (type.TypeKind == TypeKind.Enum || type.TypeKind == TypeKind.Struct)
			{
				INamedTypeSymbol containingSymbol = type.ContainingSymbol as INamedTypeSymbol;

				if (containingSymbol != null && containingSymbol.TypeKind == TypeKind.Class)
				{
					if (!containingSymbol.InheritsFrom(godotObject)) return null;
				}

				if (type.TypeKind == TypeKind.Struct)
				{
					return new TypeInfo(type.Name);
				}
				else
				{
					TypeInfoEnum typeInfoEnum = new TypeInfoEnum(type.Name);

					if (type.Name.EndsWith(ENUM_SUFFIX))
					{
						typeInfoEnum.gdScriptName = type.Name.Substring(0, type.Name.Length - ENUM_SUFFIX.Length);
					}

					foreach (string member in type.MemberNames)
					{
						if (member[0] == '.') continue;
						if (member == "value__") continue;

						typeInfoEnum.AddOption(member, member);
					}

					return typeInfoEnum;
				}
			}
			else
			{
				return null;
			}
		}

		public TypeInfo GetTypeInfo(string gdScriptType)
		{
			List<string> typeParts = gdScriptType.ToLower().Split('.').ToList();

			if (!knownTypes.ContainsKey(typeParts[0])) return null;

			TypeInfo typeInfo = knownTypes[typeParts[0]];
			typeParts.RemoveAt(0);

			while (typeParts.Count > 0)
			{
				if (typeInfo is TypeInfoGodotClass typeInfoClass)
				{
					typeInfo = typeInfoClass.GetSubType(typeParts[0]);

					if (typeInfo != null)
					{
						typeParts.RemoveAt(0);
					}
					else
					{
						return null;
					}
				}
				else
				{
					return null;
				}
			}

			return typeInfo;
        }
	}

	public class TypeInfoGodotClass : TypeInfo, ITypeInfoClass
	{
		public INamedTypeSymbol classSymbol;
		Dictionary<string, TypeInfo> subTypes = new Dictionary<string, TypeInfo>();

		public TypeInfoGodotClass(INamedTypeSymbol classSymbol) : base(classSymbol.Name)
		{
			this.classSymbol = classSymbol;
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
}
