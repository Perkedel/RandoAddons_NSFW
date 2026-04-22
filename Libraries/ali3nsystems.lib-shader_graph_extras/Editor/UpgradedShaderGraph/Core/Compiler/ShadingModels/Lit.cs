namespace Editor.ShaderGraphExtras;
public static class LitShadingModel
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
		{ "SupportsPositionOffset", true },
        { "SupportsPixelDepthOffset", true},

		{"SupportsOpaqueBlendMode", true},
		{"SupportsMaskedBlendMode", true},
		{"SupportsTranslucentBlendMode", true},
		{"SupportsDynamicBlendMode", true}
	};
	public static string Code => @"
return ShadingModelStandard::Shade( m );";
}
