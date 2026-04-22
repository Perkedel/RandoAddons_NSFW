namespace Editor.ShaderGraphExtras;

public static class SGEToonShadingModel
{
	public static Dictionary<string, bool> Features => new()
	{
		{ "SupportsAlbedo", true},
		{ "SupportsEmission", true },
		{ "SupportsOpacity", true},
		{ "SupportsNormal", true },
		{ "SupportsRoughness", true },
		{ "SupportsMetalness", true },
		{ "SupportsAmbientOcclusion", true },
		{ "SupportsPositionOffset", true},
		{ "SupportsPixelDepthOffset", true},

		{"SupportsOpaqueBlendMode", true},
		{"SupportsMaskedBlendMode", true},
		{"SupportsTranslucentBlendMode", true},
		{"SupportsDynamicBlendMode", true}
	};

	/// <summary>
	/// HLSL include path for the shading model implementation
	/// </summary>
	public static string Include => "shaders/HLSL/ShadingModels/SMDL-toon.hlsl";

	/// <summary>
	/// Pixel shader return statement
	/// </summary>
	public static string Code => @"
	return ShadingModelToon::Shade( m );";
}
