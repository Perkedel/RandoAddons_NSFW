namespace Editor.ShaderGraphExtras;

public static class ShaderTemplate
{
	// Cache for template types to avoid expensive reflection lookups
	private static readonly Dictionary<string, Type> _templateTypeCache = new();

	public static string LoadTemplate( string templatePath )
	{
		if ( string.IsNullOrWhiteSpace( templatePath ) )
			return SurfaceTemplate.Code;

		var templateType = FindTemplateType( templatePath );
		if ( templateType != null )
		{
			var property = templateType.GetProperty( "Code", BindingFlags.Public | BindingFlags.Static );
			if ( property != null && property.PropertyType == typeof( string ) )
			{
				var templateCode = (string)property.GetValue( null );
				if ( !string.IsNullOrWhiteSpace( templateCode ) )
				{
					return templateCode;
				}
			}
		}

		Log.Warning( $"Shader template at '{templatePath}' not found, using default template" );
		return SurfaceTemplate.Code;
	}

	/// <summary>
	/// Find a template type by searching for classes with a Code property in the Editor.ShaderGraph namespace
	/// </summary>
	private static Type FindTemplateType( string templatePath )
	{
		if ( string.IsNullOrWhiteSpace( templatePath ) )
			return null;

		// Check cache first
		if ( _templateTypeCache.TryGetValue( templatePath, out var cachedType ) )
			return cachedType;

		// Extract class name from file path as a hint
		string className = Path.GetFileNameWithoutExtension( templatePath );

		string[] searchNamespaces = [ "Editor.ShaderGraphExtras" ];

		try
		{
			foreach ( var assembly in AppDomain.CurrentDomain.GetAssemblies() )
			{
				try
				{
					// First try exact class name match in known namespaces
					foreach ( var ns in searchNamespaces )
					{
						var templateType = assembly.GetType( $"{ns}.{className}" );
						if ( templateType != null && HasCodeProperty( templateType ) )
						{
							_templateTypeCache[templatePath] = templateType;
							return templateType;
						}
					}

					// Search types in known namespaces for one whose name contains the filename
					foreach ( var type in assembly.GetTypes() )
					{
						if ( !type.IsClass || !type.IsPublic || !HasCodeProperty( type ) )
							continue;

						if ( searchNamespaces.Contains( type.Namespace ) &&
							type.Name.Contains( className, StringComparison.OrdinalIgnoreCase ) )
						{
							_templateTypeCache[templatePath] = type;
							return type;
						}
					}

					// Fallback: search all types for exact class name match
					foreach ( var type in assembly.GetTypes() )
					{
						if ( type.Name == className && HasCodeProperty( type ) )
						{
							_templateTypeCache[templatePath] = type;
							return type;
						}
					}
				}
				catch { continue; }
			}
		}
		catch { }

		// Cache null result to avoid repeated failed lookups
		_templateTypeCache[templatePath] = null;
		return null;
	}

	/// <summary>
	/// Check if a type has a static Code property that returns a string
	/// </summary>
	private static bool HasCodeProperty( Type type )
	{
		var property = type.GetProperty( "Code", BindingFlags.Public | BindingFlags.Static );
		return property != null && property.PropertyType == typeof( string );
	}

	public static Dictionary<string, bool> GetTemplateFeatures( string templatePath )
	{
		var features = new Dictionary<string, bool>();

		if ( string.IsNullOrWhiteSpace( templatePath ) )
			return features;

		var templateType = FindTemplateType( templatePath );
		if ( templateType != null )
		{
			var property = templateType.GetProperty( "Features", BindingFlags.Public | BindingFlags.Static );
			if ( property != null && property.PropertyType == typeof( Dictionary<string, bool> ) )
			{
				var templateFeatures = (Dictionary<string, bool>)property.GetValue( null );
				if ( templateFeatures != null )
				{
					return templateFeatures;
				}
			}
		}

		return features;
	}

	public static Dictionary<string, bool> GetShadingModelFeatures( string shadingModelPath )
	{
		var features = new Dictionary<string, bool>();

		if ( string.IsNullOrWhiteSpace( shadingModelPath ) )
			return features;

		var shadingModelType = FindTemplateType( shadingModelPath );
		if ( shadingModelType != null )
		{
			var property = shadingModelType.GetProperty( "Features", BindingFlags.Public | BindingFlags.Static );
			if ( property != null && property.PropertyType == typeof( Dictionary<string, bool> ) )
			{
				var shadingModelFeatures = (Dictionary<string, bool>)property.GetValue( null );
				if ( shadingModelFeatures != null )
				{
					return shadingModelFeatures;
				}
			}
		}

		return features;
	}

