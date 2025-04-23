using GDShrapt.Reader;
using System.Collections.Generic;
using System.Text;
using System;
using GDScriptBridge.Utils;
using GDScriptBridge.Types;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;

namespace GDScriptBridge.Generator
{
	public class GDScriptClassFile
	{
		public const string GODOT_RES_DRIVE = "res://";

		const string GENERATED_CLASS_NAMESPACE = "GDScriptBridge.Generated";
		const string ANONYMOUS_CLASS_NAMESPACE = "GDScriptBridge.Anonymous.Generated";

		public string godotScriptPath;
		public GDClassDeclaration classDeclaration;
		public GDScriptClass gdScriptClass;

		public List<GDScriptEnum> enums = new List<GDScriptEnum>();
		public List<GDScriptField> variables = new List<GDScriptField>();
		public List<GDScriptMethod> methods = new List<GDScriptMethod>();
		public List<GDScriptSignal> signals = new List<GDScriptSignal>();

		public List<GDScriptTypeReference> typeReferences = new List<GDScriptTypeReference>();

		public GDScriptClassFile(string filePath, string fileContent)
		{
			UniqueSymbolConverter uniqueSymbolConverter = new UniqueSymbolConverter(UniqueSymbolConverter.ToTitleCase);

			godotScriptPath = GODOT_RES_DRIVE + filePath.Replace('\\', '/');
			classDeclaration = new GDScriptReader().ParseFileContent(fileContent);
			gdScriptClass = new GDScriptClass(this, classDeclaration);

		}

		public string GetNamespace()
		{
			if (classDeclaration.ClassName != null)
			{
				return GENERATED_CLASS_NAMESPACE;
			}
			else
			{
				List<string> namespaceParts = godotScriptPath.Substring(GODOT_RES_DRIVE.Length, godotScriptPath.Length - GODOT_RES_DRIVE.Length).Split('/').ToList();
				namespaceParts.RemoveAt(namespaceParts.Count - 1);
				return $"{ANONYMOUS_CLASS_NAMESPACE}.{string.Join(".", namespaceParts.ConvertAll(UniqueSymbolConverter.ToTitleCase))}";
			}
		}

		public SourceText GenerateSource(GDScriptFolder folder, TypeConverterCollection globalTypeConverter)
		{
			return CSharpSyntaxTree.ParseText(SourceText.From(Generate(folder, globalTypeConverter), Encoding.UTF8)).GetRoot().NormalizeWhitespace().SyntaxTree.GetText();
		}

		string Generate(GDScriptFolder folder, TypeConverterCollection globalTypeConverter)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("using Godot;");
			sb.Append("using System;");
			sb.Append("using System.Reflection;");
			sb.Append("using System.Collections.Generic;");

			sb.Append($"namespace {GetNamespace()}");
			using (CodeBlock.Brackets(sb))
			{
				sb.Append($"[GDScriptBridge.Bundled.ScriptPathAttribute(\"{godotScriptPath}\")]");
				gdScriptClass.Generate(folder, globalTypeConverter, sb);
			}

			return sb.ToString();
		}
	}
}
