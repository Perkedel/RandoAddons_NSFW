namespace Editor.ShaderGraphExtras;
public static class UnlitShadingModel
{
	public static Dictionary<string, bool> Features => new()
	{
        { "SupportsAlbedo", true},
		{ "SupportsEmission", false },
        { "SupportsOpacity", true},
		{ "SupportsNormal", false },
		{ "SupportsRoughness", false },
		{ "SupportsMetalness", false },
		{ "SupportsAmbientOcclusion", false },
		{ "SupportsPositionOffset", true },
        { "SupportsPixelDepthOffset", true},

		{"SupportsOpaqueBlendMode", true},
		{"SupportsMaskedBlendMode", true},
		{"SupportsTranslucentBlendMode", true},
		{"SupportsDynamicBlendMode", true}
	};
	public static string Code => @"
return float4( m.Albedo, m.Opacity );";
}
