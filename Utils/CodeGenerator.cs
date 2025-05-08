using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace GDScriptBridge.Utils
{
	public abstract class CodeGenerator
	{
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
