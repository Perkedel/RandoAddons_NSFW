using Sandbox;

[Title( "Radio Station" ), Icon( "radio" ), Description( "Defines a radio station with a name, stream URL, and source. Used by RadioManager for playback." )]
public class RadioStation
{
	[Property, Title( "Station Name" )]
	public string StationName { get; set; } = "Station";

	[Property, Title( "Stream URL" )]
	public string StreamUrl { get; set; } = "";

	[Property, Title( "Source" )]
	public string Source { get; set; } = "Stream";

	public RadioStation() { }

	public RadioStation( string name, string url, string source = "Stream" )
	{
		StationName = name;
		StreamUrl = url;
		Source = source;
	}
}