	/// <summary>
	/// Load a custom shading model from a class with static Code and Include properties.
	/// Code: The pixel shader return statement (e.g., "return ShadingModelToon::Shade( m );")
	/// Include: Optional HLSL include path for the shading model implementation
	/// </summary>
	public static (string Code, string Include) LoadShadingModel( string shadingModelPath )
	{
		if ( string.IsNullOrWhiteSpace( shadingModelPath ) )
			return ("return ShadingModelStandard::Shade( m );", null);

		var shadingModelType = FindTemplateType( shadingModelPath );
		if ( shadingModelType != null )
		{
			var codeProperty = shadingModelType.GetProperty( "Code", BindingFlags.Public | BindingFlags.Static );
			if ( codeProperty != null && codeProperty.PropertyType == typeof( string ) )
			{
				var shadingModelCode = (string)codeProperty.GetValue( null );
				if ( !string.IsNullOrWhiteSpace( shadingModelCode ) )
				{
					// Check for optional Include property
					string includePath = null;
					var includeProperty = shadingModelType.GetProperty( "Include", BindingFlags.Public | BindingFlags.Static );
					if ( includeProperty != null && includeProperty.PropertyType == typeof( string ) )
					{
						includePath = (string)includeProperty.GetValue( null );
					}

					return (shadingModelCode, includePath);
				}
			}
		}

		Log.Warning( $"Shading model at '{shadingModelPath}' not found, using default Lit shading model" );
		return ("return ShadingModelStandard::Shade( m );", null);
	}

	[Function( "ColorBurn_blend" )]
	public static string ColorBurn_blend => @"
float ColorBurn_blend( float a, float b )
{
    if ( a >= 1.0f ) return 1.0f;
    if ( b <= 0.0f ) return 0.0f;
    return 1.0f - saturate( ( 1.0f - a ) / b );
}

float3 ColorBurn_blend( float3 a, float3 b )
{
    return float3(
        ColorBurn_blend( a.r, b.r ),
        ColorBurn_blend( a.g, b.g ),
        ColorBurn_blend( a.b, b.b )
	);
}

float4 ColorBurn_blend( float4 a, float4 b, bool blendAlpha = false )
{
    return float4(
        ColorBurn_blend( a.rgb, b.rgb ).rgb,
        blendAlpha ? ColorBurn_blend( a.a, b.a ) : max( a.a, b.a )
    );
}
";

	[Function( "LinearBurn_blend" )]
	public static string LinearBurn_blend => @"
float LinearBurn_blend( float a, float b )
{
    return max( 0.0f, a + b - 1.0f );
}

float3 LinearBurn_blend( float3 a, float3 b )
{
    return float3(
        LinearBurn_blend( a.r, b.r ),
        LinearBurn_blend( a.g, b.g ),
        LinearBurn_blend( a.b, b.b )
	);
}

float4 LinearBurn_blend( float4 a, float4 b, bool blendAlpha = false )
{
    return float4(
        LinearBurn_blend( a.rgb, b.rgb ).rgb,
        blendAlpha ? LinearBurn_blend( a.a, b.a ) : max( a.a, b.a )
    );
}
";

	[Function( "ColorDodge_blend" )]
	public static string ColorDodge_blend => @"
float ColorDodge_blend( float a, float b )
{
    if ( a <= 0.0f ) return 0.0f;
    if ( b >= 1.0f ) return 1.0f;
    return saturate( a / ( 1.0f - b ) );
}

float3 ColorDodge_blend( float3 a, float3 b )
{
    return float3(
        ColorDodge_blend( a.r, b.r ),
        ColorDodge_blend( a.g, b.g ),
        ColorDodge_blend( a.b, b.b )
	);
}

float4 ColorDodge_blend( float4 a, float4 b, bool blendAlpha = false )
{
    return float4(
        ColorDodge_blend( a.rgb, b.rgb ).rgb,
        blendAlpha ? ColorDodge_blend( a.a, b.a ) : max( a.a, b.a )
    );
}
";

