using GDScriptBridge.Bundler;
using GDScriptBridge.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace GDScriptBridge.Generator
{
	[Generator]

	public class GDScriptBridgeSourceGenerator : ISourceGenerator
	{
		const string GD_SCRIPT_EXTENSION = ".gd";

		public void Execute(GeneratorExecutionContext context)
		{
			Debugger.Launch();

			string godotRoot;
			context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.godotprojectdir", out godotRoot);
			Uri godotRootUri = new Uri(Uri.UnescapeDataString(godotRoot), UriKind.Absolute);

			TypeConverterCollection typeConverter = new TypeConverterCollection();
			typeConverter.Add(new VarTypes());
			typeConverter.Add(new GodotTypes(context));

			BaseCodeBundle codeBundle;

			codeBundle = new BaseGDBridgeBundle();
			context.AddSource(codeBundle.GetClassName(), codeBundle.GenerateSource());

			codeBundle = new GDBridgeWrapperBundle();
			context.AddSource(codeBundle.GetClassName(), codeBundle.GenerateSource());

			foreach (AdditionalText additionalFile in context.AdditionalFiles)
			{
				if (Path.GetExtension(additionalFile.Path) != GD_SCRIPT_EXTENSION) continue;

				SourceText sourceText = additionalFile.GetText();
				if (sourceText == null) continue;

				Uri fileUri = new Uri(Uri.UnescapeDataString(additionalFile.Path), UriKind.Absolute);

				GDScriptClass gdClass = new GDScriptClass(godotRootUri.MakeRelativeUri(fileUri).ToString(), sourceText.ToString());

				if (gdClass.isValid)
				{
					context.AddSource(gdClass.className + "A", gdClass.GenerateSource());
				}
			}
		}

		public void Initialize(GeneratorInitializationContext context)
		{
		}
	}
}
