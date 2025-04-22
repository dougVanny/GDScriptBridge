using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Text;

namespace GDScriptBridge.Generator
{
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
		public string declaredValueExpression;

		public GDScriptEnumValue(GDEnumValueDeclaration enumValueDeclaration)
		{
			name = enumValueDeclaration.Identifier.ToString();

			if (enumValueDeclaration.Value != null) declaredValueExpression = enumValueDeclaration.Value.ToString();
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
	}

	public class GDScriptInnerType : GDScriptField
	{
		public string preload = null;
		public List<string> memberPath = null;

		public GDScriptInnerType(GDVariableDeclaration variableDeclaration) : base(variableDeclaration)
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
	}

	public class GDScriptMethodParam
	{
		public string name;
		public string type;

		public string defaultValueExpression;

		public GDScriptMethodParam(GDParameterDeclaration parameterDeclaration)
		{
			name = parameterDeclaration.Identifier.ToString();
			type = parameterDeclaration.Type == null ? null : parameterDeclaration.Type.ToString();

			if (parameterDeclaration.DefaultValue != null) defaultValueExpression = parameterDeclaration.DefaultValue.ToString();
		}
	}
}
