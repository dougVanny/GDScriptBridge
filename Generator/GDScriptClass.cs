using GDScriptBridge.Types;
using GDScriptBridge.Utils;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDScriptBridge.Generator
{
	public class GDScriptClass : GDScriptBase
	{
		public string extends;
		public string fullCSharpName;

		public GDScriptClassFile owner;

		public List<GDScriptEnum> enums = new List<GDScriptEnum>();
		public List<GDScriptField> variables = new List<GDScriptField>();
		public List<GDScriptMethod> methods = new List<GDScriptMethod>();
		public List<GDScriptSignal> signals = new List<GDScriptSignal>();

		public List<GDScriptTypeReference> typeReferences = new List<GDScriptTypeReference>();

		public List<GDScriptClass> innerClasses = new List<GDScriptClass>();

		public GDScriptClass(GDScriptClassFile owner, GDClassDeclaration classDeclaration) : base(classDeclaration.ClassName==null?null:classDeclaration.ClassName.Identifier.ToString(), null)
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

		public GDScriptClass(GDScriptClassFile owner, string cSharpPath, GDInnerClassDeclaration innerClassDeclaration, UniqueSymbolConverter ownerClassUniqueSymbolConverter) : base(innerClassDeclaration.Identifier.ToString(), ownerClassUniqueSymbolConverter)
		{
			this.owner = owner;

			fullCSharpName = $"{cSharpPath}.{uniqueName}";

			if (innerClassDeclaration.BaseType != null) extends = innerClassDeclaration.BaseType.ToString();

			ParseClass(innerClassDeclaration.Members);
		}

		void ParseClass(GDClassMembersList members)
		{
			UniqueSymbolConverter uniqueSymbolConverter = new UniqueSymbolConverter(UniqueSymbolConverter.ToTitleCase);

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
				innerClasses.Add(new GDScriptClass(owner, fullCSharpName, innerClassDeclaration, uniqueSymbolConverter));
			}
		}

		public StringBuilder Generate(StringBuilder sb = null)
		{
			if (sb == null) sb = new StringBuilder();

			sb.Append($"class {uniqueName} : GDScriptBridge.Bundled.BaseGDBridge");
			using (CodeBlock.Brackets(sb))
			{
				foreach (GDScriptClass innerClass in innerClasses)
				{
					innerClass.Generate(sb);
				}

				sb.Append($"public static {uniqueName} New()");
				using (CodeBlock.Brackets(sb))
				{
					sb.Append($"GDScript myGDScript = GD.Load<GDScript>(typeof({uniqueName}).GetCustomAttribute<GDScriptBridge.Bundled.ScriptPathAttribute>().godotPath);");
					sb.Append($"return new {uniqueName}() {{ godotObject = (GodotObject)myGDScript.New() }};");
				}

				if (extends != null)
				{
					sb.Append($"public {extends} As{extends}");
					using (CodeBlock.Brackets(sb))
					{
						sb.Append($"get => ({extends})godotObject;");
					}
				}

				foreach (GDScriptEnum _enum in enums)
				{
					sb.Append($"public enum {_enum.uniqueName}");
					using (CodeBlock.Brackets(sb))
					{
						bool comma = false;

						foreach (GDScriptEnum.Option enumValue in _enum.options)
						{
							if (comma) sb.Append(",");

							sb.Append($"{enumValue.name}");

							if (enumValue.declaredValueExpression != null)
							{
								sb.Append($" = {enumValue.declaredValueExpression}");
							}

							comma = true;
						}
					}
				}

				foreach (GDScriptField variable in variables)
				{
					string varType = convertType(variable.type);

					sb.Append($"public {varType ?? "Variant"} {variable.uniqueName}");
					using (CodeBlock.Brackets(sb))
					{
						string getCast = varType == null ? "" : $"({varType})";

						GDScriptEnum knownEnum = findKnownEnum(varType);

						if (knownEnum != null)
						{
							sb.Append($"get => {getCast}godotObject.Get(\"{variable.name}\").AsInt32();");
							sb.Append($"set => godotObject.Set(\"{variable.name}\", (int)value);");
						}
						else
						{
							sb.Append($"get => {getCast}godotObject.Get(\"{variable.name}\");");
							sb.Append($"set => godotObject.Set(\"{variable.name}\", value);");
						}
					}
				}

				foreach (GDScriptSignal signal in signals)
				{
					if (signal.parameters.Count > 0)
					{
						StringBuilder angleBracketBuilder = new StringBuilder();
						using (CodeBlock.AngleBracket(angleBracketBuilder))
						{
							bool comma = false;

							foreach (GDScriptSignal.Param parameter in signal.parameters)
							{
								if (comma) angleBracketBuilder.Append(",");

								angleBracketBuilder.Append(convertType(parameter.type) ?? "Variant");

								comma = true;
							}
						}
						string angleBracket = angleBracketBuilder.ToString();

						sb.Append($"public delegate void {signal.uniqueName}EventHandler");
						using (CodeBlock.Parenthesis(sb))
						{
							bool comma = false;

							foreach (GDScriptSignal.Param parameter in signal.parameters)
							{
								if (comma) sb.Append(",");

								sb.Append($"{convertType(parameter.type) ?? "Variant"} {parameter.name}");

								comma = true;
							}
						}
						sb.Append(";");

						sb.Append($"private Dictionary<{signal.uniqueName}EventHandler, Callable> {signal.uniqueName}Callables =  new Dictionary<{signal.uniqueName}EventHandler, Callable>();");

						sb.Append($"public event {signal.uniqueName}EventHandler {signal.uniqueName}");
						using (CodeBlock.Brackets(sb))
						{
							sb.Append("add");
							using (CodeBlock.Brackets(sb))
							{
								sb.Append($"if (!{signal.uniqueName}Callables.ContainsKey(value)) {signal.uniqueName}Callables.Add(value, Callable.From{angleBracket}(new Action{angleBracket}(value)));");
								sb.Append($"godotObject.Connect(\"{signal.name}\", {signal.uniqueName}Callables[value]);");
							}

							sb.Append($"remove => godotObject.Disconnect(\"{signal.name}\", {signal.uniqueName}Callables[value]);");
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

					string retType = convertType(method.returnType) ?? "Variant";

					sb.Append($"public {retType} {method.uniqueName}");
					using (CodeBlock.Parenthesis(sb))
					{
						bool comma = false;

						foreach (GDScriptMethod.Param parameter in method.methodParams)
						{
							if (comma) sb.Append(",");

							string paramType = convertType(parameter.type) ?? "Variant";

							if (paramType == "Variant" && parameter.defaultValueExpression != null)
							{
								sb.Append($"Variant? {parameter.name} = null");

								parametersToInitialize.Add(parameter);
							}
							else
							{
								sb.Append($"{paramType} {parameter.name}");

								if (parameter.defaultValueExpression != null)
								{
									sb.Append($" = {parameter.defaultValueExpression}");
								}
							}

							comma = true;
						}
					}
					using (CodeBlock.Brackets(sb))
					{
						foreach (GDScriptMethod.Param parameter in parametersToInitialize)
						{
							sb.Append($"if({parameter.name} == null) {parameter.name} = ");

							if (parameter.defaultValueExpression == "null")
							{
								sb.Append("default(Variant);");
							}
							else if (looksLikeEnumValue(parameter.defaultValueExpression))
							{
								sb.Append($"Variant.CreateFrom((int){parameter.defaultValueExpression});");
							}
							else
							{
								sb.Append($"Variant.CreateFrom({parameter.defaultValueExpression});");
							}
						}

						if (retType != "void") sb.Append("Variant ret = ");

						sb.Append("godotObject.Call");
						using (CodeBlock.Parenthesis(sb))
						{
							sb.Append($"\"{method.name}\"");

							foreach (GDScriptMethod.Param parameter in method.methodParams)
							{
								GDScriptEnum enumType = findKnownEnum(parameter.type);

								if (parametersToInitialize.Contains(parameter))
								{
									sb.Append($", {parameter.name}.Value");
								}
								else if (enumType != null)
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

						if (retType != "void")
						{
							sb.Append("return ");
							if (retType != "Variant") sb.Append($"({retType})");
							sb.Append(" ret;");
						}
					}
				}
			}

			return sb;
		}

		public TypeInfoGDScriptClass GetAsTypeInfo(GDScriptFolder folder)
		{
			return new TypeInfoGDScriptClass(this, folder);
		}


		string convertType(string type)
		{
			if (type == null) return null;
			if (type == "float") type = "double";

			return type;
		}

		GDScriptEnum findKnownEnum(string type)
		{
			foreach (GDScriptEnum _enum in enums)
			{
				if (_enum.name == type) return _enum;
			}

			return null;
		}

		bool looksLikeEnumValue(string expression)
		{
			return expression.IndexOf('.') > 0 && char.IsLetter(expression[0]);
		}

	}

	public class TypeInfoGDScriptClass : TypeInfo, ITypeInfoClass
	{
		GDScriptClass gdClass;
		GDScriptFolder folder;

		public TypeInfoGDScriptClass(GDScriptClass gdClass, GDScriptFolder folder) : base(gdClass.name, gdClass.fullCSharpName)
		{
			this.gdClass = gdClass;
			this.folder = folder;
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
						TypeInfoEnum typeInfoEnum = new TypeInfoEnum(gdScriptEnum.name, $"{cSharpName}.{gdScriptEnum.uniqueName}");

						foreach (GDScriptEnum.Option enumOption in gdScriptEnum.options)
						{
							typeInfoEnum.options.Add(enumOption.name);
						}

						return typeInfoEnum;
					}
				}
			}

            foreach (GDScriptClass innerClass in gdClass.innerClasses)
            {
				if (innerClass.name.Equals(parts[0]))
				{
					TypeInfoGDScriptClass typeInfoClass = innerClass.GetAsTypeInfo(folder);

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
						GDScriptClassFile file = folder.GetClassFile(typeReference.preload, gdClass.owner.godotScriptPath);

						if (file == null) return null;

						context = file.gdScriptClass;
					}

					TypeInfo referencedType = context.GetAsTypeInfo(folder).GetSubType(string.Join(".", typeReference.memberPath));

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
