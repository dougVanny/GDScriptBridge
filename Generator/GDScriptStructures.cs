using GDScriptBridge.Types;
using GDScriptBridge.Utils;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Text;

namespace GDScriptBridge.Generator
{
	public class GDScriptBase
	{
		public string name;
		public string uniqueName;

		public GDScriptBase(string name, UniqueSymbolConverter uniqueSymbolConverter)
		{
			this.name = name;

			if (uniqueSymbolConverter != null) uniqueName = uniqueSymbolConverter.Convert(name);
		}

		public GDScriptBase(string name, string uniqueName, UniqueSymbolConverter uniqueSymbolConverter)
		{
			this.name = name;
			this.uniqueName = uniqueName;

			if (uniqueSymbolConverter != null) this.uniqueName = uniqueSymbolConverter.Convert(uniqueName);
		}
	}

	public class GDScriptEnum : GDScriptBase
	{
		public class Option
		{
			public string name;

			public ExpressionEvaluator declaredValue;

			public Option(GDEnumValueDeclaration enumValueDeclaration)
			{
				name = enumValueDeclaration.Identifier.ToString();

				if (enumValueDeclaration.Value != null)
				{
					declaredValue = new ExpressionEvaluator(enumValueDeclaration.Value);
				}
			}
		}

		public List<Option> options = new List<Option>();

		public GDScriptEnum(GDEnumDeclaration enumDeclaration, UniqueSymbolConverter uniqueSymbolConverter) : base(enumDeclaration.Identifier.ToString(), enumDeclaration.Identifier.ToString()+"Enum", uniqueSymbolConverter)
		{
			foreach (GDEnumValueDeclaration enumValueDeclaration in enumDeclaration.Values)
			{
				options.Add(new Option(enumValueDeclaration));
			}
		}

		public TypeInfoEnum GetAsTypeInfo(GDScriptClass gdClass)
		{
			TypeInfoEnum typeInfoEnum = new TypeInfoEnum(name, $"{gdClass.GetAsTypeInfo().cSharpName}.{uniqueName}");
			typeInfoEnum.SetUniqueOptionSymbolConverter(new UniqueSymbolConverter(UniqueSymbolConverter.ToTitleCase));

			foreach (Option enumOption in options)
			{
				typeInfoEnum.AddOption(enumOption.name);
				
				if (enumOption.declaredValue != null)
				{
					typeInfoEnum.SetOptionValue(enumOption.name, enumOption.declaredValue.Evaluate(gdClass));
				}
			}

			return typeInfoEnum;
		}
	}

	public class GDScriptField : GDScriptBase
	{
		public string type;
		public bool isConst;

		public GDScriptField(GDVariableDeclaration variableDeclaration, UniqueSymbolConverter uniqueSymbolConverter) : base(variableDeclaration.Identifier.ToString(), uniqueSymbolConverter)
		{
			type = variableDeclaration.Type == null ? null : variableDeclaration.Type.ToString();
			isConst = variableDeclaration.IsConstant;
		}
	}

	public class GDScriptTypeReference : GDScriptField
	{
		public string preload = null;
		public List<string> memberPath = null;

		public GDScriptTypeReference(GDVariableDeclaration variableDeclaration, UniqueSymbolConverter uniqueSymbolConverter) : base(variableDeclaration, uniqueSymbolConverter)
		{
			GDExpression expression = variableDeclaration.Initializer;

			if (expression is GDIdentifierExpression identifierExpression)
			{
				memberPath = new List<string> { identifierExpression.Identifier.ToString() };
			}
			else
			{
				List<string> _memberPath = new List<string>();

				while (expression is GDMemberOperatorExpression memberOperatorExpression)
				{
					_memberPath.Insert(0, memberOperatorExpression.Identifier.ToString());
					expression = memberOperatorExpression.CallerExpression;
				}

				if (expression is GDIdentifierExpression memberIdentifierExpression)
				{
					_memberPath.Insert(0, memberIdentifierExpression.Identifier.ToString());
					memberPath = _memberPath;
				}
				else if (expression is GDCallExpression callExpression)
				{
					if (callExpression.CallerExpression is GDIdentifierExpression callerIdentifierExpression)
					{
						if (nameof(preload).Equals(callerIdentifierExpression.Identifier.ToString()))
						{
							preload = ((GDStringExpression)callExpression.Parameters[0]).String.EscapedSequence;
							memberPath = _memberPath;
						}
					}
				}
			}
		}

		public bool isValid
		{
			get
			{
				return memberPath != null;
			}
		}
	}

	public class GDScriptSignal : GDScriptBase
	{
		public class Param
		{
			public string name;
			public string type;

			public Param(GDParameterDeclaration parameterDeclaration)
			{
				name = parameterDeclaration.Identifier.ToString();
				type = parameterDeclaration.Type == null ? null : parameterDeclaration.Type.ToString();
			}
		}

		public List<Param> parameters = new List<Param>();

		public GDScriptSignal(GDSignalDeclaration signalDeclaration, UniqueSymbolConverter uniqueSymbolConverter) : base(signalDeclaration.Identifier.ToString(), uniqueSymbolConverter)
		{
			foreach (GDParameterDeclaration parameterDeclaration in signalDeclaration.Parameters)
			{
				parameters.Add(new Param(parameterDeclaration));
			}
		}
	}

	public class GDScriptMethod : GDScriptBase
	{
		public class Param
		{
			public string name;
			public string type;

			public ExpressionEvaluator defaultValue;

			public Param(GDParameterDeclaration parameterDeclaration)
			{
				name = parameterDeclaration.Identifier.ToString();
				type = parameterDeclaration.Type == null ? null : parameterDeclaration.Type.ToString();

				if (parameterDeclaration.DefaultValue != null)
				{
					defaultValue = new ExpressionEvaluator(parameterDeclaration.DefaultValue);
				}
			}
		}

		public string returnType;
		public List<Param> methodParams = new List<Param>();

		public GDScriptMethod(GDMethodDeclaration methodDeclaration, UniqueSymbolConverter uniqueSymbolConverter) : base(methodDeclaration.Identifier.ToString(), uniqueSymbolConverter)
		{
			returnType = methodDeclaration.ReturnType == null ? null : methodDeclaration.ReturnType.ToString();

			foreach (GDParameterDeclaration parameterDeclaration in methodDeclaration.Parameters)
			{
				methodParams.Add(new Param(parameterDeclaration));
			}
		}
	}
}
