using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GDScriptBridge.Types
{
	public class VarTypes : ITypeConverter
	{
		static readonly Dictionary<string, TypeInfo> VAR_TYPES = new Dictionary<string, TypeInfo>
		{
			{ "bool", new TypeInfo("bool") },
			{ "void", new TypeInfo("void") },
			{ "int", new TypeInfo("int","long") },
			{ "float", new TypeInfo("float","double") },
			{ "string", new TypeInfo("string") },
			{ "object", new TypeInfo("object","GodotObject") },

			{ "PackedByteArray", new TypeInfo("PackedByteArray","byte[]") },
			{ "PackedInt32Array", new TypeInfo("PackedInt32Array","int[]") },
			{ "PackedInt64Array", new TypeInfo("PackedInt64Array","long[]") },
			{ "PackedFloat32Array", new TypeInfo("PackedFloat32Array","float[]") },
			{ "PackedFloat64Array", new TypeInfo("PackedFloat64Array","double[]") },
			{ "PackedStringArray", new TypeInfo("PackedStringArray","string[]") },

			{ "PackedVector2Array", new TypeInfo("PackedVector2Array","Godot.Vector2[]") },
			{ "PackedVector3Array", new TypeInfo("PackedVector3Array","Godot.Vector3[]") },
			{ "PackedVector4Array", new TypeInfo("PackedVector4Array","Godot.Vector4[]") },
			{ "PackedColorArray", new TypeInfo("PackedColorArray","Godot.Color[]") },
		};

		public TypeInfo GetTypeInfo(string gdScriptType)
		{
			if (VAR_TYPES.ContainsKey(gdScriptType)) return VAR_TYPES[gdScriptType];

			return null;
		}
	}
}
