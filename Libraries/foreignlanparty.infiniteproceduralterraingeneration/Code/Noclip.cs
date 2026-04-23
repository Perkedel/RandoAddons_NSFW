using Sandbox;


public sealed class Noclip : Component
{


	[Property]
	[Category( "Ref" )]
	public ModelRenderer HumanForScale { get; set; }

	[Property]
	[Category( "Ref" )]
	public CharacterController CharController { get; set; }

	[Property]
	[Category( "Ref" )]
	public GameObject Camera { get; set; }



	private bool ModelVis = true;

	private Angles Roto;



	protected override void OnUpdate()
	{




	}

	protected override void OnFixedUpdate()
	{

		float X = 0;
		float Y = 0;
		float Z = 0;

		float Speed = 800;



		if ( Input.Down( "Forward" ) ) { X = 1; }
		if ( Input.Down( "Backward" ) ) { X = -1; }
		if ( Input.Down( "Right" ) ) { Y = -1; }
		if ( Input.Down( "Left" ) ) { Y = 1; }
		if ( Input.Down( "Jump" ) ) { Z = 1; }
		if ( Input.Down( "Run" ) ) { Speed = Speed * 2; }
		if ( Input.Pressed( "use" ) ) { int Value; ModelVis = !ModelVis; if ( ModelVis ) { Value = 1; } else { Value = 0; } HumanForScale.Tint = new Vector4( 1, 1, 1, Value ); }



		Roto += Input.AnalogLook;

		GameObject.WorldRotation = Roto;


		Vector3 WishDir = new Vector3( X, Y, Z );

		WishDir = WishDir.RotateAround( new Vector3( 0, 0, 0 ), new Angles( WorldRotation.Pitch(), 0, 0 ) );

		WishDir = WishDir.Normal * Speed;


		CharController.Accelerate( WishDir );

		CharController.ApplyFriction( 5f );

		CharController.Move();





	}



}
