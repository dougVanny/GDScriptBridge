using GDScriptBridge.Types;
using GDScriptBridge.Utils;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using static GDScriptBridge.Types.TypeInfoEnum;

namespace GDScriptBridge.Generator.Bridge
{
    public class GDScriptClass : GDScriptBase
    {
        const string ASSEMBLY_UNLOADING_SIGNAL_WARNING = "Connecting to custom signals with parameters while running in the editor will cause assembly unload errors. Please avoid this whenever possible.";

        static readonly TypeInfo TYPEINFO_VARIANT = new TypeInfoVariant();

        public string extends;
        public string fullCSharpName;

        public GDScriptClassFile owner;
        public GDScriptClass parentClass;

        public List<GDScriptEnum> enums = new List<GDScriptEnum>();
        public List<GDScriptField> variables = new List<GDScriptField>();
        public List<GDScriptMethod> methods = new List<GDScriptMethod>();
        public List<GDScriptSignal> signals = new List<GDScriptSignal>();

        public List<GDScriptTypeReference> typeReferences = new List<GDScriptTypeReference>();

        public List<GDScriptClass> innerClasses = new List<GDScriptClass>();

        UniqueSymbolConverter uniqueSymbolConverter;

        public GDScriptClass(GDScriptClassFile owner, GDClassDeclaration classDeclaration) : base(classDeclaration.ClassName == null ? null : classDeclaration.ClassName.Identifier.ToString(), null)
        {
            this.owner = owner;

            if (name != null)
            {
                uniqueName = UniqueSymbolConverter.ToTitleCase(name);
            }
            else
            {
                uniqueName = UniqueSymbolConverter.ToTitleCase(owner.godotScriptPath.Split('/').Last().Split('.')[0]);
            }

            fullCSharpName = $"{owner.GetNamespace()}.{uniqueName}";

            if (classDeclaration.Extends != null) extends = classDeclaration.Extends.Type.ToString();

            ParseClass(classDeclaration.Members);
        }

        public GDScriptClass(GDScriptClassFile owner, GDScriptClass parentClass, GDInnerClassDeclaration innerClassDeclaration, UniqueSymbolConverter ownerClassUniqueSymbolConverter) : base(innerClassDeclaration.Identifier.ToString(), ownerClassUniqueSymbolConverter)
        {
            this.owner = owner;
            this.parentClass = parentClass;

            fullCSharpName = $"{parentClass.fullCSharpName}.{uniqueName}";

            if (innerClassDeclaration.BaseType != null) extends = innerClassDeclaration.BaseType.ToString();

            ParseClass(innerClassDeclaration.Members);
        }

		public enum BaseType
		{
			UNKNOWN,
			NODE,
			RESOURCE
		}

		public BaseType FindBaseType()
		{
            if (extends == null) return BaseType.UNKNOWN;

			TypeInfo extendsType = FindType(extends);

			if (extendsType is TypeInfoGodotClass godotClass)
			{
				Microsoft.CodeAnalysis.INamedTypeSymbol classSymbol = godotClass.classSymbol;

				while (classSymbol != null)
				{
					if (classSymbol.ContainingNamespace.Name.Equals("Godot") && classSymbol.Name.Equals("Node")) return BaseType.NODE;
					if (classSymbol.ContainingNamespace.Name.Equals("Godot") && classSymbol.Name.Equals("Resource")) return BaseType.RESOURCE;

					classSymbol = classSymbol.BaseType;
				}
			}
			else if (extendsType is TypeInfoGDScriptClass typeInfoGDScriptClass)
			{
				return typeInfoGDScriptClass.gdClass.FindBaseType();
			}

			return BaseType.UNKNOWN;
		}

