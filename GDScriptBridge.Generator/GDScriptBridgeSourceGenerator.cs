using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace GDScriptBridge.Generator
{
	[Generator]

	public class GDScriptBridgeSourceGenerator : ISourceGenerator
	{
		const string GD_SCRIPT_EXTENSION = ".gd";

		public void Execute(GeneratorExecutionContext context)
		{
			//Debugger.Launch();

			var sourceBuilder = new StringBuilder(@"
using System;
namespace HelloWorldGenerated
{
    public static class HelloWorld
    {
        public static void SayHello() 
        {
            Console.WriteLine(""Hello from generated code!"");
            Console.WriteLine(""The following syntax trees existed in the compilation that created this program:"");
");

			sourceBuilder.AppendLine($@"Console.WriteLine(@"" - {context.AdditionalFiles.Length}"");");
			
			foreach (AdditionalText additionalFile in context.AdditionalFiles)
            {

				if (Path.GetExtension(additionalFile.Path) != GD_SCRIPT_EXTENSION) continue;

				GDScriptClass gdClass = new GDScriptClass(additionalFile.GetText().ToString());

				if (gdClass.isValid)
				{
					sourceBuilder.AppendLine($@"Console.WriteLine(@"" - {gdClass.className}"");");
					sourceBuilder.AppendLine($@"Console.WriteLine(@"" - {gdClass.extends}"");");
				}
			}
			
            // using the context, get a list of syntax trees in the users compilation
            var syntaxTrees = context.Compilation.SyntaxTrees;

			// add the filepath of each tree to the class we're building
			foreach (SyntaxTree tree in syntaxTrees)
			{
				sourceBuilder.AppendLine($@"Console.WriteLine(@"" - {tree.FilePath}"");");
			}

			// finish creating the source to inject
			sourceBuilder.Append(@"
        }
    }
}");

			// inject the created source into the users compilation
			string sourceContent = sourceBuilder.ToString();
			context.AddSource("HelloWorld", SourceText.From(sourceContent, Encoding.UTF8));
		}

		public void Initialize(GeneratorInitializationContext context)
		{
		}
	}
}
