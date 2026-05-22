using Sandbox;

public sealed class Penfarmer : Component
{
	[Property] GameObject Bases { get; set; }
	[Property] GameObject Borders { get; set; }

	public void EnableObject(bool into = true, string whichOne = "")
	{
		Log.Info( $"(PENFARM) Set Enable of {whichOne} into {into.ToString()}" );
		switch(whichOne)
		{
			case "walls":
				if(Borders.IsValid)
				{
					Borders.Enabled = into;
				}
				break;

			case "floors":
				if ( Bases.IsValid )
				{
					Bases.Enabled = into;
				}
				break;
			default:
				break;
		}
	}

	protected override void OnUpdate()
	{

	}
}
