using GDScriptBridge.Bundler;
using GDScriptBridge.Types;
using GDScriptBridge.Utils;
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

			GDScriptFolder gdScriptFolder = new GDScriptFolder();
			foreach (AdditionalText additionalFile in context.AdditionalFiles)
			{
				if (Path.GetExtension(additionalFile.Path) != GD_SCRIPT_EXTENSION) continue;

				SourceText sourceText = additionalFile.GetText();
				if (sourceText == null) continue;

				Uri fileUri = new Uri(Uri.UnescapeDataString(additionalFile.Path), UriKind.Absolute);

				GDScriptClassFile gdClassFile = new GDScriptClassFile(godotRootUri.MakeRelativeUri(fileUri).ToString(), sourceText.ToString());
				gdScriptFolder.AddFile(gdClassFile);
			}
			typeConverter.Add(gdScriptFolder);

			UniqueSymbolConverter uniqueFileNameConverter = new UniqueSymbolConverter();

			BaseCodeBundle codeBundle;

			codeBundle = new BaseGDBridgeBundle();
			context.AddSource(uniqueFileNameConverter.Convert(codeBundle.GetClassName()), codeBundle.GenerateSource());

			codeBundle = new GDBridgeWrapperBundle();
			context.AddSource(uniqueFileNameConverter.Convert(codeBundle.GetClassName()), codeBundle.GenerateSource());

			foreach (GDScriptClassFile file in gdScriptFolder.GetFiles())
            {
				context.AddSource(uniqueFileNameConverter.Convert(file.gdScriptClass.uniqueName), file.GenerateSource(gdScriptFolder, typeConverter));
			}
		}

		public void Initialize(GeneratorInitializationContext context)
		{
		}
	}
}
