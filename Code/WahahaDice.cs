using System;
using Sandbox;

public sealed class WahahaDice : Component
{
	[Property] public Rigidbody rigidItself { get; set; }
	[Property] public float UpwardForce { get; set; } = 500f;
	[Property] public float RotationForce { get; set; } = 250f;

	protected override void OnAwake()
	{
		if ( !rigidItself.IsValid() )
		{
			rigidItself = GetComponent<Rigidbody>();
		}
	}

	protected override void OnStart()
	{

	}

	protected override void OnUpdate()
	{

	}

	public void RollTheDiceNow()
	{
		Log.Info( "Roll Dice" );
		if(rigidItself.IsValid())
		{
			var rnd = new Random();
			//rigidItself.ApplyForce( Vector3.Up * UpwardForce );
			rigidItself.ApplyImpulse( Vector3.Up * UpwardForce );
			rigidItself.ApplyTorque( new Vector3(rnd.NextSingle() * RotationForce,rnd.NextSingle() * RotationForce,rnd.NextSingle() * RotationForce) );
		}
	}
}