		void ParseClass(GDClassMembersList members)
        {
            uniqueSymbolConverter = new UniqueSymbolConverter(UniqueSymbolConverter.ToTitleCase);

            foreach (GDEnumDeclaration enumDeclaration in members.OfType<GDEnumDeclaration>())
            {
                if (enumDeclaration.Identifier == null) continue;

                enums.Add(new GDScriptEnum(enumDeclaration, uniqueSymbolConverter));
            }

            foreach (GDVariableDeclaration variableDeclaration in members.OfType<GDVariableDeclaration>())
            {
                if (variableDeclaration.IsConstant)
                {
                    GDScriptTypeReference innerType = new GDScriptTypeReference(variableDeclaration, uniqueSymbolConverter);

                    if (innerType.isValid)
                    {
                        typeReferences.Add(innerType);
                    }
                    else
                    {
                        variables.Add(innerType);
                    }
                }
                else
                {
                    variables.Add(new GDScriptField(variableDeclaration, uniqueSymbolConverter));
                }
            }

            foreach (GDMethodDeclaration methodDeclaration in members.OfType<GDMethodDeclaration>())
            {
                methods.Add(new GDScriptMethod(methodDeclaration, uniqueSymbolConverter));
            }

            foreach (GDSignalDeclaration signalDeclaration in members.OfType<GDSignalDeclaration>())
            {
                signals.Add(new GDScriptSignal(signalDeclaration, uniqueSymbolConverter));
            }

            foreach (GDInnerClassDeclaration innerClassDeclaration in members.OfType<GDInnerClassDeclaration>())
            {
                innerClasses.Add(new GDScriptClass(owner, this, innerClassDeclaration, uniqueSymbolConverter));
            }
        }

        public void EvaluateExpressions()
        {
            for (int i = 0; i < enums.Count; i++)
            {
                GDScriptEnum gdEnum = enums[i];
                if (!gdEnum.GetAsTypeInfo(this).IsValidEnum)
                {
                    enums.RemoveAt(i--);
                }
            }

            foreach (GDScriptClass innerClass in innerClasses)
            {
                innerClass.EvaluateExpressions();

            }
        }

