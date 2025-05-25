using GDScriptBridge.Generator.Bridge;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDScriptBridge.Types
{
    public class OperationEvaluation
	{
		public enum Type
		{
			Reference,
			Bool,
			Long,
			Double,
			String,

			Null,
			Undefined,
		};

		public Type type = Type.Undefined;

		public string referencePath;

		public bool boolValue;
		public long longValue;
		public double doubleValue;
		public string stringValue;

		GDScriptClass contextClass;
		public OperationEvaluation(GDScriptClass contextClass)
		{
			this.contextClass = contextClass;
		}

		public class Reference
		{
			public TypeInfo type;
			public string fieldCSharpName;

			public Reference(TypeInfo type, string fieldCSharpName)
			{
				this.type = type;
				this.fieldCSharpName = fieldCSharpName;
			}

			public string AsCode()
			{
				return type.cSharpName + "." + fieldCSharpName;
			}
		}

		public Reference RetrieveReference()
		{
			if (type != Type.Reference) return null;

			TypeInfo typeInfo = contextClass.GetAsTypeInfo();

			List<string> referenceParts = referencePath.Split('.').ToList();

			if (referenceParts.Count > 1)
			{
				typeInfo = ((ITypeInfoClass)typeInfo).GetSubType(string.Join(".", referenceParts.GetRange(0, referenceParts.Count - 1)));
			}

			string referenceLeaf = referenceParts.Last();

			if (typeInfo is TypeInfoEnum typeInfoEnum)
			{
				foreach ((TypeInfoEnum.OptionInfo optionInfo, long optionValue) option in typeInfoEnum.Options)
				{
					if (option.optionInfo.name.ToLower().Equals(referenceLeaf.ToLower()))
					{
						return new Reference(typeInfoEnum, option.optionInfo.cSharpName);
					}
				}
			}

			return null;
		}

		public string AsCode(bool resolveReference = true)
		{
			if (!resolveReference && type == Type.Reference)
			{
				Reference reference = RetrieveReference();

				if (reference != null) return reference.AsCode();
			}

			ResolveReference();

			switch (type)
			{
				case Type.Bool:
					return boolValue.ToString().ToLower();
				case Type.Long:
					return longValue.ToString();
				case Type.Double:
					return doubleValue.ToString();
				case Type.String:
					return "@\"" + stringValue + "\"";
				case Type.Null:
				default:
					return ExpressionEvaluator.NULL;
			}
		}

		public void ResolveReference()
		{
			if (type != Type.Reference) return;

			Reference reference = RetrieveReference();

			if (reference.type is TypeInfoEnum typeInfoEnum)
			{
				foreach ((TypeInfoEnum.OptionInfo optionInfo, long optionValue) option in typeInfoEnum.Options)
				{
					if (option.optionInfo.cSharpName.Equals(reference.fieldCSharpName))
					{
						type = Type.Long;
						longValue = option.optionValue;

						return;
					}
				}
			}
			else
			{
				type = Type.Undefined;
			}
		}

		public void ConvertToBool()
		{
			ResolveReference();

			if (type == Type.Bool) return;

			type = Type.Undefined;
		}

		public void ConvertToLong()
		{
			ResolveReference();

			if (type == Type.Long)
			{
				return;
			}
			else if (type == Type.Bool)
			{
				longValue = boolValue ? 1 : 0;
				type = Type.Long;
			}
			else
			{
				type = Type.Undefined;
			}
		}

		public void ConvertToDouble()
		{
			ResolveReference();

			if (type == Type.Double)
			{
				return;
			}
			else if (type == Type.Long || type == Type.Bool)
			{
				ConvertToLong();

				doubleValue = longValue;
				type = Type.Double;
			}
			else
			{
				type = Type.Undefined;
			}
		}

		public void ConvertToString()
		{
			ResolveReference();

			type = Type.String;

			if (type == Type.String)
			{
				return;
			}
			else if (type == Type.Double)
			{
				stringValue = doubleValue.ToString();
			}
			else if (type == Type.Long)
			{
				stringValue = longValue.ToString();
			}
			else if (type == Type.Bool)
			{
				stringValue = boolValue.ToString();
			}
		}
	}

	public class ExpressionEvaluator
	{
		public const string NULL = "null";

		GDExpression expression;

		public ExpressionEvaluator(GDExpression expression)
		{
			this.expression = expression;
		}

		public OperationEvaluation Evaluate(GDScriptClass context)
		{
			return Evaluate(context, expression);
		}

		OperationEvaluation Evaluate(GDScriptClass context, GDExpression expression)
		{
			if (expression is GDBoolExpression boolExpression)
			{
				OperationEvaluation ret = new OperationEvaluation(context);

				ret.type = OperationEvaluation.Type.Bool;
				ret.boolValue = boolExpression.BoolKeyword.Value;

				return ret;
			}
			if (expression is GDNumberExpression numberExpression)
			{
				OperationEvaluation ret = new OperationEvaluation(context);

				if (numberExpression.Number.ResolveNumberType() == GDNumberType.Double)
				{
					ret.type = OperationEvaluation.Type.Double;
					ret.doubleValue = numberExpression.Number.ValueDouble;
				}
				else
				{
					ret.type = OperationEvaluation.Type.Long;
					ret.longValue = numberExpression.Number.ValueInt64;
				}

				return ret;
			}
			else if (expression is GDStringExpression stringExpression)
			{
				OperationEvaluation ret = new OperationEvaluation(context);

				ret.type = OperationEvaluation.Type.String;
				ret.stringValue = stringExpression.String.EscapedSequence;

				return ret;
			}
			else if (expression is GDIdentifierExpression identifierExpression)
			{
				OperationEvaluation ret = new OperationEvaluation(context);

				if (NULL.Equals(identifierExpression.Identifier.ToString()))
				{
					ret.type = OperationEvaluation.Type.Null;
				}
				else
				{
					ret.type = OperationEvaluation.Type.Reference;
					ret.referencePath = identifierExpression.Identifier.ToString();
				}

				return ret;
			}
			else if (expression is GDMemberOperatorExpression memberOperatorExpression)
			{
				OperationEvaluation ret = Evaluate(context, memberOperatorExpression.CallerExpression);

				if (ret.type != OperationEvaluation.Type.Reference)
				{
					ret.type = OperationEvaluation.Type.Undefined;
					return ret;
				}

				ret.referencePath += "." + memberOperatorExpression.Identifier.ToString();

				return ret;
			}
			else if (expression is GDBracketExpression bracketExpression)
			{
				return Evaluate(context, bracketExpression.InnerExpression);
			}
			else if (expression is GDIfExpression ifExpression)
			{
				OperationEvaluation condition = Evaluate(context, ifExpression.Condition);

				if (condition.type != OperationEvaluation.Type.Bool) return new OperationEvaluation(context);

				if (condition.boolValue)
				{
					return Evaluate(context, ifExpression.TrueExpression);
				}
				else
				{
					return Evaluate(context, ifExpression.FalseExpression);
				}
			}
			else if (expression is GDSingleOperatorExpression singleOperatorExpression)
			{
				OperationEvaluation target = Evaluate(context, singleOperatorExpression.TargetExpression);

				target.ResolveReference();

				if (target.type == OperationEvaluation.Type.Undefined) return target;

				Func<bool, bool> boolFunc = null;
				Func<long, long> longFunc = null;
				Func<double, double> doubleFunc = null;

				switch (singleOperatorExpression.OperatorType)
				{
					case GDSingleOperatorType.Negate:
						longFunc = (a) => -a;
						doubleFunc = (a) => -a;
						break;
					case GDSingleOperatorType.BitwiseNegate:
						longFunc = (a) => ~a;
						break;
					case GDSingleOperatorType.Not:
					case GDSingleOperatorType.Not2:
						boolFunc = (a) => !a;
						break;
				}

				switch (target.type)
				{
					case OperationEvaluation.Type.Bool:
						if (boolFunc != null)
						{
							target.boolValue = boolFunc(target.boolValue);
						}
						else
						{
							target.type = OperationEvaluation.Type.Undefined;
						}
						break;
					case OperationEvaluation.Type.Long:
						if (longFunc != null)
						{
							target.longValue = longFunc(target.longValue);
						}
						else
						{
							target.type = OperationEvaluation.Type.Undefined;
						}
						break;
					case OperationEvaluation.Type.Double:
						if (doubleFunc != null)
						{
							target.doubleValue = doubleFunc(target.doubleValue);
						}
						else
						{
							target.type = OperationEvaluation.Type.Undefined;
						}
						break;
					default:
						target.type = OperationEvaluation.Type.Undefined;
						break;
				}

				return target;
			}
			else if (expression is GDDualOperatorExpression dualOperatorExpression)
			{
				OperationEvaluation left = Evaluate(context, dualOperatorExpression.LeftExpression);
				OperationEvaluation right = Evaluate(context, dualOperatorExpression.RightExpression);

				left.ResolveReference();
				right.ResolveReference();

				if (left.type == OperationEvaluation.Type.Undefined) return left;
				if (right.type == OperationEvaluation.Type.Undefined) return right;

				Func<bool, bool, bool> boolFunc = null;
				Func<long, long, long> longFunc = null;
				Func<double, double, double> doubleFunc = null;
				Func<string, string, string> stringFunc = null;

				Func<string, string, bool> stringBoolFunc = null;
				Func<long, long, bool> longBoolFunc = null;
				Func<double, double, bool> doubleBoolFunc = null;

				switch (dualOperatorExpression.OperatorType)
				{
					case GDDualOperatorType.Addition:
						longFunc = (a, b) => a + b;
						doubleFunc = (a, b) => a + b;
						stringFunc = (a, b) => a + b;
						break;
					case GDDualOperatorType.Subtraction:
						longFunc = (a, b) => a - b;
						doubleFunc = (a, b) => a - b;
						break;
					case GDDualOperatorType.Multiply:
						longFunc = (a, b) => a * b;
						doubleFunc = (a, b) => a * b;
						break;
					case GDDualOperatorType.Division:
						longFunc = (a, b) => a / b;
						doubleFunc = (a, b) => a / b;
						break;
					case GDDualOperatorType.BitShiftLeft:
						longFunc = (a, b) => a << (int)b;
						break;
					case GDDualOperatorType.BitShiftRight:
						longFunc = (a, b) => a >> (int)b;
						break;
					case GDDualOperatorType.BitwiseOr:
						longFunc = (a, b) => a | b;
						break;
					case GDDualOperatorType.BitwiseAnd:
						longFunc = (a, b) => a & b;
						break;
					case GDDualOperatorType.Xor:
						longFunc = (a, b) => a ^ b;
						boolFunc = (a, b) => a ^ b;
						break;
					case GDDualOperatorType.Mod:
						longFunc = (a, b) => a % b;
						doubleFunc = (a, b) => a % b;
						break;
					case GDDualOperatorType.Power:
						longFunc = PowLong;
						doubleFunc = Math.Pow;
						break;
					case GDDualOperatorType.And:
					case GDDualOperatorType.And2:
						boolFunc = (a, b) => a && b;
						break;
					case GDDualOperatorType.Or:
					case GDDualOperatorType.Or2:
						boolFunc = (a, b) => a || b;
						break;
					case GDDualOperatorType.Equal:
						boolFunc = (a, b) => a == b;
						stringBoolFunc = (a, b) => a == b;
						longBoolFunc = (a, b) => a == b;
						doubleBoolFunc = (a, b) => a == b;
						break;
					case GDDualOperatorType.NotEqual:
						boolFunc = (a, b) => a != b;
						stringBoolFunc = (a, b) => a != b;
						longBoolFunc = (a, b) => a != b;
						doubleBoolFunc = (a, b) => a != b;
						break;
					case GDDualOperatorType.MoreThan:
						longBoolFunc = (a, b) => a > b;
						doubleBoolFunc = (a, b) => a > b;
						break;
					case GDDualOperatorType.MoreThanOrEqual:
						longBoolFunc = (a, b) => a >= b;
						doubleBoolFunc = (a, b) => a >= b;
						break;
					case GDDualOperatorType.LessThan:
						longBoolFunc = (a, b) => a < b;
						doubleBoolFunc = (a, b) => a < b;
						break;
					case GDDualOperatorType.LessThanOrEqual:
						longBoolFunc = (a, b) => a <= b;
						doubleBoolFunc = (a, b) => a <= b;
						break;
				}

				if (left.type == OperationEvaluation.Type.String || right.type == OperationEvaluation.Type.String)
				{
					left.ConvertToString();
					right.ConvertToString();
				}
				else if (left.type == OperationEvaluation.Type.Double || right.type == OperationEvaluation.Type.Double)
				{
					left.ConvertToDouble();
					right.ConvertToDouble();
				}
				else if (left.type == OperationEvaluation.Type.Long || right.type == OperationEvaluation.Type.Long)
				{
					left.ConvertToLong();
					right.ConvertToLong();
				}

				OperationEvaluation ret = new OperationEvaluation(context);

				switch (left.type)
				{
					case OperationEvaluation.Type.String:
						if (stringFunc != null)
						{
							ret.type = OperationEvaluation.Type.String;
							ret.stringValue = stringFunc.Invoke(left.stringValue, right.stringValue);
						}
						else if (stringBoolFunc != null)
						{
							ret.type = OperationEvaluation.Type.Bool;
							ret.boolValue = stringBoolFunc.Invoke(left.stringValue, right.stringValue);
						}
						else
						{
							ret.type = OperationEvaluation.Type.Undefined;
						}
						break;
					case OperationEvaluation.Type.Double:
						if (doubleFunc != null)
						{
							ret.type = OperationEvaluation.Type.Double;
							ret.doubleValue = doubleFunc.Invoke(left.doubleValue, right.doubleValue);
						}
						else if (doubleBoolFunc != null)
						{
							ret.type = OperationEvaluation.Type.Bool;
							ret.boolValue = doubleBoolFunc.Invoke(left.doubleValue, right.doubleValue);
						}
						else
						{
							ret.type = OperationEvaluation.Type.Undefined;
						}
						break;
					case OperationEvaluation.Type.Long:
						if (longFunc != null)
						{
							ret.type = OperationEvaluation.Type.Long;
							ret.longValue = longFunc.Invoke(left.longValue, right.longValue);
						}
						else if (longBoolFunc != null)
						{
							ret.type = OperationEvaluation.Type.Bool;
							ret.boolValue = longBoolFunc.Invoke(left.longValue, right.longValue);
						}
						else
						{
							ret.type = OperationEvaluation.Type.Undefined;
						}
						break;
					case OperationEvaluation.Type.Bool:
						if (boolFunc != null)
						{
							ret.type = OperationEvaluation.Type.Bool;
							ret.boolValue = boolFunc.Invoke(left.boolValue, right.boolValue);
						}
						else
						{
							ret.type = OperationEvaluation.Type.Undefined;
						}
						break;
					default:
						ret.type = OperationEvaluation.Type.Undefined;
						break;
				}

				return ret;
			}

			return new OperationEvaluation(context);
		}

		long PowLong(long a, long b)
		{
			long ret = a;

			while (b > 1)
			{
				ret *= a;
				b--;
			}
			while (b < 1)
			{
				ret /= a;
				b++;
			}

			return ret;
		}
	}
}
