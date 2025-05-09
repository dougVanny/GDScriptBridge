using GDScriptBridge.Generator.Bridge;
using GDScriptBridge.Types;
using GDShrapt.Reader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace GDScriptBridge.Generator.Export
{
	public class ExportPropertiesLookup
	{
		readonly string[] GDBridgeSkipPropertyOverride = {"GDScriptBridge","Bundled","GDBridgeSkipPropertyOverride"};
		readonly string[] ExportGDBridge = {"GDScriptBridge","Bundled", "ExportGDBridge" };

		public class Using
		{
			public bool isStatic;
			public string[] path;
			public string alias;
		}

		public class NameSpace
		{
			public NameSpace parent;

			public string name;

			public List<Using> usings = new List<Using>();

			public void FillUsings(SyntaxList<UsingDirectiveSyntax> usingList)
			{
				foreach (UsingDirectiveSyntax usingDirective in usingList)
				{
					Using usingEntry = new Using();

					usingEntry.path = usingDirective.Name.ToString().Split('.');
					if (usingDirective.Alias != null) usingEntry.alias = usingDirective.Alias.Name.ToString();
					usingEntry.isStatic = usingDirective.StaticKeyword.Value != null;

					usings.Add(usingEntry);
				}
			}
		}

		public class UserClass
		{
			public class Property
			{
				public string name;
				public List<string> modifiers;
				public GDScriptClass type;
			}

			public ClassDeclarationSyntax classDeclaration;
			public NameSpace nameSpace;
			public bool overrideMethods;

			public List<Property> properties = new List<Property>();
		}

		public List<UserClass> classes = new List<UserClass>();

		public ExportPropertiesLookup(GeneratorExecutionContext context, GDScriptFolder gdScriptFolder)
		{
			foreach (SyntaxTree syntaxTree in context.Compilation.SyntaxTrees)
            {
				NameSpace fileNamespace = new NameSpace();

				CompilationUnitSyntax syntaxRoot = (CompilationUnitSyntax)syntaxTree.GetRoot();

				fileNamespace.FillUsings(syntaxRoot.Usings);

				foreach (UserClass foundClass in FindValidClasses(syntaxRoot.Members, fileNamespace))
				{
                    foreach (MemberDeclarationSyntax member in foundClass.classDeclaration.Members)
                    {
						if (member is PropertyDeclarationSyntax property)
						{
							if (!property.Modifiers.Any(m => m.Text.Equals("partial"))) continue;
							if (property.AccessorList.Accessors.Count != 2) continue;
							if (property.AccessorList.Accessors.Any(a => a.Body != null)) continue;
							if (!property.AccessorList.Accessors.Any(a => a.Keyword.Text.Equals("get")) || !property.AccessorList.Accessors.Any(a => a.Keyword.Text.Equals("set"))) continue;
							if (!property.AttributeLists.Any(l => l.Attributes.Any(a => IsType(a.ToString().Split('.'), ExportGDBridge, foundClass.nameSpace)))) continue;

							foreach (GDScriptClassFile gdScriptClassFile in gdScriptFolder.GetFiles())
                            {
                                foreach (GDScriptClass gdScriptClass in FindAllClasses(gdScriptClassFile.gdScriptClass))
                                {
									if (IsType(property.Type.ToString().Split('.'), gdScriptClass.fullCSharpName.Split('.'), foundClass.nameSpace))
									{
										foundClass.properties.Add(new UserClass.Property
										{
											type = gdScriptClass,
											modifiers = property.Modifiers.ToList().ConvertAll(m => m.Text),
											name = property.Identifier.ToString(),
										});
									}
                                };
                            }
						}
                    }

					if (foundClass.properties.Count == 0) continue;

					classes.Add(foundClass);
				}
            }
        }

		IEnumerable<GDScriptClass> FindAllClasses(GDScriptClass gdScriptClass)
		{
			yield return gdScriptClass;

            foreach (GDScriptClass innerClass in gdScriptClass.innerClasses)
            {
                foreach (GDScriptClass foundInnerClass in FindAllClasses(innerClass))
                {
					yield return foundInnerClass;

				}
            }
        }

		IEnumerable<UserClass> FindValidClasses(SyntaxList<MemberDeclarationSyntax> members, NameSpace nameSpace)
		{
            foreach (MemberDeclarationSyntax member in members)
            {
				if (member is FileScopedNamespaceDeclarationSyntax fileNameSpaceDeclaration)
				{
					foreach (string nameSpacePart in fileNameSpaceDeclaration.Name.ToString().Split('.'))
					{
						NameSpace newNamespace = new NameSpace();

						newNamespace.name = nameSpacePart;
						newNamespace.parent = nameSpace;

						nameSpace = newNamespace;
					}

					nameSpace.FillUsings(fileNameSpaceDeclaration.Usings);

					foreach (UserClass subClassDeclaration in FindValidClasses(fileNameSpaceDeclaration.Members, nameSpace))
					{
						yield return subClassDeclaration;
					}
				}
				else if (member is NamespaceDeclarationSyntax nameSpaceDeclaration)
				{
					foreach (string nameSpacePart in nameSpaceDeclaration.Name.ToString().Split('.'))
					{
						NameSpace newNamespace = new NameSpace();

						newNamespace.name = nameSpacePart;
						newNamespace.parent = nameSpace;

						nameSpace = newNamespace;
					}

					nameSpace.FillUsings(nameSpaceDeclaration.Usings);

					foreach (UserClass subClassDeclaration in FindValidClasses(nameSpaceDeclaration.Members, nameSpace))
					{
						yield return subClassDeclaration;
					}
				}
				else if (member is ClassDeclarationSyntax classDeclaration)
				{
					if (!classDeclaration.Modifiers.Any(m => m.Text.Equals("partial"))) continue;
					yield return new UserClass
					{
						classDeclaration = classDeclaration,
						nameSpace = nameSpace,
						overrideMethods = !classDeclaration.AttributeLists.Any(l => l.Attributes.Any(a => IsType(a.ToString().Split('.'), GDBridgeSkipPropertyOverride, nameSpace)))
					};
				}
			}
        }

		static bool IsType(string[] testingType, string[] desiredType, NameSpace nameSpace)
		{
			List<string> test = new List<string>();

			List<string> fullNameSpace = new List<string>();

			NameSpace nameSpaceIter = nameSpace;
			while (nameSpaceIter != null)
			{
				if(nameSpaceIter.name != null) fullNameSpace.Insert(0, nameSpaceIter.name);
				nameSpaceIter = nameSpaceIter.parent;
			}

			foreach (Using usingEntry in nameSpace.usings)
			{
				for (int i = fullNameSpace.Count; i >= 0; i--)
				{
					List<string> usingTest = new List<string>();
					usingTest.AddRange(fullNameSpace.GetRange(0, i));
					usingTest.AddRange(usingEntry.path);

					test.Clear();
					test.AddRange(usingTest);
					test.AddRange(testingType);

					if (usingEntry.alias != null)
					{
						if (!testingType[0].Equals(usingEntry.alias)) continue;

						test.RemoveAt(usingTest.Count);

						if (IsEquals(test, desiredType)) return true;
					}
					else
					{
						if (IsEquals(test, desiredType)) return true;
					}
				}
			}

			test.Clear();
			test.AddRange(fullNameSpace);
			test.AddRange(testingType);

			if (IsEquals(test, desiredType)) return true;

			if (nameSpace.parent != null) return IsType(testingType, desiredType, nameSpace.parent);
			
			return false;
		}

		static bool IsEquals(List<string> a, string[] b)
		{
			if (a.Count != b.Length) return false;

			for (int i = 0; i < a.Count; i++)
			{
				if (!a[i].Equals(b[i])) return false;
			}

			return true;
		}
	}
}