        public StringBuilder Generate(StringBuilder sb = null)
        {
            if (sb == null) sb = new StringBuilder();

            TypeInfo extendsTypeInfo = FindType(extends);

            sb.Append($"public class {uniqueName} : ");

            if (extendsTypeInfo is TypeInfoGDScriptClass)
            {
                sb.Append(extendsTypeInfo.cSharpName);
            }
            else
            {
                sb.Append("GDScriptBridge.Bundled.BaseGDBridge");
            }

            using (CodeBlock.Brackets(sb))
            {
                foreach (GDScriptClass innerClass in innerClasses)
                {
                    innerClass.Generate(sb);
                }

                sb.Append($"public {uniqueName}()");
                using (CodeBlock.Brackets(sb))
                {
                    sb.Append($"GDScript myGDScript = typeof({uniqueName}).GetCustomAttribute<GDScriptBridge.Bundled.ScriptPathAttribute>().LoadGDScript();");
                    sb.Append($"godotObject = (GodotObject)myGDScript.New();");
                }

                if (extendsTypeInfo is TypeInfoGodotClass)
                {
                    string extendProperty = uniqueSymbolConverter.Convert($"As_{extendsTypeInfo.cSharpName.Split('.').Last()}");

                    sb.Append($"public {extendsTypeInfo.cSharpName} {extendProperty}");
                    using (CodeBlock.Brackets(sb))
                    {
                        sb.Append($"get => ({extendsTypeInfo.cSharpName})godotObject;");
                    }
                }

                foreach (GDScriptEnum _enum in enums)
                {
                    sb.Append($"public enum {_enum.uniqueName}");
                    using (CodeBlock.Brackets(sb))
                    {
                        TypeInfoEnum typeInfoEnum = _enum.GetAsTypeInfo(this);

                        foreach ((OptionInfo optionInfo, long optionValue) option in StringBuilderIterable.Comma(sb, typeInfoEnum.Options))
                        {
                            sb.Append($"{option.optionInfo.cSharpName}");

                            if (option.optionInfo.value != null)
                            {
                                sb.Append($" = {option.optionValue}");
                            }
                        }
                    }
                }

                List<GDScriptField> allVariables = variables.ToList();
                allVariables.AddRange(typeReferences);

                foreach (GDScriptField variable in allVariables)
                {
                    TypeInfo typeInfo = FindType(variable.type) ?? TYPEINFO_VARIANT;

                    sb.Append($"public {typeInfo.cSharpName} {variable.uniqueName}");
                    using (CodeBlock.Brackets(sb))
                    {
                        string getter = typeInfo.CastFromVariant($"godotObject.Get(\"{variable.name}\")");
                        sb.Append($"get => {getter};");

                        if (!variable.isConst)
                        {
                            string setter = typeInfo.CastToVariant("value");
                            sb.Append($"set => godotObject.Set(\"{variable.name}\", {setter});");
                        }
                    }
                }

                foreach (GDScriptSignal signal in signals)
                {
                    if (signal.parameters.Count > 0)
                    {
                        sb.Append($"public delegate void {signal.uniqueName}EventHandler");
                        using (CodeBlock.Parenthesis(sb))
                        {
                            foreach (GDScriptSignal.Param parameter in StringBuilderIterable.Comma(sb, signal.parameters))
                            {
                                TypeInfo typeInfo = FindType(parameter.type) ?? TYPEINFO_VARIANT;

                                sb.Append($"{typeInfo.cSharpName} {parameter.name}");
                            }
                        }
                        sb.Append(";");

                        sb.Append($"private Dictionary<{signal.uniqueName}EventHandler, List<Callable>> {signal.uniqueName}Callables =  new Dictionary<{signal.uniqueName}EventHandler, List<Callable>>();");

                        sb.Append($"public event {signal.uniqueName}EventHandler {signal.uniqueName}");
                        using (CodeBlock.Brackets(sb))
                        {
                            sb.Append("add");
                            using (CodeBlock.Brackets(sb))
                            {
                                sb.Append($"\n#if TOOLS\n");
                                sb.Append($"if (Engine.IsEditorHint())");
                                sb.Append($"GD.PushWarning(\"{ASSEMBLY_UNLOADING_SIGNAL_WARNING}\");");
                                sb.Append($"\n#endif\n");

                                sb.Append($"if (!{signal.uniqueName}Callables.ContainsKey(value))");
                                using (CodeBlock.Brackets(sb))
                                {
                                    sb.Append($"{signal.uniqueName}Callables.Add(value, new List<Callable>());");
                                }

                                sb.Append("void VariantCasting");
                                using (CodeBlock.Parenthesis(sb))
                                {
                                    foreach (GDScriptSignal.Param parameter in StringBuilderIterable.Comma(sb, signal.parameters))
                                    {
                                        sb.Append($"Variant {parameter.name}");
                                    }
                                }
                                using (CodeBlock.Brackets(sb))
                                {
                                    sb.Append("value.Invoke");
                                    using (CodeBlock.Parenthesis(sb))
                                    {
                                        foreach (GDScriptSignal.Param parameter in StringBuilderIterable.Comma(sb, signal.parameters))
                                        {
                                            TypeInfo parameterType = FindType(parameter.type) ?? TYPEINFO_VARIANT;

                                            sb.Append(parameterType.CastFromVariant(parameter.name));
                                        }
                                    }
                                    sb.Append(";");
                                }

                                StringBuilder angleBracketBuilder = new StringBuilder();
                                using (CodeBlock.AngleBracket(angleBracketBuilder))
                                {
                                    foreach (GDScriptSignal.Param parameter in StringBuilderIterable.Comma(angleBracketBuilder, signal.parameters))
                                    {
                                        angleBracketBuilder.Append("Variant");
                                    }
                                }
                                string angleBracket = angleBracketBuilder.ToString();

                                sb.Append($"Callable __callable = Callable.From{angleBracket}(new Action{angleBracket}(VariantCasting));");
                                sb.Append($"{signal.uniqueName}Callables[value].Add(__callable);");

                                sb.Append($"godotObject.Connect(\"{signal.name}\", __callable);");
                            }

                            sb.Append($"remove");
                            using (CodeBlock.Brackets(sb))
                            {
                                sb.Append($"godotObject.Disconnect(\"{signal.name}\", {signal.uniqueName}Callables[value][0]);");
                                sb.Append($"{signal.uniqueName}Callables[value].RemoveAt(0);");
                            }
                        }
                    }
                    else
                    {
                        sb.Append($"public event Action {signal.uniqueName}");
                        using (CodeBlock.Brackets(sb))
                        {
                            sb.Append($"add => godotObject.Connect(\"{signal.name}\", Callable.From(value));");
                            sb.Append($"remove => godotObject.Disconnect(\"{signal.name}\", Callable.From(value));");
                        }
                    }
                }

                foreach (GDScriptMethod method in methods)
                {
                    List<GDScriptMethod.Param> parametersToInitialize = new List<GDScriptMethod.Param>();

                    Dictionary<GDScriptMethod.Param, OperationEvaluation> defaultValues = new Dictionary<GDScriptMethod.Param, OperationEvaluation>();
                    foreach (GDScriptMethod.Param parameter in method.methodParams)
                    {
                        if (parameter.defaultValue != null)
                        {
                            OperationEvaluation value = parameter.defaultValue.Evaluate(this);

                            if (value.type == OperationEvaluation.Type.Reference)
                            {
                                if (value.RetrieveReference() == null)
                                {
                                    defaultValues.Clear();
                                    break;
                                }
                            }
                            else if (value.type == OperationEvaluation.Type.Undefined)
                            {
                                defaultValues.Clear();
                                break;
                            }

                            defaultValues.Add(parameter, value);
                        }
                    }

                    TypeInfo returnTypeInfo = FindType(method.returnType) ?? TYPEINFO_VARIANT;

                    sb.Append(CodeGenerator.AsDocumentation($"Original Declaration:\n{method.stringDeclaration}"));
                    sb.Append($"public new {returnTypeInfo.cSharpName} {method.uniqueName}");
                    using (CodeBlock.Parenthesis(sb))
                    {
                        foreach (GDScriptMethod.Param parameter in StringBuilderIterable.Comma(sb, method.methodParams))
                        {
                            TypeInfo paramTypeInfo = FindType(parameter.type) ?? TYPEINFO_VARIANT;

                            if (paramTypeInfo == TYPEINFO_VARIANT && defaultValues.ContainsKey(parameter))
                            {
                                sb.Append($"Variant? {parameter.name} = null");

                                parametersToInitialize.Add(parameter);
                            }
                            else
                            {
                                sb.Append($"{paramTypeInfo.cSharpName} {parameter.name}");

                                if (defaultValues.ContainsKey(parameter))
                                {
                                    sb.Append($" = ");

                                    if (defaultValues[parameter].type == OperationEvaluation.Type.Reference)
                                    {
                                        OperationEvaluation.Reference reference = defaultValues[parameter].RetrieveReference();

                                        if (reference.type is TypeInfoEnum)
                                        {
                                            sb.Append(reference.AsCode());
                                        }
                                        else
                                        {
                                            sb.Append(defaultValues[parameter].AsCode());
                                        }
                                    }
                                    else
                                    {
                                        sb.Append(defaultValues[parameter].AsCode());
                                    }
                                }
                            }
                        }
                    }
                    using (CodeBlock.Brackets(sb))
                    {
                        foreach (GDScriptMethod.Param parameter in parametersToInitialize)
                        {
                            sb.Append($"if({parameter.name} == null) {parameter.name} = ");

                            if (defaultValues[parameter].type == OperationEvaluation.Type.Null)
                            {
                                sb.Append("default(Variant);");
                            }
                            else if (defaultValues[parameter].type == OperationEvaluation.Type.Reference)
                            {
                                OperationEvaluation.Reference reference = defaultValues[parameter].RetrieveReference();

                                if (reference.type is TypeInfoEnum)
                                {
                                    sb.Append($"Variant.CreateFrom((int){reference.AsCode()});");
                                }
                                else
                                {
                                    sb.Append($"Variant.CreateFrom({defaultValues[parameter].AsCode()});");
                                }
                            }
                            else
                            {
                                sb.Append($"Variant.CreateFrom({defaultValues[parameter].AsCode()});");
                            }
                        }

                        if (returnTypeInfo.cSharpName != "void") sb.Append("Variant ret = ");

                        sb.Append("godotObject.Call");
                        using (CodeBlock.Parenthesis(sb))
                        {
                            sb.Append($"\"{method.name}\"");

                            foreach (GDScriptMethod.Param parameter in method.methodParams)
                            {
                                TypeInfo paramTypeInfo = FindType(parameter.type) ?? TYPEINFO_VARIANT;

                                if (parametersToInitialize.Contains(parameter))
                                {
                                    sb.Append($", {parameter.name}.Value");
                                }
                                else if (paramTypeInfo is TypeInfoEnum)
                                {
                                    sb.Append($", Variant.CreateFrom((int){parameter.name})");
                                }
                                else
                                {
                                    sb.Append($", {parameter.name}");
                                }
                            }
                        }
                        sb.Append(";");

                        if (returnTypeInfo.cSharpName != "void")
                        {
                            sb.Append("return " + returnTypeInfo.CastFromVariant("ret") + ";");
                        }
                    }
                }
            }

            return sb;
        }

