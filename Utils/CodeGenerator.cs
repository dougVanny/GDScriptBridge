using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Text;

namespace GDScriptBridge.Utils
{
	public abstract class CodeGenerator
	{
		public static readonly HashSet<string> keywords = new HashSet<string>()
		{
			"abstract",
			"as",
			"base",
			"bool",
			"break",
			"byte",
			"case",
			"catch",
			"char",
			"checked",
			"class",
			"const",
			"continue",
			"decimal",
			"default",
			"delegate",
			"do",
			"double",
			"else",
			"enum",
			"event",
			"explicit",
			"extern",
			"false",
			"finally",
			"fixed",
			"float",
			"for",
			"foreach",
			"goto",
			"if",
			"implicit",
			"in",
			"int",
			"interface",
			"internal",
			"is",
			"lock",
			"long",
			"namespace",
			"new",
			"null",
			"object",
			"operator",
			"out",
			"override",
			"params",
			"private",
			"protected",
			"public",
			"readonly",
			"ref",
			"return",
			"sbyte",
			"sealed",
			"short",
			"sizeof",
			"stackalloc",
			"static",
			"string",
			"struct",
			"switch",
			"this",
			"throw",
			"true",
			"try",
			"typeof",
			"uint",
			"ulong",
			"unchecked",
			"unsafe",
			"ushort",
			"using",
			"virtual",
			"void",
			"volatile",
			"while"
		};

		public SourceText GenerateSource()
		{
			return CSharpSyntaxTree.ParseText(SourceText.From(Generate(), Encoding.UTF8)).GetRoot().NormalizeWhitespace().SyntaxTree.GetText();
		}

		public abstract string Generate();

		public static string AsDocumentation(string input)
		{
			return "\n/// <summary>" + string.Join("<para/>\n/// ", input.Split('\n')) + "</summary>\n";
		}
	}
}
