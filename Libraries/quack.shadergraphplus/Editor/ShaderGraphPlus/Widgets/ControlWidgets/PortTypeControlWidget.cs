using Editor;
using ShaderGraphPlus.Nodes;
using static ShaderGraphPlus.ShaderGraphPlusGlobals;

namespace ShaderGraphPlus;

/// <summary>
/// 
/// </summary>
[CustomEditor( typeof( string ), NamedEditor = ControlWidgetCustomEditors.PortTypeChoiceEditor )]
sealed class PortTypeControlWidget : DropdownControlWidget<string>
{
	public PortTypeControlWidget( SerializedProperty property ) : base( property )
	{
	}

	protected override IEnumerable<object> GetDropdownValues()
	{
		return new List<object>()
		{
			"bool",
			"int",
			"float",
			"Vector2",
			"Vector3",
			"Vector4",
			"float2x2",
			"float3x3",
			"float4x4",
			"Texture2D",
			"TextureCube",
			"SamplerState",
		};
	}
}

