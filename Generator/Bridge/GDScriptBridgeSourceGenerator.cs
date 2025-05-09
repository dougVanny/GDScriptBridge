using GDScriptBridge.Bundler;
using GDScriptBridge.Generator.Export;
using GDScriptBridge.Types;
using GDScriptBridge.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Diagnostics;
using System.IO;

namespace GDScriptBridge.Generator.Bridge
{
    [Generator]

    public class GDScriptBridgeSourceGenerator : ISourceGenerator
    {
        const string GD_SCRIPT_EXTENSION = ".gd";
        const string BUILD_PROPERTY_GodotProjectDir = "build_property.godotprojectdir";
        const string BUILD_METADATA_SkipGenerator = "build_metadata.AdditionalFiles.GDScriptBridge_SkipGenerator";

        public void Execute(GeneratorExecutionContext context)
        {
#if DEBUG
            //Debugger.Launch();
#endif

            string godotRoot;
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BUILD_PROPERTY_GodotProjectDir, out godotRoot);
            Uri godotRootUri = new Uri(Uri.UnescapeDataString(godotRoot), UriKind.Absolute);

            TypeConverterCollection globalContext = new TypeConverterCollection();
            globalContext.Add(new VarTypes());
            globalContext.Add(new GodotTypes(context));

            GDScriptFolder gdScriptFolder = new GDScriptFolder();
            foreach (AdditionalText additionalFile in context.AdditionalFiles)
            {
                if (Path.GetExtension(additionalFile.Path) != GD_SCRIPT_EXTENSION) continue;

                context.AnalyzerConfigOptions.GetOptions(additionalFile).TryGetValue(BUILD_METADATA_SkipGenerator, out string skipGenerator);
                if (skipGenerator != null && skipGenerator.ToLower().Equals("true")) continue;

                SourceText sourceText = additionalFile.GetText();
                if (sourceText == null) continue;

                Uri fileUri = new Uri(Uri.UnescapeDataString(additionalFile.Path), UriKind.Absolute);

                GDScriptClassFile gdClassFile = new GDScriptClassFile(godotRootUri.MakeRelativeUri(fileUri).ToString(), sourceText.ToString());
                gdScriptFolder.AddFile(gdClassFile);
                gdClassFile.SetContext(gdScriptFolder, globalContext);
            }
            globalContext.Add(gdScriptFolder);

            foreach (GDScriptClassFile file in gdScriptFolder.GetFiles())
            {
                file.EvaluateExpressions();
            }

            UniqueSymbolConverter uniqueFileNameConverter = new UniqueSymbolConverter();

            BaseCodeBundle codeBundle;

            codeBundle = new BaseGDBridgeBundle();
            context.AddSource(uniqueFileNameConverter.Convert("Bundled_" + codeBundle.GetClassName()), codeBundle.GenerateSource());

            codeBundle = new GDBridgeWrapperBundle(gdScriptFolder);
            context.AddSource(uniqueFileNameConverter.Convert("Bundled_" + codeBundle.GetClassName()), codeBundle.GenerateSource());

            codeBundle = new ExportGDBridgeBundle();
            context.AddSource(uniqueFileNameConverter.Convert("Bundled_" + codeBundle.GetClassName()), codeBundle.GenerateSource());

            foreach (GDScriptClassFile file in gdScriptFolder.GetFiles())
            {
                context.AddSource(uniqueFileNameConverter.Convert("GDScriptBridge_" + file.gdScriptClass.uniqueName), file.GenerateSource());
            }

			ExportPropertiesLookup propertiesLookup = new ExportPropertiesLookup(context, gdScriptFolder);
			foreach (ExportPropertiesLookup.UserClass userClass in propertiesLookup.classes)
			{
				ExportPropertiesGenerator generator = new ExportPropertiesGenerator(userClass);

				context.AddSource(uniqueFileNameConverter.Convert("Partial_" + generator.GetClassName()), generator.GenerateSource());
			}
		}

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
