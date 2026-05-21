using System;
using Sandbox;

public sealed class WahahaDice : Component
{
	[Property,RequireComponent] public Rigidbody rigidItself { get; set; }
	[Property] public float UpwardForce { get; set; } = 750f;
	[Property] public float RotationForce { get; set; } = 750f;

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
		Log.Info( $"Roll Dice" );
		if(rigidItself.IsValid())
		{
			var rnd = new Random();
			float scaleRoot = (float)Math.Cbrt( (WorldScale.x * WorldScale.y * WorldScale.z) * 1f );
			// float scaleRoot = WorldScale.x;
			// DONE: cube root method instead!
			// https://stackoverflow.com/a/34090411/9079640
			// rigidItself.ApplyForce( Vector3.Up * UpwardForce );
			// rigidItself.ApplyImpulse( Vector3.Up * (float)Math.Pow(UpwardForce, scaleRoot));
			// rigidItself.ApplyImpulse( Vector3.Up * (UpwardForce + scaleRoot));
			// rigidItself.ApplyImpulse( Vector3.Up * (UpwardForce * (float)Math.Exp(scaleRoot)));
			// rigidItself.ApplyImpulse( Vector3.Up * (UpwardForce * (float)Math.Pow(scaleRoot,2)));
			// rigidItself.ApplyForce( Vector3.Up * UpwardForce * (scaleRoot * 50f) );
			// rigidItself.ApplyForce( Vector3.Up * UpwardForce * (float)Math.Pow(50f,scaleRoot) );
			// rigidItself.ApplyForce( Vector3.Up * (float)Math.Pow(UpwardForce,scaleRoot) * 50f );
			// rigidItself.ApplyForce( Vector3.Up * UpwardForce * (50f * scaleRoot) );
			rigidItself.ApplyForce( Vector3.Up * UpwardForce * (50f * rigidItself.Mass) );
			rigidItself.ApplyTorque( new Vector3(rnd.NextSingle() * RotationForce,rnd.NextSingle() * RotationForce,rnd.NextSingle() * RotationForce) );
		}
	}
}
