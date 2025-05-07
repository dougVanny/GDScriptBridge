using System;
using System.Collections;
using System.Text;

namespace GDScriptBridge.Utils
{
	public class StringBuilderIterable : IEnumerable
	{
		StringBuilder stringBuilder;
		string separator;
		IEnumerable enumerable;

		public StringBuilderIterable(StringBuilder stringBuilder, string separator, IEnumerable enumerable)
		{
			this.stringBuilder = stringBuilder;
			this.separator = separator;
			this.enumerable = enumerable;
		}

		public static StringBuilderIterable Comma(StringBuilder stringBuilder, IEnumerable enumerable)
		{
			return new StringBuilderIterable(stringBuilder, ",", enumerable);
		}

		public IEnumerator GetEnumerator()
		{
			bool separate = false;

            foreach (var item in enumerable)
            {
				if (separate) stringBuilder.Append(separator);

				yield return item;

				separate = true;
			}
        }
	}

	public class CodeBlock : IDisposable
	{
		StringBuilder sb;
		string close;

		public CodeBlock(StringBuilder sb, string open, string close)
		{
			this.sb = sb;
			this.close = close;

			sb.Append(open);
		}

		public static CodeBlock Brackets(StringBuilder sb)
		{
			return new CodeBlock(sb, "{", "}");
		}

		public static CodeBlock Parenthesis(StringBuilder sb)
		{
			return new CodeBlock(sb, "(", ")");
		}

		public static CodeBlock AngleBracket(StringBuilder sb)
		{
			return new CodeBlock(sb, "<", ">");
		}

		public void Dispose()
		{
			sb.Append(close);
		}
	}
}