	[Function( "LinearDodge_blend" )]
	public static string LinearDodge_blend => @"
float LinearDodge_blend( float a, float b )
{
    return min( 1.0f, a + b );
}

float3 LinearDodge_blend( float3 a, float3 b )
{
    return float3(
        LinearDodge_blend( a.r, b.r ),
        LinearDodge_blend( a.g, b.g ),
        LinearDodge_blend( a.b, b.b )
	);
}

float4 LinearDodge_blend( float4 a, float4 b, bool blendAlpha = false )
{
    return float4(
        LinearDodge_blend( a.rgb, b.rgb ).rgb,
        blendAlpha ? LinearDodge_blend( a.a, b.a ) : max( a.a, b.a )
    );
}
";

	[Function( "Overlay_blend" )]
	public static string Overlay_blend => @"
float Overlay_blend( float a, float b )
{
    if ( a <= 0.5f )
        return 2.0f * a * b;
    else
        return 1.0f - 2.0f * ( 1.0f - a ) * ( 1.0f - b );
}

float3 Overlay_blend( float3 a, float3 b )
{
    return float3(
        Overlay_blend( a.r, b.r ),
        Overlay_blend( a.g, b.g ),
        Overlay_blend( a.b, b.b )
	);
}

float4 Overlay_blend( float4 a, float4 b, bool blendAlpha = false )
{
    return float4(
        Overlay_blend( a.rgb, b.rgb ).rgb,
        blendAlpha ? Overlay_blend( a.a, b.a ) : max( a.a, b.a )
    );
}
";

	[Function( "SoftLight_blend" )]
	public static string SoftLight_blend => @"
float SoftLight_blend( float a, float b )
{
    if ( b <= 0.5f )
        return 2.0f * a * b + a * a * ( 1.0f * 2.0f * b );
    else 
        return sqrt( a ) * ( 2.0f * b - 1.0f ) + 2.0f * a * (1.0f - b);
}

float3 SoftLight_blend( float3 a, float3 b )
{
    return float3(
        SoftLight_blend( a.r, b.r ),
        SoftLight_blend( a.g, b.g ),
        SoftLight_blend( a.b, b.b )
	);
}

float4 SoftLight_blend( float4 a, float4 b, bool blendAlpha = false )
{
    return float4(
        SoftLight_blend( a.rgb, b.rgb ).rgb,
        blendAlpha ? SoftLight_blend( a.a, b.a ) : max( a.a, b.a )
    );
}
";

	[Function( "HardLight_blend" )]
	public static string HardLight_blend => @"
float HardLight_blend( float a, float b )
{
    if(b <= 0.5f)
        return 2.0f * a * b;
    else
        return 1.0f - 2.0f * (1.0f - a) * (1.0f - b);
}

float3 HardLight_blend( float3 a, float3 b )
{
    return float3(
        HardLight_blend( a.r, b.r ),
        HardLight_blend( a.g, b.g ),
        HardLight_blend( a.b, b.b )
	);
}

float4 HardLight_blend( float4 a, float4 b, bool blendAlpha = false )
{
    return float4(
        HardLight_blend( a.rgb, b.rgb ).rgb,
        blendAlpha ? HardLight_blend( a.a, b.a ) : max( a.a, b.a )
    );
}
";

	[Function( "VividLight_blend" )]
	public static string VividLight_blend => @"
float VividLight_blend( float a, float b )
{
    if ( b <= 0.5f )
	{
		b *= 2.0f;
		if ( a >= 1.0f ) return 1.0f;
		if ( b <= 0.0f ) return 0.0f;
		return 1.0f - saturate( ( 1.0f - a ) / b );
	}
    else
	{
		b = 2.0f * ( b - 0.5f );
		if ( a <= 0.0f ) return 0.0f;
		if ( b >= 1.0f ) return 1.0f;
		return saturate( a / ( 1.0f - b ) );
	}
}

float3 VividLight_blend( float3 a, float3 b )
{
    return float3(
        VividLight_blend( a.r, b.r ),
        VividLight_blend( a.g, b.g ),
        VividLight_blend( a.b, b.b )
	);
}

float4 VividLight_blend( float4 a, float4 b, bool blendAlpha = false )
{
    return float4(
        VividLight_blend( a.rgb, b.rgb ).rgb,
        blendAlpha ? VividLight_blend( a.a, b.a ) : max( a.a, b.a )
    );
}
";

