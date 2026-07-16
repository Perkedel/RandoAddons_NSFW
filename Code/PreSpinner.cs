using Sandbox;
using System;

public sealed class PreSpinner : Component
{
	[Property, RequireComponent] public Rigidbody rigidItself { get; set; }
	[Property] public float forceAngularFor { get; set; } = 50000f;
	[Property] public bool enableSpinning { get; set; } = false;

	protected override void OnStart()
	{
		SpinNow();
	}

	protected override void OnUpdate()
	{
		if(enableSpinning)
			if(rigidItself.IsValid())
			{
				rigidItself.ApplyTorque( LocalRotation * Vector3.Up * forceAngularFor );
			}
	}

	public void SpinNow()
	{
		//Log.Info( $"Spin the T" );

		if ( !rigidItself.IsValid() )
		{
			rigidItself = GetComponent<Rigidbody>();
		}

		if ( rigidItself.IsValid() )
		{
			rigidItself.ApplyTorque( LocalRotation * Vector3.Up * forceAngularFor );
		}
	}

	public void toggleSpinning()
	{
		enableSpinning = !enableSpinning;
	}

	public void turnOnSpinning()
	{
		enableSpinning = true;
	}

	public void turnOffSpinning()
	{
		enableSpinning = false;
	}
}
