# GDScriptBridge

GDScriptBridge is a Source Generator for C# that will create bridge classes to ease interactions between C# and GDScript classes in Godot Projects.

**This package is currently in alpha. Stability is not guaranteed.**

## What is this

Consider you have the following GDScript class in your Godot project

```gdscript
class_name CustomResource
extends Resource

@export var num : int = 10;

func get_magic_number() -> float:
	return sqrt(num);
```

This library is will take that script and generate the following C# class automagically

```csharp
namespace GDScriptBridge.Generated
{
    [GDScriptBridge.Bundled.GDScriptPathAttribute(@"res://src/custom_resource.gd", "CustomResource", BaseType.RESOURCE)]
    public class CustomResource : GDScriptBridge.Bundled.BaseGDBridge
    {
        public CustomResource()
        {
            GDScript myGDScript = typeof(CustomResource).GetCustomAttribute<GDScriptBridge.Bundled.GDScriptPathAttribute>().LoadGDScript();
            godotObject = (GodotObject)myGDScript.New();
        }

        public Godot.Resource AsResource { get => (Godot.Resource)godotObject; }
        public long Num { get => (long)godotObject.Get("num"); set => godotObject.Set("num", value); }

        /// <summary>Original Declaration:<para/>
        /// func get_magic_number() -> float:</summary>
        public new double GetMagicNumber()
        {
            Variant ret = godotObject.Call("get_magic_number");
            return (double)ret;
        }
    }
}
```

And with that, in any other class of your C# project, you can do the following

```csharp
public partial class CustomNode : Node
{
  [Export] public Resource resource;

  public override void _Ready()
  {
    CustomResource customResource = resource.AsGDBridge<CustomResource>();
    double magicNumber = customResource.GetMagicNumber();
  }
}
```

GDBridge currently supports accessing variables, methods, enums and signals or custom GDScript classes

## How to install

Get the lattest version from the [Releases page](https://github.com/dougVanny/GDScriptBridge/releases)

## How to use

Once installed, add to your csproj all GDScript files you would like to create bridges for in C#

```xml
<Project Sdk="Godot.NET.Sdk/x.x">
  ...
  <ItemGroup>
    <AdditionalFiles Include="**/*.gd" /> <!-- Use to add all GDScript files -->
    <AdditionalFiles Include="custom_resource.gd" /> <!-- Or just add the specific files you want -->
  </ItemGroup>
</Project>
```

Next time you compile your code, all Bridge classes will be generated to all GDScript files supported

## Acknowledgements

This project wouldn't be possible without the [GDShrapt](https://github.com/elamaunt/GDShrapt) library made by [elamaunt](https://github.com/elamaunt)