	[Function( "LinearLight_blend" )]
	public static string LinearLight_blend => @"
float LinearLight_blend( float a, float b )
{
    if ( b <= 0.5f )
	{
		b *= 2.0f;
		return max( 0.0f, a + b - 1.0f );
	}
    else
	{
		b = 2.0f * ( b - 0.5f );
		return min( 1.0f, a + b );
	}
}

float3 LinearLight_blend( float3 a, float3 b )
{
    return float3(
        LinearLight_blend( a.r, b.r ),
        LinearLight_blend( a.g, b.g ),
        LinearLight_blend( a.b, b.b )
	);
}

float4 LinearLight_blend( float4 a, float4 b, bool blendAlpha = false )
{
    return float4(
        LinearLight_blend( a.rgb, b.rgb ).rgb,
        blendAlpha ? LinearLight_blend( a.a, b.a ) : max( a.a, b.a )
    );
}
";

	[Function( "HardMix_blend" )]
	public static string HardMix_blend => @"
float HardMix_blend( float a, float b )
{
    if(a + b >= 1.0f) return 1.0f;
    else return 0.0f;
}

float3 HardMix_blend( float3 a, float3 b )
{
    return float3(
        HardMix_blend( a.r, b.r ),
        HardMix_blend( a.g, b.g ),
        HardMix_blend( a.b, b.b )
	);
}

float4 HardMix_blend( float4 a, float4 b, bool blendAlpha = false )
{
    return float4(
        HardMix_blend( a.rgb, b.rgb ).rgb,
        blendAlpha ? HardMix_blend( a.a, b.a ) : max( a.a, b.a )
    );
}
";

	[Function( "Divide_blend" )]
	public static string Divide_blend => @"
float Divide_blend( float a, float b )
{
    if( b > 0.0f )
        return saturate( a / b );
    else
        return 0.0f;
}

float3 Divide_blend( float3 a, float3 b )
{
    return float3(
        Divide_blend( a.r, b.r ),
        Divide_blend( a.g, b.g ),
        Divide_blend( a.b, b.b )
	);
}

float4 Divide_blend( float4 a, float4 b, bool blendAlpha = false )
{
    return float4(
        Divide_blend( a.rgb, b.rgb ).rgb,
        blendAlpha ? Divide_blend( a.a, b.a ) : max( a.a, b.a )
    );
}
";

	[Function( "RGB2HSV" )]
	public static string RGB2HSV => @"
float3 RGB2HSV( float3 c )
{
    float4 K = float4( 0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0 );
    float4 p = lerp( float4( c.bg, K.wz ), float4( c.gb, K.xy ), step( c.b, c.g ) );
    float4 q = lerp( float4( p.xyw, c.r ), float4( c.r, p.yzx ), step( p.x, c.r ) );

    float d = q.x - min( q.w, q.y );
    float e = 1.0e-10;
    return float3( abs( q.z + ( q.w - q.y ) / ( 6.0 * d + e ) ), d / ( q.x + e ), q.x );
}
";

	[Function( "HSV2RGB" )]
	public static string HSV2RGB => @"
float3 HSV2RGB( float3 c )
{
    float4 K = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
    float3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
    return c.z * lerp( K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y );
}
";

	[Function( "TexTriplanar_Color" )]
	public static string TexTriplanar_Color => @"
float4 TexTriplanar_Color( in Texture2D tTex, in SamplerState sSampler, float3 vPosition, float3 vNormal )
{
	float2 uvX = vPosition.zy;
	float2 uvY = vPosition.xz;
	float2 uvZ = vPosition.xy;

	float3 triblend = saturate(pow(abs(vNormal), 4));
	triblend /= max(dot(triblend, half3(1,1,1)), 0.0001);

	half3 axisSign = vNormal < 0 ? -1 : 1;

	uvX.x *= axisSign.x;
	uvY.x *= axisSign.y;
	uvZ.x *= -axisSign.z;

	float4 colX = Tex2DS( tTex, sSampler, uvX );
	float4 colY = Tex2DS( tTex, sSampler, uvY );
	float4 colZ = Tex2DS( tTex, sSampler, uvZ );

	return colX * triblend.x + colY * triblend.y + colZ * triblend.z;
}
";

