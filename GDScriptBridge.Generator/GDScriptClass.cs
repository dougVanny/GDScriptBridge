using Microsoft.CodeAnalysis.CSharp.Scripting;
using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDScriptBridge.Generator
{
	public class GDScriptClass
	{
		public GDClassDeclaration classDeclaration;

		public string className;
		public string extends;
		
		public List<GDScriptEnum> enums = new List<GDScriptEnum>();
		public List<GDScriptField> variables = new List<GDScriptField>();
		public List<GDScriptMethod> methods = new List<GDScriptMethod>();
		public List<GDScriptSignal> signals = new List<GDScriptSignal>();

		public bool isValid
		{
			get
			{
				return className != null;
			}
		}

		public GDScriptClass(string fileContent)
		{
			classDeclaration = new GDScriptReader().ParseFileContent(fileContent);

			if (classDeclaration.ClassName == null) return;

			className = classDeclaration.ClassName.Identifier.ToString();
			if (classDeclaration.Extends != null) extends = classDeclaration.Extends.Type.ToString();

            foreach (GDEnumDeclaration enumDeclaration in classDeclaration.Enums)
            {
				enums.Add(new GDScriptEnum(enumDeclaration));
            }

            foreach (GDVariableDeclaration variableDeclaration in classDeclaration.Variables)
            {
				if (!variableDeclaration.PreviousNode.ToString().StartsWith("@export")) continue;

				variables.Add(new GDScriptField(variableDeclaration));
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
	}

	public class GDScriptEnum
	{
		public string name;
		public List<GDScriptEnumValue> values = new List<GDScriptEnumValue>();

		public GDScriptEnum(GDEnumDeclaration enumDeclaration)
		{
			name = enumDeclaration.Identifier.ToString();

            foreach (GDEnumValueDeclaration enumValueDeclaration in enumDeclaration.Values)
            {
				values.Add(new GDScriptEnumValue(enumValueDeclaration));
			}
        }
	}

	public class GDScriptEnumValue
	{
		public string name;
		public int? declaredValue;

		public GDScriptEnumValue(GDEnumValueDeclaration enumValueDeclaration)
		{
			name = enumValueDeclaration.Identifier.ToString();

			if (enumValueDeclaration.Value != null)
			{
				if (CSharpScript.EvaluateAsync(enumValueDeclaration.Value.ToString()).Result is int intResult)
				{
					declaredValue = intResult;
				}
			}
		}
	}

	public class GDScriptField
	{
		public string name;
		public string type;

		public GDScriptField(GDVariableDeclaration variableDeclaration)
		{
			name = variableDeclaration.Identifier.ToString();
			type = variableDeclaration.Type == null ? null : variableDeclaration.Type.ToString();
		}

		public GDScriptField(GDParameterDeclaration parameterDeclaration)
		{
			name = parameterDeclaration.Identifier.ToString();
			type = parameterDeclaration.Type == null ? null : parameterDeclaration.Type.ToString();
		}

		public override string ToString()
		{
			string ret = name;

			if (type != null) ret += " : " + type;

			return ret;
		}
	}

	public class GDScriptSignal
	{
		public string name;
		public List<GDScriptField> parameters = new List<GDScriptField>();

		public GDScriptSignal(GDSignalDeclaration signalDeclaration)
		{
			name = signalDeclaration.Identifier.ToString();

            foreach (GDParameterDeclaration parameterDeclaration in signalDeclaration.Parameters)
            {
				parameters.Add(new GDScriptField(parameterDeclaration));
			}
        }
	}

	public class GDScriptMethod
	{
		public string name;
		public string returnType;
		public List<GDScriptMethodParam> methodParams = new List<GDScriptMethodParam>();

		public GDScriptMethod(GDMethodDeclaration methodDeclaration)
		{
			name = methodDeclaration.Identifier.ToString();
			returnType = methodDeclaration.ReturnType == null ? null : methodDeclaration.ReturnType.ToString();

			foreach (GDParameterDeclaration parameterDeclaration in methodDeclaration.Parameters)
			{
				methodParams.Add(new GDScriptMethodParam(parameterDeclaration));
			}
		}

		public override string ToString()
		{
			string ret = "func " + name;

			ret += "(" + string.Join(", ", methodParams) + ")";

			if (returnType != null) ret += " -> " + returnType;

			return ret;
		}
	}

	public class GDScriptMethodParam
	{
		public string name;
		public string type;
		
		public bool hasDefaultValue;
		public object defaultValue;

		public GDScriptMethodParam(GDParameterDeclaration parameterDeclaration)
		{
			name = parameterDeclaration.Identifier.ToString();
			type = parameterDeclaration.Type == null ? null : parameterDeclaration.Type.ToString();

			if (parameterDeclaration.DefaultValue != null)
			{
				hasDefaultValue = true;

				try
				{
					defaultValue = CSharpScript.EvaluateAsync(parameterDeclaration.DefaultValue.ToString()).Result;
				}
				catch
				{
				}
			}
		}

		public override string ToString()
		{
			string ret = name;

			if (type != null) ret += " : " + type;

			if (hasDefaultValue)
			{
				ret += " = ";

				if (defaultValue == null) ret += "null";
				else if (defaultValue is string) ret += $"\"{defaultValue}\"";
				else ret += defaultValue;
			}

			return ret;
		}
	}
}
