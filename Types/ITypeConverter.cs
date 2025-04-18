namespace GDScriptBridge.Types
{
	public interface ITypeConverter
	{
		TypeInfo GetTypeInfo(string gdScriptType);
	}
}