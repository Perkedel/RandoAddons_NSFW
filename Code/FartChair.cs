using Sandbox;

[Icon( "wind_power" ), Group( "Game" ), Title( "Fart Chair" )]
public sealed class FartChair : Component
{
	[Property] public BaseChair ChairController { get; set; }

	protected override void OnAwake()
	{
		if ( !ChairController.IsValid() )
		{
			ChairController = GetOrAddComponent<BaseChair>();
		}
	}

	protected override void OnUpdate()
	{

	}
}
