using GDScriptBridge.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace GDScriptBridge.Bundler
{
	public abstract class BaseCodeBundle : CodeGenerator
	{
		public abstract string GetClassName();
	}
}
