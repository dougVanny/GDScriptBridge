using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GDScriptBridge.Types
{
	public class VarTypes : ITypeConverter
	{
		static readonly Dictionary<string, string> VAR_TYPES = new Dictionary<string, string>
		{
			{ "bool", "bool" },
			{ "void", "void" },
			{ "int", "long" },
			{ "float", "double" },
			{ "string", "string" },

			{ "object", "GodotObject" },
			{ "Array", "Godot.Collections.Array" },
			{ "Dictionary", "Godot.Collections.Dictionary" },

			{ "PackedByteArray", "byte[]" },
			{ "PackedInt32Array", "int[]" },
			{ "PackedInt64Array", "long[]" },
			{ "PackedFloat32Array", "float[]" },
			{ "PackedFloat64Array", "double[]" },
			{ "PackedStringArray", "string[]" },

			{ "PackedVector2Array", "Godot.Vector2[]" },
			{ "PackedVector3Array", "Godot.Vector3[]" },
			{ "PackedVector4Array", "Godot.Vector4[]" },
			{ "PackedColorArray", "Godot.Color[]" },
		};

		public string GetConvertedType(string gdScriptType)
		{
			if (VAR_TYPES.ContainsKey(gdScriptType)) return VAR_TYPES[gdScriptType];

			return null;
		}
	}
}
