/*
Credits:
- https://sbox.game/quack/shadergraphplus
- https://github.com/QuackCola/ShaderGraphPlus
*/

namespace Editor.ShaderGraph.Nodes;

[Title( "SGE - Component Mask" ), Category( "Shader Graph Extras - Universal" ), Icon( "call_split" )]
public sealed class SGEComponentMaskNode : ShaderNode
{
	[Hide]
	public override string Title
	{
		get
		{
			List<string> components = new List<string>();

			if ( R ) components.Add( "R" );
			if ( G ) components.Add( "G" );
			if ( B ) components.Add( "B" );
			if ( A ) components.Add( "A" );

			var suffix = components.Count > 0 ? $"{string.Join( " ", components )}" : "";

			return !string.IsNullOrWhiteSpace( suffix ) ? $"{DisplayInfo.For( this ).Name} ( {suffix} )" : DisplayInfo.For( this ).Name;
		}
	}

	[Input, Hide]
	public NodeInput Input { get; set; }

	public bool R { get; set; } = true;
	public bool G { get; set; } = true;
	public bool B { get; set; } = true;
	public bool A { get; set; } = true;

	[Output, Hide]
	public NodeResult.Func Output => ( GraphCompiler compiler ) =>
	{
		var output = compiler.Result( Input );

		if ( !output.IsValid )
		{
			return new NodeResult( 1, "0.0f" );
		}

		var components = string.Empty;

		switch ( output.Components )
		{
			case 1:
				if ( R ) components += "x";
				break;

			case 2:
				if ( R ) components += "x";
				if ( G ) components += "y";
				break;

			case 3:
				if ( R ) components += "x";
				if ( G ) components += "y";
				if ( B ) components += "z";
				break;

			case 4:
				if ( R ) components += "x";
				if ( G ) components += "y";
				if ( B ) components += "z";
				if ( A ) components += "w";
				break;
		}

		var outputType = components.Length;

		if ( components == string.Empty )
			return new NodeResult( 1, "0.0f" );

		return new NodeResult( outputType, $"{output}.{components}" );
	};
}