        public TypeInfoGDScriptClass GetAsTypeInfo()
        {
            return new TypeInfoGDScriptClass(this);
        }

        public TypeInfo FindType(string type)
        {
            if (type == null) return null;

            TypeInfo localTypeInfo = GetAsTypeInfo().GetSubType(type);
            if (localTypeInfo != null) return localTypeInfo;

            if (parentClass != null)
            {
                return parentClass.FindType(type);
            }
            else
            {
                return owner.globalContext.GetTypeInfo(type);
            }
        }
    }

    public class TypeInfoGDScriptClass : TypeInfo, ITypeInfoClass
    {
        public GDScriptClass gdClass;

        public TypeInfoGDScriptClass(GDScriptClass gdClass) : base(gdClass.name, gdClass.fullCSharpName)
        {
            this.gdClass = gdClass;

            isVariantCompatible = false;
        }

        public override string CastFromVariant(string variantSymbol)
        {
            return $"({variantSymbol}).AsGodotObject().AsGDBridge<{cSharpName}>()";
        }

        public override string CastToVariant(string symbol)
        {
            return $"Variant.From({symbol}.godotObject)";
        }

        static readonly char[] DOT = new char[] { '.' };
        public TypeInfo GetSubType(string subType)
        {
            List<string> parts = subType.Split(DOT, 2).ToList();

            if (string.IsNullOrEmpty(subType) || parts.Count == 0)
            {
                return this;
            }
            else if (parts.Count == 1)
            {
                foreach (GDScriptEnum gdScriptEnum in gdClass.enums)
                {
                    if (parts[0].Equals(gdScriptEnum.name))
                    {
                        return gdScriptEnum.GetAsTypeInfo(gdClass);
                    }
                }
            }

            foreach (GDScriptClass innerClass in gdClass.innerClasses)
            {
                if (innerClass.name.Equals(parts[0]))
                {
                    TypeInfoGDScriptClass typeInfoClass = innerClass.GetAsTypeInfo();

                    if (parts.Count == 1) return typeInfoClass;

                    return typeInfoClass.GetSubType(parts[1]);
                }
            }

            foreach (GDScriptTypeReference typeReference in gdClass.typeReferences)
            {
                if (typeReference.name.Equals(parts[0]))
                {
                    GDScriptClass context = gdClass;

                    if (!string.IsNullOrEmpty(typeReference.preload))
                    {
                        GDScriptClassFile file = gdClass.owner.folder.GetClassFile(typeReference.preload, gdClass.owner.godotScriptPath);

                        if (file == null) return null;

                        context = file.gdScriptClass;
                    }

                    TypeInfo referencedType = context.GetAsTypeInfo().GetSubType(string.Join(".", typeReference.memberPath));

                    if (parts.Count == 1)
                    {
                        return referencedType;
                    }
                    else if (referencedType is TypeInfoGDScriptClass referencedTypeClass)
                    {
                        return referencedTypeClass.GetSubType(parts[1]);
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return null;
        }
    }
}
