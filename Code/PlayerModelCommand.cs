using Sandbox;

public sealed class PlayerModelCommand : Component
{
	//[Property] public PlayerData playerInfo { get; set; }
	[Property] public PlayerController Myself { get; set; }

	protected override void OnAwake()
	{
		// if(!Myself.IsValid())
		// {
		// 	//var obtainPlayerInfo = Scene.Directory
		// 	Myself = Scene.Directory.FindByName("").First().GetComponent<PlayerController>();

		// }
	}

	protected override void OnUpdate()
	{

	}

	public void ExecuteChangeModel(string withIdentOf = "")
	{
		if(Myself.IsValid())
		{

		}
	}

	[ConCmd( "loadpm" )]
	public static void ChangePlayerModel(Connection caller, string withIdentOf = "")
	{
		Log.Info( $"Attempt Player Model {withIdentOf} into {caller.DisplayName}" );
		//var Myself = Scene.Directory.FindByName(caller.DisplayName).First().GetComponent<PlayerController>();
		//ExecuteChageModel
	}
}
