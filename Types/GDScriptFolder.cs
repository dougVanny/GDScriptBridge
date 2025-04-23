using GDScriptBridge.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDScriptBridge.Types
{
	public class GDScriptFolder : ITypeConverter
	{
		Dictionary<string, GDScriptClassFile> files = new Dictionary<string, GDScriptClassFile>();
		Dictionary<string, GDScriptClassFile> classes = new Dictionary<string, GDScriptClassFile>();

		public void AddFile(GDScriptClassFile file)
		{
			files.Add(file.godotScriptPath, file);
			if(file.gdScriptClass.name != null) classes.Add(file.gdScriptClass.name, file);
		}

		public IEnumerable<GDScriptClassFile> GetFiles()
		{
			return files.Values;
		}

		public GDScriptClassFile GetClassFile(string filePath, string curPath = GDScriptClassFile.GODOT_RES_DRIVE)
		{
			if (!filePath.StartsWith(GDScriptClassFile.GODOT_RES_DRIVE))
			{
				List<string> pathParts = curPath.Split('/').ToList();
				if(pathParts.Last().Last() != '/') pathParts.RemoveAt(pathParts.Count - 1);

				foreach (string filePathPart in filePath.Split('/'))
				{
					if (filePathPart == ".")
					{
						continue;
					}
					else if (filePathPart == "..")
					{
						pathParts.RemoveAt(pathParts.Count - 1);
					}
					else
					{
						pathParts.Add(filePathPart);
					}
				}

				filePath = string.Join("/", pathParts);
			}

			GDScriptClassFile ret = null;

			if (files.TryGetValue(filePath, out ret))
			{
				return ret;
			}

			return null;
		}

		public TypeInfo GetTypeInfo(string gdScriptType)
		{
			List<string> parts = gdScriptType.Split('.').ToList();

			if (!classes.ContainsKey(parts[0])) return null;

			GDScriptClassFile classFile = classes[parts[0]];
			parts.RemoveAt(0);

			TypeInfoGDScriptClass typeInfoGDClass = classFile.gdScriptClass.GetAsTypeInfo(this);

			return typeInfoGDClass.GetSubType(string.Join(".", parts));
		}
	}
}
