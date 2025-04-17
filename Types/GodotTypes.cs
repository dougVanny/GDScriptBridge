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

		HashSet<string> knownTypes = new HashSet<string>();

		public GodotTypes(GeneratorExecutionContext context)
		{
			Dictionary<INamespaceSymbol, List<INamedTypeSymbol>> namespaceTypes = new Dictionary<INamespaceSymbol, List<INamedTypeSymbol>>(SymbolEqualityComparer.Default);

			foreach (IAssemblySymbol assemblySymbol in context.Compilation.SourceModule.ReferencedAssemblySymbols)
            {
				foreach (INamespaceSymbol namespaceSymbol in GetAllNamespaces(assemblySymbol.GlobalNamespace))
				{
					List<INamedTypeSymbol> namedTypeSymbolList = new List<INamedTypeSymbol>();
					
					foreach (INamedTypeSymbol typeMembers in namespaceSymbol.GetTypeMembers())
                    {
						namedTypeSymbolList.AddRange(AllNestedTypesAndSelf(typeMembers));
					}

					namespaceTypes.Add(namespaceSymbol, namedTypeSymbolList);
				}
            }

			foreach (INamespaceSymbol namespaceSymbol in namespaceTypes.Keys)
			{
				if (GODOT_NAMESPACE.Equals(namespaceSymbol.Name))
				{
					INamedTypeSymbol godotObject = null;

					foreach (INamedTypeSymbol namedTypeSymbol in namespaceTypes[namespaceSymbol])
					{
						if (GODOT_OBJECT_NAME.Equals(namedTypeSymbol.Name) && SymbolEqualityComparer.Default.Equals(namespaceSymbol, namedTypeSymbol.ContainingSymbol))
						{
							godotObject = namedTypeSymbol;

							break;
						}
					}

					if (godotObject == null) continue;

					foreach (INamedTypeSymbol namedTypeSymbol in namespaceTypes[namespaceSymbol])
					{
						if (namedTypeSymbol.Name.StartsWith("<"))
						{
							continue;
						}
						else if (namedTypeSymbol.TypeKind == TypeKind.Class)
						{
							INamedTypeSymbol test = namedTypeSymbol;

							while (test != null && !SymbolEqualityComparer.Default.Equals(test, godotObject))
							{
								test = test.BaseType;
							}

							if (test == null) continue;
						}
						else if (namedTypeSymbol.TypeKind == TypeKind.Enum || namedTypeSymbol.TypeKind == TypeKind.Struct)
						{
							INamedTypeSymbol containingSymbol = namedTypeSymbol.ContainingSymbol as INamedTypeSymbol;

							if (containingSymbol != null && containingSymbol.TypeKind == TypeKind.Class)
							{
								INamedTypeSymbol test = containingSymbol;

								while (test != null && !SymbolEqualityComparer.Default.Equals(test, godotObject))
								{
									test = test.BaseType;
								}

								if (test == null) continue;
							}
						}
						else
						{
							continue;
						}

						string fullName = namedTypeSymbol.Name;

						INamedTypeSymbol parentType = namedTypeSymbol.ContainingSymbol as INamedTypeSymbol;
						while (parentType != null && !SymbolEqualityComparer.Default.Equals(parentType, namedTypeSymbol))
						{
							fullName = parentType.Name + "." + fullName;
							parentType = parentType.ContainingSymbol as INamedTypeSymbol;
						}

						knownTypes.Add(fullName);
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

		public string GetConvertedType(string gdScriptType)
		{
            foreach (string type in knownTypes)
            {
				if (gdScriptType.ToLower().Equals(type.ToLower()))
				{
					return type;
				}
            }

			return null;
        }
	}
}
