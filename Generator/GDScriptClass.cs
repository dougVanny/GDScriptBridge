using GDShrapt.Reader;
using System.Collections.Generic;
using System.Text;
using System;
using GDScriptBridge.Utils;

namespace GDScriptBridge.Generator
{
	public class GDScriptClass : CodeGenerator
	{
		public string godotScriptPath;
		public GDClassDeclaration classDeclaration;

		public string className;
		public string extends;
		
		public List<GDScriptEnum> enums = new List<GDScriptEnum>();
		public List<GDScriptField> variables = new List<GDScriptField>();
		public List<GDScriptMethod> methods = new List<GDScriptMethod>();
		public List<GDScriptSignal> signals = new List<GDScriptSignal>();

		public List<GDScriptInnerType> innerTypes = new List<GDScriptInnerType>();

		public bool isValid
		{
			get
			{
				return className != null;
			}
		}

		public GDScriptClass(string filePath, string fileContent)
		{
			godotScriptPath = "res://" + filePath.Replace('\\', '/');
			classDeclaration = new GDScriptReader().ParseFileContent(fileContent);

			if (classDeclaration.ClassName != null) className = classDeclaration.ClassName.Identifier.ToString();
			if (classDeclaration.Extends != null) extends = classDeclaration.Extends.Type.ToString();

            foreach (GDEnumDeclaration enumDeclaration in classDeclaration.Enums)
            {
				enums.Add(new GDScriptEnum(enumDeclaration));
            }

            foreach (GDVariableDeclaration variableDeclaration in classDeclaration.Variables)
            {
				if (variableDeclaration.IsConstant)
				{
					GDScriptInnerType innerType = new GDScriptInnerType(variableDeclaration);

					if (innerType.isValid)
					{
						innerTypes.Add(innerType);
					}
					else
					{
						variables.Add(innerType);
					}
				}
				else
				{
					variables.Add(new GDScriptField(variableDeclaration));
				}
			}

            foreach (GDMethodDeclaration methodDeclaration in classDeclaration.Methods)
            {
				methods.Add(new GDScriptMethod(methodDeclaration));
            }

            foreach (GDSignalDeclaration signalDeclaration in classDeclaration.Signals)
            {
				signals.Add(new GDScriptSignal(signalDeclaration));
            }
        }

		public override string Generate()
		{
			UniqueSymbolConverter symbolConverter = new UniqueSymbolConverter(ToTitleCase);

			StringBuilder sb = new StringBuilder();

			sb.Append("using Godot;");
			sb.Append("using System;");
			sb.Append("using System.Reflection;");
			sb.Append("using System.Collections.Generic;");

			sb.Append("namespace GDScriptBridge.Generated");
			using (CodeBlock.Brackets(sb))
			{
				sb.Append($"[GDScriptBridge.Bundled.ScriptPathAttribute(\"{godotScriptPath}\")]");
				sb.Append($"class {symbolConverter.Convert(className)} : GDScriptBridge.Bundled.BaseGDBridge");
				using (CodeBlock.Brackets(sb))
				{
					sb.Append($"public static {symbolConverter.Convert(className)} New()");
					using (CodeBlock.Brackets(sb))
					{
						sb.Append($"GDScript myGDScript = GD.Load<GDScript>(typeof({symbolConverter.Convert(className)}).GetCustomAttribute<GDScriptBridge.Bundled.ScriptPathAttribute>().godotPath);");
						sb.Append($"return new {symbolConverter.Convert(className)}() {{ godotObject = (GodotObject)myGDScript.New() }};");
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
						sb.Append($"public enum {symbolConverter.Convert(_enum.name)}");
						using (CodeBlock.Brackets(sb))
						{
							bool comma = false;

                            foreach (GDScriptEnumValue enumValue in _enum.values)
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

						sb.Append($"public {varType ?? "Variant"} {symbolConverter.Convert(variable.name)}");
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

								foreach (GDScriptField parameter in signal.parameters)
								{
									if (comma) angleBracketBuilder.Append(",");

									angleBracketBuilder.Append(convertType(parameter.type) ?? "Variant");

									comma = true;
								}
							}
							string angleBracket = angleBracketBuilder.ToString();

							sb.Append($"public delegate void {symbolConverter.Convert(signal.name)}EventHandler");
							using (CodeBlock.Parenthesis(sb))
							{
								bool comma = false;

								foreach (GDScriptField parameter in signal.parameters)
								{
									if (comma) sb.Append(",");

									sb.Append($"{convertType(parameter.type) ?? "Variant"} {parameter.name}");

									comma = true;
								}
							}
							sb.Append(";");

							sb.Append($"private Dictionary<{symbolConverter.Convert(signal.name)}EventHandler, Callable> {symbolConverter.Convert(signal.name)}Callables =  new Dictionary<{symbolConverter.Convert(signal.name)}EventHandler, Callable>();");

							sb.Append($"public event {symbolConverter.Convert(signal.name)}EventHandler {symbolConverter.Convert(signal.name)}");
							using (CodeBlock.Brackets(sb))
							{
								sb.Append("add");
								using (CodeBlock.Brackets(sb))
								{
									sb.Append($"if (!{symbolConverter.Convert(signal.name)}Callables.ContainsKey(value)) {symbolConverter.Convert(signal.name)}Callables.Add(value, Callable.From{angleBracket}(new Action{angleBracket}(value)));");
									sb.Append($"godotObject.Connect(\"{signal.name}\", {symbolConverter.Convert(signal.name)}Callables[value]);");
								}

								sb.Append($"remove => godotObject.Disconnect(\"{signal.name}\", {symbolConverter.Convert(signal.name)}Callables[value]);");
							}
						}
						else
						{
							sb.Append($"public event Action {symbolConverter.Convert(signal.name)}");
							using (CodeBlock.Brackets(sb))
							{
								sb.Append($"add => godotObject.Connect(\"{signal.name}\", Callable.From(value));");
								sb.Append($"remove => godotObject.Disconnect(\"{signal.name}\", Callable.From(value));");
							}
						}
					}

					foreach (GDScriptMethod method in methods)
                    {
						List<GDScriptMethodParam> parametersToInitialize = new List<GDScriptMethodParam>();

						string retType = convertType(method.returnType)??"Variant";

						sb.Append($"public {retType} {symbolConverter.Convert(method.name)}");
						using (CodeBlock.Parenthesis(sb))
						{
							bool comma = false;

                            foreach (GDScriptMethodParam parameter in method.methodParams)
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
                            foreach (GDScriptMethodParam parameter in parametersToInitialize)
                            {
								sb.Append($"if({parameter.name} == null) {parameter.name} = ");

								if (parameter.defaultValueExpression == "null")
								{
									sb.Append("default(Variant);");
								}
								else if(looksLikeEnumValue(parameter.defaultValueExpression))
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

								foreach (GDScriptMethodParam parameter in method.methodParams)
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
			}

			return sb.ToString();
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

		string ToTitleCase(string name)
		{
			string[] nameParts = name.Split('_');

			for (int i = 0; i < nameParts.Length; i++)
            {
				if (nameParts[i].Length == 0) continue;
				nameParts[i] = char.ToUpper(nameParts[i][0]) + nameParts[i].Substring(1);
			}

			string newName = string.Join("", nameParts);
			if (name[0] == '_') newName = "_" + newName;
			return newName;
		}
	}
}