	[Function( "TexTriplanar_Normal" )]
	public static string TexTriplanar_Normal => @"
float3 TexTriplanar_Normal( in Texture2D tTex, in SamplerState sSampler, float3 vPosition, float3 vNormal )
{
	float2 uvX = vPosition.zy;
	float2 uvY = vPosition.xz;
	float2 uvZ = vPosition.xy;

	float3 triblend = saturate( pow( abs( vNormal ), 4 ) );
	triblend /= max( dot( triblend, half3( 1, 1, 1 ) ), 0.0001 );

	half3 axisSign = vNormal < 0 ? -1 : 1;

	uvX.x *= axisSign.x;
	uvY.x *= axisSign.y;
	uvZ.x *= -axisSign.z;

	float3 tnormalX = DecodeNormal( Tex2DS( tTex, sSampler, uvX ).xyz );
	float3 tnormalY = DecodeNormal( Tex2DS( tTex, sSampler, uvY ).xyz );
	float3 tnormalZ = DecodeNormal( Tex2DS( tTex, sSampler, uvZ ).xyz );

	tnormalX.x *= axisSign.x;
	tnormalY.x *= axisSign.y;
	tnormalZ.x *= -axisSign.z;

	tnormalX = half3( tnormalX.xy + vNormal.zy, vNormal.x );
	tnormalY = half3( tnormalY.xy + vNormal.xz, vNormal.y );
	tnormalZ = half3( tnormalZ.xy + vNormal.xy, vNormal.z );

	return normalize(
		tnormalX.zyx * triblend.x +
		tnormalY.xzy * triblend.y +
		tnormalZ.xyz * triblend.z +
		vNormal
	);
}
";
	[Function( "Quaternion_FromAngles" )]
	public static string Quaternion_FromAngles => @"
float4 Quaternion_FromAngles( float3 vAngles )
{
	float4 rot = { 0.0, 0.0, 0.0, 1.0 };

	const float ANGLE_CONVERSION = 3.14159265 / 360.0;

	float pitch = vAngles.x * ANGLE_CONVERSION;
	float yaw = vAngles.y * ANGLE_CONVERSION;
	float roll = vAngles.z * ANGLE_CONVERSION;

	float sp = sin( pitch );
	float cp = cos( pitch );

	float sy = sin( yaw );
	float cy = cos( yaw );

	float sr = sin( roll );
	float cr = cos( roll );

	float srXcp = sr * cp;
	float crXsp = cr * sp;

	rot.x = srXcp * cy - crXsp * sy; // X
	rot.y = crXsp * cy + srXcp * sy; // Y

	float crXcp = cr * cp;
	float srXsp = sr * sp;

	rot.z = crXcp * sy - srXsp * cy; // Z
	rot.w = crXcp * cy + srXsp * sy; // W (real component)

	return rot;
}
";

	[Function( "Matrix_Identity" )]
	public static string Matrix_Identity => @"
float4x4 Matrix_Identity()
{
	return
	{
		1.0, 0.0, 0.0, 0.0,
		0.0, 1.0, 0.0, 0.0,
		0.0, 0.0, 1.0, 0.0,
		0.0, 0.0, 0.0, 1.0
	};
}
";

	[Function( "Matrix_FromQuaternion" )]
	public static string Matrix_FromQuaternion => @"
float4x4 Matrix_FromQuaternion( float4 qRotation )
{
	float xx = qRotation.x * qRotation.x;
	float yy = qRotation.y * qRotation.y;
	float zz = qRotation.z * qRotation.z;

	float xy = qRotation.x * qRotation.y;
	float wz = qRotation.z * qRotation.w;
	float xz = qRotation.z * qRotation.x;
	float wy = qRotation.y * qRotation.w;
	float yz = qRotation.y * qRotation.z;
	float wx = qRotation.x * qRotation.w;

	float4x4 result =
	{
		1.0, 0.0, 0.0, 0.0,
		0.0, 1.0, 0.0, 0.0,
		0.0, 0.0, 1.0, 0.0,
		0.0, 0.0, 0.0, 1.0
	};

	result._11 = 1.0 - 2.0 * (yy + zz);
	result._21 = 2.0 * (xy + wz);
	result._31 = 2.0 * (xz - wy);

	result._12 = 2.0 * (xy - wz);
	result._22 = 1.0 - 2.0 * (zz + xx);
	result._32 = 2.0 * (yz + wx);

	result._13 = 2.0 * (xz + wy);
	result._23 = 2.0 * (yz - wx);
	result._33 = 1.0 - 2.0 * (yy + xx);

	return result;
}
";

