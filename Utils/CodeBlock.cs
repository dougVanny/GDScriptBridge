using System;
using System.Text;

namespace GDScriptBridge.Utils
{
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

		public static CodeBlock Comment(StringBuilder sb)
		{
			return new CodeBlock(sb, "/* ", " */");
		}

		public void Dispose()
		{
			sb.Append(close);
		}
	}
}
