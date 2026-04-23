using Sandbox;

public sealed class PenComponent : Component
{
	// Map Script of Minimal Pen
	// https://sbox.game/dev/doc/code/
	// Dude, coding on s&box is still fucking different wtf?!?!?!?!?!?
	// I thought it's about similar like Unity back in my college 🗿..

	// protected override void OnStart() // where tf is Start in s&box ffs!!!??
	protected override void OnStart()
	{
		Log.Info("This is Minimal Pen. Welcome to Minimal Pen.");

		// ✅, Jesus Christ on Innova! Took me the fuck while! Lucky guess!

		// I forgor what was I originally add this one for 💀
		// Oh yeah, achievement yess.
		// https://sbox.game/dev/doc/services/achievements
		Sandbox.Services.Achievements.Unlock( "entered_first" );

		// File system?!
		// https://sbox.game/dev/doc/assets/file-system
		// https://asset.party/api/Sandbox.FileSystem.Data **GONE**
		if ( !FileSystem.Data.FileExists( "hello.txt" ) )
    		FileSystem.OrganizationData.WriteAllText( "minFolder/hello.txt", "Hello, world!" );

      	var hello = FileSystem.Data.ReadAllText( "hello.txt" );
	}

	protected override void OnUpdate()
	{

	}

	// Tutorial Console Commands!

	[ConCmd( "test", ConVarFlags.Server )]
	public static void TestCmd( Connection caller , string arg)
	{
		Log.Info( "The caller is: " + caller.DisplayName  + ". Yeah " + arg);
	}

	[ConCmd("hello")]
    public static void HelloCommand( string name = "citizen" )
    {
        Log.Info( $"Hello there {name}!" );
    }
}