	[Function( "Matrix_FromScale" )]
	public static string Matrix_FromScale => @"
float4x4 Matrix_FromScale( float3 vScale )
{
	float4x4 result =
	{
		1.0, 0.0, 0.0, 0.0,
		0.0, 1.0, 0.0, 0.0,
		0.0, 0.0, 1.0, 0.0,
		0.0, 0.0, 0.0, 1.0
	};

	result._11 = vScale.x;
	result._22 = vScale.y;
	result._33 = vScale.z;

	return result;
}
";

	[Function( "Matrix_FromTranslation" )]
	public static string Matrix_FromTranslation => @"
float4x4 Matrix_FromTranslation( float3 vTranslation )
{
	float4x4 result =
	{
		1.0, 0.0, 0.0, 0.0,
		0.0, 1.0, 0.0, 0.0,
		0.0, 0.0, 1.0, 0.0,
		0.0, 0.0, 0.0, 1.0
	};

	result._14 = vTranslation.x;
	result._24 = vTranslation.y;
	result._34 = vTranslation.z;

	return result;
}
";

	[Function( "Vec3OsToTs" )]
	public static string Vec3OsToTs => @"
float3 Vec3OsToTs( float3 vVectorOs, float3 vNormalOs, float3 vTangentUOs, float3 vTangentVOs )
{
	float3 vVectorTs;
	vVectorTs.x = dot( vVectorOs.xyz, vTangentUOs.xyz );
	vVectorTs.y = dot( vVectorOs.xyz, vTangentVOs.xyz );
	vVectorTs.z = dot( vVectorOs.xyz, vNormalOs.xyz );
	return vVectorTs.xyz;
}
";

	public static string TextureDefinition => @"<!-- dmx encoding keyvalues2_noids 1 format vtex 1 -->
""CDmeVtex""
{{
    ""m_inputTextureArray"" ""element_array"" 
    [
        ""CDmeInputTexture""
        {{
            ""m_name"" ""string"" ""0""
            ""m_fileName"" ""string"" ""{0}""
            ""m_colorSpace"" ""string"" ""{1}""
            ""m_typeString"" ""string"" ""2D""
            ""m_imageProcessorArray"" ""element_array"" 
            [
                ""CDmeImageProcessor""
                {{
                    ""m_algorithm"" ""string"" ""{3}""
                    ""m_stringArg"" ""string"" """"
                    ""m_vFloat4Arg"" ""vector4"" ""0 0 0 0""
                }}
            ]
        }}
    ]
    ""m_outputTypeString"" ""string"" ""2D""
    ""m_outputFormat"" ""string"" ""{2}""
    ""m_textureOutputChannelArray"" ""element_array""
    [
        ""CDmeTextureOutputChannel""
        {{
            ""m_inputTextureArray"" ""string_array""
            [
                ""0""
            ]
            ""m_srcChannels"" ""string"" ""rgba""
            ""m_dstChannels"" ""string"" ""rgba""
            ""m_mipAlgorithm"" ""CDmeImageProcessor""
            {{
                ""m_algorithm"" ""string"" ""Box""
                ""m_stringArg"" ""string"" """"
                ""m_vFloat4Arg"" ""vector4"" ""0 0 0 0""
            }}
            ""m_outputColorSpace"" ""string"" ""{1}""
        }}
    ]
}}";

	[AttributeUsage( AttributeTargets.Property )]
	private class FunctionAttribute : Attribute
	{
		public string Name { get; set; }

		public FunctionAttribute( string name )
		{
			Name = name;
		}
	}

	private static Dictionary<string, string> Functions;

	public static bool TryGetFunction( string name, out string func )
	{
		return Functions.TryGetValue( name, out func );
	}

	public static bool HasFunction( string name )
	{
		return Functions.ContainsKey( name );
	}

	internal static bool RegisterFunction( string name, string code )
	{
		if ( Functions.ContainsKey( name ) )
			return false;
		Functions[name] = code;
		return true;
	}

	static ShaderTemplate()
	{
		CreateFunctions();
	}

	[EditorEvent.Hotload]
	private static void CreateFunctions()
	{
		Functions = new Dictionary<string, string>();
		var properties = typeof( ShaderTemplate ).GetProperties( BindingFlags.Public | BindingFlags.Static );

		foreach ( var property in properties )
		{
			if ( property.PropertyType == typeof( string ) )
			{
				var attr = (FunctionAttribute)Attribute.GetCustomAttribute( property, typeof( FunctionAttribute ) );
				if ( attr != null )
				{
					Functions[attr.Name] = (string)property.GetValue( null );
				}
			}
		}
	}
}
