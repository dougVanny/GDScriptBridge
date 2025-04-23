using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

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
				foreach (INamespaceSymbol namespaceSymbol in GetAllNamespaces(assemblySymbol.GlobalNamespace))
				{
					if (GODOT_NAMESPACE.Equals(namespaceSymbol.Name))
					{
						INamedTypeSymbol godotObject = null;
						foreach (INamedTypeSymbol typeMembers in namespaceSymbol.GetTypeMembers())
						{
							if (GODOT_OBJECT_NAME.Equals(typeMembers.Name))
							{
								godotObject = typeMembers;

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

		private static IEnumerable<INamespaceSymbol> GetAllNamespaces(INamespaceSymbol root)
		{
			yield return root;

			foreach (var child in root.GetNamespaceMembers())
			{
				foreach (var next in GetAllNamespaces(child))
				{
					yield return next;
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
				if (!isValidClass(type, godotObject)) return null;

				TypeInfoClass typeInfoClass = new TypeInfoClass(type.Name);

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
					if (!isValidClass(containingSymbol, godotObject)) return null;
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

						typeInfoEnum.options.Add(member);
					}

					return typeInfoEnum;
				}
			}
			else
			{
				return null;
			}
		}

		static bool isValidClass(INamedTypeSymbol namedTypeSymbol, INamedTypeSymbol godotObject)
		{
			INamedTypeSymbol test = namedTypeSymbol;

			while (test != null && !SymbolEqualityComparer.Default.Equals(test, godotObject))
			{
				test = test.BaseType;
			}

			return test != null;
		}

		public TypeInfo GetTypeInfo(string gdScriptType)
		{
			List<string> typeParts = gdScriptType.ToLower().Split('.').ToList();

			if (!knownTypes.ContainsKey(typeParts[0])) return null;

			TypeInfo typeInfo = knownTypes[typeParts[0]];
			typeParts.RemoveAt(0);

			while (typeParts.Count > 0)
			{
				if (typeInfo is TypeInfoClass typeInfoClass)
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
}
