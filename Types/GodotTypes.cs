using GDShrapt.Reader;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

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
					List<INamedTypeSymbol> namedTypeSymbolList = new List<INamedTypeSymbol>();
					
					foreach (INamedTypeSymbol typeMembers in namespaceSymbol.GetTypeMembers())
                    {
						namedTypeSymbolList.AddRange(AllNestedTypesAndSelf(typeMembers));
					}

					if (GODOT_NAMESPACE.Equals(namespaceSymbol.Name))
					{
						INamedTypeSymbol godotObject = null;

						foreach (INamedTypeSymbol namedTypeSymbol in namedTypeSymbolList)
						{
							if (GODOT_OBJECT_NAME.Equals(namedTypeSymbol.Name) && SymbolEqualityComparer.Default.Equals(namespaceSymbol, namedTypeSymbol.ContainingSymbol))
							{
								godotObject = namedTypeSymbol;

								break;
							}
						}

						if (godotObject == null) continue;

						foreach (INamedTypeSymbol namedTypeSymbol in namedTypeSymbolList)
						{
							if (namedTypeSymbol.Name.StartsWith("<"))
							{
								continue;
							}
							else if (namedTypeSymbol.TypeKind == TypeKind.Class)
							{
								if (!isValidClass(namedTypeSymbol, godotObject)) continue;
							}
							else if (namedTypeSymbol.TypeKind == TypeKind.Enum || namedTypeSymbol.TypeKind == TypeKind.Struct)
							{
								INamedTypeSymbol containingSymbol = namedTypeSymbol.ContainingSymbol as INamedTypeSymbol;

								if (containingSymbol != null && containingSymbol.TypeKind == TypeKind.Class)
								{
									if (!isValidClass(containingSymbol, godotObject)) continue;
								}
							}
							else
							{
								continue;
							}

							string fullName = namedTypeSymbol.Name;

							INamedTypeSymbol parentType = namedTypeSymbol.ContainingSymbol as INamedTypeSymbol;
							while (parentType != null)
							{
								fullName = parentType.Name + "." + fullName;
								parentType = parentType.ContainingSymbol as INamedTypeSymbol;
							}

							if (namedTypeSymbol.TypeKind == TypeKind.Enum)
							{
								TypeInfoEnum typeInfoEnum = new TypeInfoEnum(fullName);

								if (fullName.EndsWith(ENUM_SUFFIX))
								{
									typeInfoEnum.gdScriptName = fullName.Substring(0, fullName.Length - ENUM_SUFFIX.Length);
								}

								foreach(string member in namedTypeSymbol.MemberNames)
								{
									if (member[0] == '.') continue;

									typeInfoEnum.options.Add(member);
								}

								knownTypes.Add(typeInfoEnum.gdScriptName, typeInfoEnum);
							}
							else
							{
								knownTypes.Add(fullName, new TypeInfo(fullName));
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

		static IEnumerable<INamedTypeSymbol> AllNestedTypesAndSelf(INamedTypeSymbol type)
		{
			yield return type;

			foreach (var typeMember in type.GetTypeMembers())
			{
				foreach (var nestedType in AllNestedTypesAndSelf(typeMember))
				{
					yield return nestedType;
				}
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
            foreach (string type in knownTypes.Keys)
            {
				if (gdScriptType.ToLower().Equals(type.ToLower()))
				{
					return knownTypes[type];
				}
            }

			return null;
        }
	}
}
