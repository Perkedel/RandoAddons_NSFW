using System.Threading.Tasks;
using Sandbox;
using System.IO;
using Sandbox.Utility;

public sealed class LocalFile : Component
{



	public void Save( string FileName, Curve HeightCurve, int Noise1Type, int Noise1Seed, Curve Noise1Weight, float Noise1Frequency, int Noise2Type, int Noise2Seed, Curve Noise2Weight, float Noise2Frequency, int Noise3Type, int Noise3Seed, Curve Noise3Weight, float Noise3Frequency, int FalloffValue, int Resolution, Vector2 TerrainOffset, Vector2 FalloffCenter, int SampleRes, int Noise1Octave, int Noise2Octave, int Noise3Octave, float Noise1gain, float Noise2gain, float Noise3gain, float Noise1Lac, float Noise2Lac, float Noise3Lac)
	{
		//Makes filename string
		string FloatsName = FileName + " Floats" + ".json";
		string CurvesName = FileName + " Curves" + ".json";

		//Writes data to json
		FileSystem.Data.WriteJson( FloatsName, new float[25] { Noise1Type, Noise1Seed, Noise1Frequency, Noise2Type, Noise2Seed, Noise2Frequency, Noise3Type, Noise3Seed, Noise3Frequency, FalloffValue, Resolution, TerrainOffset.x, TerrainOffset.y, FalloffCenter.x, FalloffCenter.y, SampleRes, Noise1Octave, Noise2Octave, Noise3Octave, Noise1gain, Noise2gain, Noise3gain, Noise1Lac, Noise2Lac, Noise3Lac } );
		FileSystem.Data.WriteJson( CurvesName, new Curve[4] { HeightCurve, Noise1Weight, Noise2Weight, Noise3Weight } );

		

		Log.Info( "Saved " + FileName.ToString() + " as " + FloatsName.ToString() + " and " + CurvesName.ToString());
		Log.Info( "Saved at: " + FileSystem.Data.GetFullPath( FloatsName ).ToString() );

	}

	public void Load( string FileName, out float[] LoadedFloatValues, out Curve[] LoadedCurveValues )
	{
		//Makes filename string
		string FloatsName = FileName + " Floats" + ".json";
		string CurvesName = FileName + " Curves" + ".json";

		LoadedCurveValues = new Curve[4];
		LoadedFloatValues = new float[25];


		if ( FileSystem.Data.ReadJson<float[]>( FloatsName ) != null && FileSystem.Data.ReadJson<Curve[]>( CurvesName ) != null ) //Checks for file
		{
			//Loads File
			LoadedFloatValues = FileSystem.Data.ReadJson<float[]>( FloatsName );
			LoadedCurveValues = FileSystem.Data.ReadJson<Curve[]>( CurvesName );

		}
		else
		{

			Log.Error( "FILE NOT FOUND" );

		}
		


	}



}
