using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace GDScriptBridge.Utils
{
	public static class GeneratorExecutionContextHelper
	{
		public static IEnumerable<INamespaceSymbol> GetAllNamespaces(this INamespaceSymbol root)
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

		public static bool InheritsFrom(this INamedTypeSymbol typeSymbol, INamedTypeSymbol baseTypeSymbol)
		{
			INamedTypeSymbol test = typeSymbol;

			while (test != null && !SymbolEqualityComparer.Default.Equals(test, baseTypeSymbol))
			{
				test = test.BaseType;
			}

			return test != null;
		}
	}
}
