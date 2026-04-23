using Sandbox;
using Sandbox.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;



public sealed class TerrainManager : Component
{
	

	[Property]
	[Category( "LoadConfig" ), Description( "Enable/Disable Load Config Settings From File" ) ]
	public bool LoadConfigFromFile { get; set; }

	[Property]
	[Category( "LoadConfig" ), ShowIf( "LoadConfigFromFile", true ), Description( "Config File Name" )]
	public string ConfigFileName { get; set; }


	[Property]
	[Category( "Debug" ), Description( "Show/Hide Configuration Settings" )]
	public bool ConfigureMode { get; set; }

	[Property]
	[Category( "Debug" ), ShowIf( "ConfigureMode", true ), Description("Regenerates the terrain with current config settings  ! Important ONLY USE IN STARTING CHUNK, when used will desync the surrounding area with newly generated chunks") ] 

	public bool RegenNoise { get; set; } 

	[Space]


	[Property]
	[Category("Noise"), ShowIf("ConfigureMode", true), Range( 0, 5, 1), Description("Controls how many pixels are sampled  ! Dont change at runtime !  0 = 32  1 = 64  2 = 128  3 = 256  4 = 512  6 = 1024 ")]

	public int SetResolution { get; set; }

	[Property]
	[Category("Noise"), ShowIf("ConfigureMode", true), Range( 32, 2048, 1), Description("Controls the size of the sample area")]
	public int SampleResolution { get; set; }

	[Property]
	[Category("Noise"), ShowIf("ConfigureMode", true), Description("Offsets the terrain data")]

	public Vector2 TerrainOffset { get; set; }

	[Property]
	[Category( "Noise" ), ShowIf( "ConfigureMode", true ), Description( "Controls the height of the terrain relative to the noise's sample value" ) ]
	public Curve HeightCurve { get; set; }

	[Property]
	[Category("Noise"), ShowIf("ConfigureMode", true), Range(-100, 100, 1), Description("Controls how fast the height should falloff  0 = no falloff ")]
	public int FalloffMapSize { get; set; }

	[Property]
	[Category("Noise"), ShowIf("ConfigureMode", true), Description("Controls where the center of the falloff map is ")]
	public Vector2 FalloffCenter { get; set; }

	[Space]

	[Property]
	[Category( "Noise" ), Range( 0, 2, 1f, true ), ShowIf( "ConfigureMode", true ), Description( "Controls the governing noise algorithm ( 0 = Perlin Noise, 1 = Simplex Noise, 2 = Value Noise )" ) ]
	public int Noise1Type { get; set; }

	[Property]
	[Category( "Noise" ), Range( 0, 5000, 1f, true ), ShowIf( "ConfigureMode", true ), Description( "Sets the noise base seed" )]
	public int Noise1Seed { get; set; }
	
	[Property]
	[Category( "Noise" ), Range( 0, 10, 1f, true ), ShowIf( "ConfigureMode", true ), Description("Sets the noise base octave | Octave: How many layers of noise to use.")]
	public int Noise1Octave { get; set; }

	[Property]
	[Category("Noise"), Range(0, 10, .1f, true), ShowIf("ConfigureMode", true), Description("Sets the noise base lacunarity | Lacunarity: How much to multiply the frequency of each successive octave by.")]
	public float Noise1lacunarity { get; set; }

	[Property]
	[Category("Noise"), Range(0, 10, .1f, true), ShowIf("ConfigureMode", true), Description("Sets the noise base Gain | Gain: How much to multiply the amplitude of each successive octave by.")]
	public float Noise1Gain { get; set; }

	[Property]
	[Category( "Noise" ), ShowIf( "ConfigureMode", true ), Description( "Controls the weight of this noise's values relative to the other noise's values" ) ]
	public Curve Noise1Weight { get; set; }

	[Property]
	[Category( "Noise" ), Range( 0, .25f, 0.001f, true ), ShowIf( "ConfigureMode", true ), Description("Controls the noise's frequency | Frequency: How quickly should samples change across space.") ]
	public float Noise1Frequency { get; set; }



	[Space]

	[Property]
	[Category( "Noise" ), Range( 0, 2, 1f, true ), ShowIf( "ConfigureMode", true ), Description( "Controls the governing noise algorithm ( 0 = Perlin Noise, 1 = Simplex Noise, 2 = Value Noise )" )]
	public int Noise2Type { get; set; }

	[Property]
	[Category( "Noise" ), Range( 0, 5000, 1f, true ), ShowIf( "ConfigureMode", true ), Description( "Sets the noise base seed" )]
	public int Noise2Seed { get; set; }

	[Property]
	[Category("Noise"), Range(0, 10, 1f, true), ShowIf("ConfigureMode", true), Description("Sets the noise base octave | Octave: How many layers of noise to use.")]
	public int Noise2Octave { get; set; }
	
	[Property]
	[Category("Noise"), Range(0, 10, .1f, true), ShowIf("ConfigureMode", true), Description("Sets the noise base lacunarity | Lacunarity: How much to multiply the frequency of each successive octave by.")]
	public float Noise2lacunarity { get; set; }
	
	[Property]
	[Category("Noise"), Range(0, 10, .1f, true), ShowIf("ConfigureMode", true), Description("Sets the noise base Gain | Gain: How much to multiply the amplitude of each successive octave by.")]
	public float Noise2Gain { get; set; }

	[Property]
	[Category( "Noise" ), ShowIf( "ConfigureMode", true ), Description( "Controls the weight of this noise's values relative to the other noise's values" )]
	public Curve Noise2Weight { get; set; }

	[Property]
	[Category( "Noise" ), Range( 0, .25f, 0.001f, true ), ShowIf( "ConfigureMode", true ), Description("Controls the noise's frequency | Frequency: How quickly should samples change across space.")]
	public float Noise2Frequency { get; set; }



	[Space]

	[Property]
	[Category( "Noise" ), Range( 0, 2, 1f, true ), ShowIf( "ConfigureMode", true ), Description( "Controls the governing noise algorithm ( 0 = Perlin Noise, 1 = Simplex Noise, 2 = Value Noise )" )]
	public int Noise3Type { get; set; }

	[Property]
	[Category( "Noise" ), Range( 0, 5000, 1f, true ), ShowIf( "ConfigureMode", true ), Description( "Sets the noise base seed" )]
	public int Noise3Seed { get; set; }

	[Property]
	[Category("Noise"), Range(0, 10, 1f, true), ShowIf("ConfigureMode", true), Description("Sets the noise base octave | Octave: How many layers of noise to use.")]
	public int Noise3Octave { get; set; }

	[Property]
	[Category("Noise"), Range(0, 10, .1f, true), ShowIf("ConfigureMode", true), Description("Sets the noise base lacunarity | Lacunarity: How much to multiply the frequency of each successive octave by.")]
	public float Noise3lacunarity { get; set; }

	[Property]
	[Category("Noise"), Range(0, 10, .1f, true), ShowIf("ConfigureMode", true), Description("Sets the noise base Gain | Gain: How much to multiply the amplitude of each successive octave by.")]
	public float Noise3Gain { get; set; }

	[Property]
	[Category( "Noise" ), ShowIf( "ConfigureMode", true ), Description( "Controls the weight of this noise's values relative to the other noise's values" )]
	public Curve Noise3Weight { get; set; }

	[Property]
	[Category( "Noise" ), Range( 0, .25f, 0.001f, true ), ShowIf( "ConfigureMode", true ), Description("Controls the noise's frequency | Frequency: How quickly should samples change across space.")]
	public float Noise3Frequency { get; set; }



	[Property]
	[Category( "ExportNoise" ), ShowIf( "ConfigureMode", true ), Description( "Show/Hide Export Option's" )]
	public bool ExportOptions { get; set; }

	[Property]
	[Category( "ExportNoise" ), ShowIf( "ExportOptions", true), Description( "Show/Hide Export to config file option's" )]
	public bool ExportToConfigFileOptions { get; set; }

	[Property]
	[Category( "ExportNoise" ), ShowIf( "ExportToConfigFileOptions", true ), Description( "Exported file's name" )]
	public string ExportConfigName { get; set; }

	[Property]
	[Category( "ExportNoise" ), ShowIf( "ExportToConfigFileOptions", true ), Description( "Export Now" )]
	public bool ExportToConfigFileNow { get; set; }

	[Property]
	[Category( "ExportNoise" ), ShowIf( "ExportOptions", true ), Description( "Show/Hide Export to height map option's" )]
	public bool ExportAsHeightMapOptions { get; set; }

	[Property]
	[Category( "ExportNoise" ), ShowIf( "ExportAsHeightMapOptions", true ), Description( "Size of the exported height map ( In Chunk Scale )" )]
	public int ExportHeightMapSize { get; set; }
	
	[Property]
	[Category( "ExportNoise" ), ShowIf( "ExportAsHeightMapOptions", true ), Description( "Exported file's name" )]
	public string ExportHeightMapName { get; set; }

	[Property]
	[Category( "ExportNoise" ), ShowIf( "ExportAsHeightMapOptions", true ), Description( "Exports the current terrain as .raw files" ) ]
	public bool ExportAsHeightMapNow { get; set; }




	[Property]
	[Category( "Spawning" ), Description( "Material to set !Uses terrain material override!" )]
	public Material TerrainMat { get; set; }

	[Property]
	[Category( "Spawning" ), Description( "Terrain chunk prefab" )]
	public GameObject TerrainObj { get; set; }

	[Property]
	[Category( "Spawning" ), Description( "Space between chunks" )]
	public float ChunkSpaceing { get; set; }










	private int Resolution;  //True resolution value

	private INoiseField NoiseField; //Noise Layer 1
	private INoiseField NoiseField1; //Noise Layer 2
	private INoiseField NoiseField2; //Noise Layer 3

	private LocalFile LocalFile; //LocalFile component reference

	private List<CameraComponent> PlayerCameras = new List<CameraComponent>(); //List of players cameras

	private List<GameObject> SpawnedChunks = new List<GameObject>(); //List of Chunk gameobjects

	private List<Vector2> LastChunkSpacePOS = new List<Vector2>();//List of players chunk pos

	private List<byte[]> HeightMapDataStorage = new List<byte[]>();//List of all terrain data

	private List<byte[]> MaterialMapDataStorage = new List<byte[]>();//List of all terrain material data

	private List<Vector4> VoidDataStorage = new List<Vector4>();//List of Chunks with no data

	private int RenderDistance;//Render Distance

	private int DataStorageSize;//The length of one side of the square of the matrix that holds the terrian data

	private int FramesCount = 0;//Used to run function every 10 frames





	void WriteToFile()
	{

		//Calls localsave function and passes config info
		LocalFile.Save( ExportConfigName, HeightCurve, Noise1Type, Noise1Seed, Noise1Weight, Noise1Frequency, Noise2Type, Noise2Seed, Noise2Weight, Noise2Frequency, Noise3Type, Noise3Seed, Noise3Weight, Noise3Frequency, FalloffMapSize, SetResolution, TerrainOffset, FalloffCenter, SampleResolution, Noise1Octave, Noise2Octave, Noise3Octave, Noise1Gain, Noise2Gain, Noise3Gain, Noise1lacunarity, Noise2lacunarity, Noise3lacunarity );

		ExportToConfigFileNow = false;

	} //Function to write config to json

	void LoadData()
	{

		if( LoadConfigFromFile )
		{

			float[] NoiseFloats = new float[25];
			Curve[] NoiseCurves = new Curve[4];


			LocalFile.Load( ConfigFileName, out NoiseFloats, out NoiseCurves ); //calls load function and pulls the config data from the file



			//Sets Curves

			HeightCurve = NoiseCurves[0];
			Noise1Weight = NoiseCurves[1];
			Noise2Weight = NoiseCurves[2];
			Noise3Weight = NoiseCurves[3];


			//Sets floats

			Noise1Type = (int)NoiseFloats[0];
			Noise1Seed = (int)NoiseFloats[1];
			Noise1Frequency = NoiseFloats[2];
			Noise2Type = (int)NoiseFloats[3];
			Noise2Seed = (int)NoiseFloats[4];
			Noise2Frequency = NoiseFloats[5];
			Noise3Type = (int)NoiseFloats[6];
			Noise3Seed = (int)NoiseFloats[7];
			Noise3Frequency = NoiseFloats[8];
			FalloffMapSize = (int)NoiseFloats[9];
			SetResolution = (int)NoiseFloats[10];
			TerrainOffset = new Vector2(NoiseFloats[11], NoiseFloats[12] );
			FalloffCenter = new Vector2(NoiseFloats[13], NoiseFloats[14] );
			SampleResolution = (int)NoiseFloats[15];
			Noise1Octave = (int)NoiseFloats[16];
			Noise2Octave = (int)NoiseFloats[17];
			Noise3Octave = (int)NoiseFloats[18];
			Noise1Gain = NoiseFloats[19];
			Noise2Gain = NoiseFloats[20];
			Noise3Gain = NoiseFloats[21];
			Noise1lacunarity = NoiseFloats[22];
			Noise2lacunarity = NoiseFloats[23];
			Noise3lacunarity = NoiseFloats[24];

		}

	} //Load config data from file

	Vector2 FindVector2( float pos )
	{

		float interval = ((float)SampleResolution / (float)Resolution);

		float Y = interval * (float)(MathF.Floor(pos / SampleResolution));

		float X = pos % SampleResolution;



		return new Vector2( X, Y );

	} //Converts float to vector2

	Vector2 FindVector2ForExport( float pos )
	{

		//just adjusts the XY scope to ExportHeightMapSize

		float interval = ((float)SampleResolution / (float)Resolution);

		float Y = interval * (float)(MathF.Floor(pos / (ExportHeightMapSize * SampleResolution)));

		float X = pos % (ExportHeightMapSize * SampleResolution);



		return new Vector2( X, Y );

	} //Converts float to vector2 for .raw export

	int FindIntPosForDataSpace( Vector2 pos)
	{

		int temp = 0;

		temp += (int)pos.x;

		temp += (int)( pos.y * ((RenderDistance * 2) + 1 + 4 ));



		return temp;

	} //Converts vector2 to index

	Vector2 FindDataSpaceVector2FromInt( int pos )
	{



		int Y = pos / ((RenderDistance * 2) + 1 + 4 );

		int X = pos % ((RenderDistance * 2) + 1 + 4 );



		return new Vector2( X, Y );

	} //Converts index to dataspace matrix vector2

	Vector2 IntToChunkSpace( int Pos)
	{

		Vector2 temp = 0;

		int Y = Pos / (RenderDistance + RenderDistance + 1);

		int X = Pos % (RenderDistance + RenderDistance + 1);

		Vector2 DataPosCorrected = new Vector2( -((RenderDistance) ), -((RenderDistance) ) ) + new Vector2( X, Y);

		temp = DataPosCorrected;

		return temp;

	} //Index to chunk space vector2

	Vector2 DataSpaceToChunkSpace( Vector2 DataPos, Vector2 CenterOfChunks )
	{

		Vector2 temp = 0;

		Vector2 DataPosCorrected = new Vector2( -((RenderDistance) + 1 + 2), -((RenderDistance) + 1 + 2) ) + DataPos;

		temp = CenterOfChunks + DataPosCorrected;

		return temp;

	} //Converts dataspace to chunkspace

	Vector2 ChunkSpaceToDataSpace( Vector2 ChunkPos)
	{

		Vector2 Temp = 0;


		Temp = ChunkPos;

		Temp = new Vector2( ((RenderDistance) + 2), ((RenderDistance) + 2) ) + Temp;

		return Temp;

	} //Converts chunkspace to dataspace

	Vector2 FindChunkPOSFromWorldSpace( Vector3 WorldSpacePos )
	{

		int Y = (int)Math.Ceiling( (WorldSpacePos.y - 2500) / ChunkSpaceing );

		int X = (int)Math.Ceiling( (WorldSpacePos.x - 2500) / ChunkSpaceing );



		return new Vector2( X, Y );

	}  //Find Chunkspace vector2 from worldspace

	Vector3 ConvertChunkSpaceToWorldSpace( Vector3 ChunkSpace )
	{

		Vector3 Temp = 0;

		Temp = new Vector3( (ChunkSpace.x * ChunkSpaceing) - (ChunkSpaceing / 2), (ChunkSpace.y * ChunkSpaceing) - (ChunkSpaceing / 2), 0 );


		return Temp;

	} //Convert Chunkspace to worldspace

	GameObject CheckIfChunkExists( Vector2 ChunkSpacePos )
	{

		GameObject temp = null;


		Vector3 WorldSpacePOS = new Vector3( ChunkSpacePos.x * ChunkSpaceing, ChunkSpacePos.y * ChunkSpaceing, 0 );


		var Ray = Scene.Trace.Ray( new Vector3( WorldSpacePOS.x, WorldSpacePOS.y, -500 ), new Vector3( WorldSpacePOS.x, WorldSpacePOS.y, 1000 ) )
							.WithTag( "terrain" )
							.Size( 5f )
							.Run();

		if ( Ray.GameObject != null ) { temp = Ray.GameObject; }

		return temp;

	} //Checks to see if chunk at chunkspace pos exists

	void Sample16Pixels( float pos, Vector2 offset, out byte[] bytes, out byte[] MaterialBytes )
	{

		float[] floats = new float[16];

		bytes = new byte[16];
		MaterialBytes = new byte[16];

		float interval = ( (float)SampleResolution / (float)Resolution ); //Interval to increase by each step

		//Finds 16 vector2 positions
		Vector2 Vect2Pos = FindVector2( pos );
		Vector2 Vect2Pos1 = FindVector2( pos + interval);
		Vector2 Vect2Pos2 = FindVector2( pos + (interval * 2) );
		Vector2 Vect2Pos3 = FindVector2( pos + (interval * 3) );
		Vector2 Vect2Pos4 = FindVector2( pos + (interval * 4) );
		Vector2 Vect2Pos5 = FindVector2( pos + (interval * 5) );
		Vector2 Vect2Pos6 = FindVector2( pos + (interval * 6) );
		Vector2 Vect2Pos7 = FindVector2( pos + (interval * 7) );
		Vector2 Vect2Pos8 = FindVector2( pos + (interval * 8) );
		Vector2 Vect2Pos9 = FindVector2( pos + (interval * 9) );
		Vector2 Vect2Pos10 = FindVector2( pos + (interval * 10) );
		Vector2 Vect2Pos11 = FindVector2( pos + (interval * 11) );
		Vector2 Vect2Pos12 = FindVector2( pos + (interval * 12) );
		Vector2 Vect2Pos13 = FindVector2( pos + (interval * 13) );
		Vector2 Vect2Pos14 = FindVector2( pos + (interval * 14) );
		Vector2 Vect2Pos15 = FindVector2( pos + (interval * 15) );

		
		//Offsets positions
		Vect2Pos = new Vector2( Vect2Pos.x + offset.x, Vect2Pos.y + offset.y );
		Vect2Pos1 = new Vector2( Vect2Pos1.x + offset.x, Vect2Pos1.y + offset.y );
		Vect2Pos2 = new Vector2( Vect2Pos2.x + offset.x, Vect2Pos2.y + offset.y );
		Vect2Pos3 = new Vector2( Vect2Pos3.x + offset.x, Vect2Pos3.y + offset.y );
		Vect2Pos4 = new Vector2( Vect2Pos4.x + offset.x, Vect2Pos4.y + offset.y );
		Vect2Pos5 = new Vector2( Vect2Pos5.x + offset.x, Vect2Pos5.y + offset.y );
		Vect2Pos6 = new Vector2( Vect2Pos6.x + offset.x, Vect2Pos6.y + offset.y );
		Vect2Pos7 = new Vector2( Vect2Pos7.x + offset.x, Vect2Pos7.y + offset.y );
		Vect2Pos8 = new Vector2( Vect2Pos8.x + offset.x, Vect2Pos8.y + offset.y );
		Vect2Pos9 = new Vector2( Vect2Pos9.x + offset.x, Vect2Pos9.y + offset.y );
		Vect2Pos10 = new Vector2( Vect2Pos10.x + offset.x, Vect2Pos10.y + offset.y );
		Vect2Pos11 = new Vector2( Vect2Pos11.x + offset.x, Vect2Pos11.y + offset.y );
		Vect2Pos12 = new Vector2( Vect2Pos12.x + offset.x, Vect2Pos12.y + offset.y );
		Vect2Pos13 = new Vector2( Vect2Pos13.x + offset.x, Vect2Pos13.y + offset.y );
		Vect2Pos14 = new Vector2( Vect2Pos14.x + offset.x, Vect2Pos14.y + offset.y );
		Vect2Pos15 = new Vector2( Vect2Pos15.x + offset.x, Vect2Pos15.y + offset.y );


		//Samples and combines 3 noises

		floats[0] = (NoiseField.Sample( Vect2Pos ) * Noise1Weight.Evaluate( NoiseField.Sample( Vect2Pos )));
		floats[0] += (NoiseField1.Sample( Vect2Pos ) * Noise2Weight.Evaluate( NoiseField1.Sample( Vect2Pos )) );
		floats[0] += (NoiseField2.Sample(Vect2Pos) * (Noise3Weight.Evaluate(NoiseField2.Sample(Vect2Pos))));

		floats[1] = (NoiseField.Sample(Vect2Pos1) * Noise1Weight.Evaluate(NoiseField.Sample(Vect2Pos1)));
		floats[1] += (NoiseField1.Sample(Vect2Pos1) * Noise2Weight.Evaluate(NoiseField1.Sample(Vect2Pos1)));
		floats[1] += (NoiseField2.Sample(Vect2Pos1) * (Noise3Weight.Evaluate(NoiseField2.Sample(Vect2Pos1))));

		floats[2] = (NoiseField.Sample(Vect2Pos2) * Noise1Weight.Evaluate(NoiseField.Sample(Vect2Pos2)));
		floats[2] += (NoiseField1.Sample(Vect2Pos2) * Noise2Weight.Evaluate(NoiseField1.Sample(Vect2Pos2)));
		floats[2] += (NoiseField2.Sample(Vect2Pos2) * (Noise3Weight.Evaluate(NoiseField2.Sample(Vect2Pos2))));

		floats[3] = (NoiseField.Sample(Vect2Pos3) * Noise1Weight.Evaluate(NoiseField.Sample(Vect2Pos3)));
		floats[3] += (NoiseField1.Sample(Vect2Pos3) * Noise2Weight.Evaluate(NoiseField1.Sample(Vect2Pos3)));
		floats[3] += (NoiseField2.Sample(Vect2Pos3) * (Noise3Weight.Evaluate(NoiseField2.Sample(Vect2Pos3))));

		floats[4] = (NoiseField.Sample(Vect2Pos4) * Noise1Weight.Evaluate(NoiseField.Sample(Vect2Pos4)));
		floats[4] += (NoiseField1.Sample(Vect2Pos4) * Noise2Weight.Evaluate(NoiseField1.Sample(Vect2Pos4)));
		floats[4] += (NoiseField2.Sample(Vect2Pos4) * (Noise3Weight.Evaluate(NoiseField2.Sample(Vect2Pos4))));

		floats[5] = (NoiseField.Sample(Vect2Pos5) * Noise1Weight.Evaluate(NoiseField.Sample(Vect2Pos5)));
		floats[5] += (NoiseField1.Sample(Vect2Pos5) * Noise2Weight.Evaluate(NoiseField1.Sample(Vect2Pos5)));
		floats[5] += (NoiseField2.Sample(Vect2Pos5) * (Noise3Weight.Evaluate(NoiseField2.Sample(Vect2Pos5))));

		floats[6] = (NoiseField.Sample(Vect2Pos6) * Noise1Weight.Evaluate(NoiseField.Sample(Vect2Pos6)));
		floats[6] += (NoiseField1.Sample(Vect2Pos6) * Noise2Weight.Evaluate(NoiseField1.Sample(Vect2Pos6)));
		floats[6] += (NoiseField2.Sample(Vect2Pos6) * (Noise3Weight.Evaluate(NoiseField2.Sample(Vect2Pos6))));

		floats[7] = (NoiseField.Sample(Vect2Pos7) * Noise1Weight.Evaluate(NoiseField.Sample(Vect2Pos7)));
		floats[7] += (NoiseField1.Sample(Vect2Pos7) * Noise2Weight.Evaluate(NoiseField1.Sample(Vect2Pos7)));
		floats[7] += (NoiseField2.Sample(Vect2Pos7) * (Noise3Weight.Evaluate(NoiseField2.Sample(Vect2Pos7))));

		floats[8] = (NoiseField.Sample(Vect2Pos8) * Noise1Weight.Evaluate(NoiseField.Sample(Vect2Pos8)));
		floats[8] += (NoiseField1.Sample(Vect2Pos8) * Noise2Weight.Evaluate(NoiseField1.Sample(Vect2Pos8)));
		floats[8] += (NoiseField2.Sample(Vect2Pos8) * (Noise3Weight.Evaluate(NoiseField2.Sample(Vect2Pos8))));

		floats[9] = (NoiseField.Sample(Vect2Pos9) * Noise1Weight.Evaluate(NoiseField.Sample(Vect2Pos9)));
		floats[9] += (NoiseField1.Sample(Vect2Pos9) * Noise2Weight.Evaluate(NoiseField1.Sample(Vect2Pos9)));
		floats[9] += (NoiseField2.Sample(Vect2Pos9) * (Noise3Weight.Evaluate(NoiseField2.Sample(Vect2Pos9))));

		floats[10] = (NoiseField.Sample(Vect2Pos10) * Noise1Weight.Evaluate(NoiseField.Sample(Vect2Pos10)));
		floats[10] += (NoiseField1.Sample(Vect2Pos10) * Noise2Weight.Evaluate(NoiseField1.Sample(Vect2Pos10)));
		floats[10] += (NoiseField2.Sample(Vect2Pos10) * (Noise3Weight.Evaluate(NoiseField2.Sample(Vect2Pos10))));

		floats[11] = (NoiseField.Sample(Vect2Pos11) * Noise1Weight.Evaluate(NoiseField.Sample(Vect2Pos11)));
		floats[11] += (NoiseField1.Sample(Vect2Pos11) * Noise2Weight.Evaluate(NoiseField1.Sample(Vect2Pos11)));
		floats[11] += (NoiseField2.Sample(Vect2Pos11) * (Noise3Weight.Evaluate(NoiseField2.Sample(Vect2Pos11))));

		floats[12] = (NoiseField.Sample(Vect2Pos12) * Noise1Weight.Evaluate(NoiseField.Sample(Vect2Pos12)));
		floats[12] += (NoiseField1.Sample(Vect2Pos12) * Noise2Weight.Evaluate(NoiseField1.Sample(Vect2Pos12)));
		floats[12] += (NoiseField2.Sample(Vect2Pos12) * (Noise3Weight.Evaluate(NoiseField2.Sample(Vect2Pos12))));

		floats[13] = (NoiseField.Sample(Vect2Pos13) * Noise1Weight.Evaluate(NoiseField.Sample(Vect2Pos13)));
		floats[13] += (NoiseField1.Sample(Vect2Pos13) * Noise2Weight.Evaluate(NoiseField1.Sample(Vect2Pos13)));
		floats[13] += (NoiseField2.Sample(Vect2Pos13) * (Noise3Weight.Evaluate(NoiseField2.Sample(Vect2Pos13))));

		floats[14] = (NoiseField.Sample(Vect2Pos14) * Noise1Weight.Evaluate(NoiseField.Sample(Vect2Pos14)));
		floats[14] += (NoiseField1.Sample(Vect2Pos14) * Noise2Weight.Evaluate(NoiseField1.Sample(Vect2Pos14)));
		floats[14] += (NoiseField2.Sample(Vect2Pos14) * (Noise3Weight.Evaluate(NoiseField2.Sample(Vect2Pos14))));

		floats[15] = (NoiseField.Sample(Vect2Pos15) * Noise1Weight.Evaluate(NoiseField.Sample(Vect2Pos15)));
		floats[15] += (NoiseField1.Sample(Vect2Pos15) * Noise2Weight.Evaluate(NoiseField1.Sample(Vect2Pos15)));
		floats[15] += (NoiseField2.Sample(Vect2Pos15) * (Noise3Weight.Evaluate(NoiseField2.Sample(Vect2Pos15))));

		if( FalloffMapSize != 0 )//Falloff Map
		{

			Vector2 zero = FalloffCenter * 10;

			int multiAmount = 100;

			//Removes height depending on distance

			floats[0] -= (Vector2.DistanceBetween(Vect2Pos, zero) / ( FalloffMapSize * multiAmount));
			floats[1] -= (Vector2.DistanceBetween(Vect2Pos1, zero) / ( FalloffMapSize * multiAmount));
			floats[2] -= (Vector2.DistanceBetween(Vect2Pos2, zero) / ( FalloffMapSize * multiAmount));
			floats[3] -= (Vector2.DistanceBetween(Vect2Pos3, zero) / ( FalloffMapSize * multiAmount));
			floats[4] -= (Vector2.DistanceBetween(Vect2Pos4, zero) / ( FalloffMapSize * multiAmount));
			floats[5] -= (Vector2.DistanceBetween(Vect2Pos5, zero) / ( FalloffMapSize * multiAmount));
			floats[6] -= (Vector2.DistanceBetween(Vect2Pos6, zero) / ( FalloffMapSize * multiAmount));
			floats[7] -= (Vector2.DistanceBetween(Vect2Pos7, zero) / ( FalloffMapSize * multiAmount));
			floats[8] -= (Vector2.DistanceBetween(Vect2Pos8, zero) / ( FalloffMapSize * multiAmount));
			floats[9] -= (Vector2.DistanceBetween(Vect2Pos9, zero) / ( FalloffMapSize * multiAmount));
			floats[10] -= (Vector2.DistanceBetween(Vect2Pos10, zero) / ( FalloffMapSize * multiAmount));
			floats[11] -= (Vector2.DistanceBetween(Vect2Pos11, zero) / ( FalloffMapSize * multiAmount));
			floats[12] -= (Vector2.DistanceBetween(Vect2Pos12, zero) / ( FalloffMapSize * multiAmount));
			floats[13] -= (Vector2.DistanceBetween(Vect2Pos13, zero) / ( FalloffMapSize * multiAmount));
			floats[14] -= (Vector2.DistanceBetween(Vect2Pos14, zero) / ( FalloffMapSize * multiAmount));
			floats[15] -= (Vector2.DistanceBetween(Vect2Pos15, zero) / ( FalloffMapSize * multiAmount));

		}

		//Applies height curve & clamps
		bytes[0] = (byte)((Math.Clamp( floats[0] * HeightCurve.Evaluate( floats[0] ) * 255, 0, 255 )));
		bytes[1] = (byte)((Math.Clamp( floats[1] * HeightCurve.Evaluate( floats[1] ) * 255, 0, 255 )));
		bytes[2] = (byte)((Math.Clamp( floats[2] * HeightCurve.Evaluate( floats[2] ) * 255, 0, 255 )));
		bytes[3] = (byte)((Math.Clamp( floats[3] * HeightCurve.Evaluate( floats[3] ) * 255, 0, 255 )));
		bytes[4] = (byte)((Math.Clamp( floats[4] * HeightCurve.Evaluate( floats[4] ) * 255, 0, 255 )));
		bytes[5] = (byte)((Math.Clamp( floats[5] * HeightCurve.Evaluate( floats[5] ) * 255, 0, 255 )));
		bytes[6] = (byte)((Math.Clamp( floats[6] * HeightCurve.Evaluate( floats[6] ) * 255, 0, 255 )));
		bytes[7] = (byte)((Math.Clamp( floats[7] * HeightCurve.Evaluate( floats[7] ) * 255, 0, 255 )));
		bytes[8] = (byte)((Math.Clamp( floats[8] * HeightCurve.Evaluate( floats[8] ) * 255, 0, 255 )));
		bytes[9] = (byte)((Math.Clamp( floats[9] * HeightCurve.Evaluate( floats[9] ) * 255, 0, 255 )));
		bytes[10] = (byte)((Math.Clamp( floats[10] * HeightCurve.Evaluate( floats[10] ) * 255, 0, 255 )));
		bytes[11] = (byte)((Math.Clamp( floats[11] * HeightCurve.Evaluate( floats[11] ) * 255, 0, 255 )));
		bytes[12] = (byte)((Math.Clamp( floats[12] * HeightCurve.Evaluate( floats[12] ) * 255, 0, 255 )));
		bytes[13] = (byte)((Math.Clamp( floats[13] * HeightCurve.Evaluate( floats[13] ) * 255, 0, 255 )));
		bytes[14] = (byte)((Math.Clamp( floats[14] * HeightCurve.Evaluate( floats[14] ) * 255, 0, 255 )));
		bytes[15] = (byte)((Math.Clamp( floats[15] * HeightCurve.Evaluate( floats[15] ) * 255, 0, 255 )));

		//Gets Raw data without height curve for use with shader
		MaterialBytes[0] = (byte)(Math.Clamp( floats[0] * 255, 0, 255 ));
		MaterialBytes[1] = (byte)(Math.Clamp( floats[1] * 255, 0, 255 ));
		MaterialBytes[2] = (byte)(Math.Clamp( floats[2] * 255, 0, 255 ));
		MaterialBytes[3] = (byte)(Math.Clamp( floats[3] * 255, 0, 255 ));
		MaterialBytes[4] = (byte)(Math.Clamp( floats[4] * 255, 0, 255 ));
		MaterialBytes[5] = (byte)(Math.Clamp( floats[5] * 255, 0, 255 ));
		MaterialBytes[6] = (byte)(Math.Clamp( floats[6] * 255, 0, 255 ));
		MaterialBytes[7] = (byte)(Math.Clamp( floats[7] * 255, 0, 255 ));
		MaterialBytes[8] = (byte)(Math.Clamp( floats[8] * 255, 0, 255 ));
		MaterialBytes[9] = (byte)(Math.Clamp( floats[9] * 255, 0, 255 ));
		MaterialBytes[10] = (byte)(Math.Clamp( floats[10] * 255, 0, 255 ));
		MaterialBytes[11] = (byte)(Math.Clamp( floats[11] * 255, 0, 255 ));
		MaterialBytes[12] = (byte)(Math.Clamp( floats[12] * 255, 0, 255 ));
		MaterialBytes[13] = (byte)(Math.Clamp( floats[13] * 255, 0, 255 ));
		MaterialBytes[14] = (byte)(Math.Clamp( floats[14] * 255, 0, 255 ));
		MaterialBytes[15] = (byte)(Math.Clamp( floats[15] * 255, 0, 255 ));

	} //Samples 16 positions in each noise and combines into one value

	void Sample16PixelsForExport( float pos, Vector2 offset, out byte[] bytes, out byte[] MaterialBytes )
	{

		float[] floats = new float[16];

		bytes = new byte[16];
		MaterialBytes = new byte[16];


		float interval = (float)((float)SampleResolution / (float)Resolution);

		//Same as Sample16Pixels but uses FindVector2ForExport \/  FindVector2ForExport just adjusts the XY scope to ExportHeightMapSize

		Vector2 Vect2Pos = FindVector2ForExport(pos);
		Vector2 Vect2Pos1 = FindVector2ForExport(pos + interval);
		Vector2 Vect2Pos2 = FindVector2ForExport(pos + (interval * 2));
		Vector2 Vect2Pos3 = FindVector2ForExport(pos + (interval * 3));
		Vector2 Vect2Pos4 = FindVector2ForExport(pos + (interval * 4));
		Vector2 Vect2Pos5 = FindVector2ForExport(pos + (interval * 5));
		Vector2 Vect2Pos6 = FindVector2ForExport(pos + (interval * 6));
		Vector2 Vect2Pos7 = FindVector2ForExport(pos + (interval * 7));
		Vector2 Vect2Pos8 = FindVector2ForExport(pos + (interval * 8));
		Vector2 Vect2Pos9 = FindVector2ForExport(pos + (interval * 9));
		Vector2 Vect2Pos10 = FindVector2ForExport(pos + (interval * 10));
		Vector2 Vect2Pos11 = FindVector2ForExport(pos + (interval * 11));
		Vector2 Vect2Pos12 = FindVector2ForExport(pos + (interval * 12));
		Vector2 Vect2Pos13 = FindVector2ForExport(pos + (interval * 13));
		Vector2 Vect2Pos14 = FindVector2ForExport(pos + (interval * 14));
		Vector2 Vect2Pos15 = FindVector2ForExport(pos + (interval * 15));



		Vect2Pos = new Vector2( Vect2Pos.x + offset.x, Vect2Pos.y + offset.y );
		Vect2Pos1 = new Vector2( Vect2Pos1.x + offset.x, Vect2Pos1.y + offset.y );
		Vect2Pos2 = new Vector2( Vect2Pos2.x + offset.x, Vect2Pos2.y + offset.y );
		Vect2Pos3 = new Vector2( Vect2Pos3.x + offset.x, Vect2Pos3.y + offset.y );
		Vect2Pos4 = new Vector2( Vect2Pos4.x + offset.x, Vect2Pos4.y + offset.y );
		Vect2Pos5 = new Vector2( Vect2Pos5.x + offset.x, Vect2Pos5.y + offset.y );
		Vect2Pos6 = new Vector2( Vect2Pos6.x + offset.x, Vect2Pos6.y + offset.y );
		Vect2Pos7 = new Vector2( Vect2Pos7.x + offset.x, Vect2Pos7.y + offset.y );
		Vect2Pos8 = new Vector2( Vect2Pos8.x + offset.x, Vect2Pos8.y + offset.y );
		Vect2Pos9 = new Vector2( Vect2Pos9.x + offset.x, Vect2Pos9.y + offset.y );
		Vect2Pos10 = new Vector2( Vect2Pos10.x + offset.x, Vect2Pos10.y + offset.y );
		Vect2Pos11 = new Vector2( Vect2Pos11.x + offset.x, Vect2Pos11.y + offset.y );
		Vect2Pos12 = new Vector2( Vect2Pos12.x + offset.x, Vect2Pos12.y + offset.y );
		Vect2Pos13 = new Vector2( Vect2Pos13.x + offset.x, Vect2Pos13.y + offset.y );
		Vect2Pos14 = new Vector2( Vect2Pos14.x + offset.x, Vect2Pos14.y + offset.y );
		Vect2Pos15 = new Vector2( Vect2Pos15.x + offset.x, Vect2Pos15.y + offset.y );


		floats[0] = (NoiseField.Sample( Vect2Pos ) * Noise1Weight.Evaluate( NoiseField.Sample( Vect2Pos ) ));
		floats[0] += (NoiseField1.Sample( Vect2Pos ) * Noise2Weight.Evaluate( NoiseField1.Sample( Vect2Pos ) ));
		floats[0] += (NoiseField2.Sample( Vect2Pos ) * (Noise3Weight.Evaluate( NoiseField2.Sample( Vect2Pos ) )));

		floats[1] = (NoiseField.Sample( Vect2Pos1 ) * Noise1Weight.Evaluate( NoiseField.Sample( Vect2Pos1 ) ));
		floats[1] += (NoiseField1.Sample( Vect2Pos1 ) * Noise2Weight.Evaluate( NoiseField1.Sample( Vect2Pos1 ) ));
		floats[1] += (NoiseField2.Sample( Vect2Pos1 ) * (Noise3Weight.Evaluate( NoiseField2.Sample( Vect2Pos1 ) )));

		floats[2] = (NoiseField.Sample( Vect2Pos2 ) * Noise1Weight.Evaluate( NoiseField.Sample( Vect2Pos2 ) ));
		floats[2] += (NoiseField1.Sample( Vect2Pos2 ) * Noise2Weight.Evaluate( NoiseField1.Sample( Vect2Pos2 ) ));
		floats[2] += (NoiseField2.Sample( Vect2Pos2 ) * (Noise3Weight.Evaluate( NoiseField2.Sample( Vect2Pos2 ) )));

		floats[3] = (NoiseField.Sample( Vect2Pos3 ) * Noise1Weight.Evaluate( NoiseField.Sample( Vect2Pos3 ) ));
		floats[3] += (NoiseField1.Sample( Vect2Pos3 ) * Noise2Weight.Evaluate( NoiseField1.Sample( Vect2Pos3 ) ));
		floats[3] += (NoiseField2.Sample( Vect2Pos3 ) * (Noise3Weight.Evaluate( NoiseField2.Sample( Vect2Pos3 ) )));

		floats[4] = (NoiseField.Sample( Vect2Pos4 ) * Noise1Weight.Evaluate( NoiseField.Sample( Vect2Pos4 ) ));
		floats[4] += (NoiseField1.Sample( Vect2Pos4 ) * Noise2Weight.Evaluate( NoiseField1.Sample( Vect2Pos4 ) ));
		floats[4] += (NoiseField2.Sample( Vect2Pos4 ) * (Noise3Weight.Evaluate( NoiseField2.Sample( Vect2Pos4 ) )));

		floats[5] = (NoiseField.Sample( Vect2Pos5 ) * Noise1Weight.Evaluate( NoiseField.Sample( Vect2Pos5 ) ));
		floats[5] += (NoiseField1.Sample( Vect2Pos5 ) * Noise2Weight.Evaluate( NoiseField1.Sample( Vect2Pos5 ) ));
		floats[5] += (NoiseField2.Sample( Vect2Pos5 ) * (Noise3Weight.Evaluate( NoiseField2.Sample( Vect2Pos5 ) )));

		floats[6] = (NoiseField.Sample( Vect2Pos6 ) * Noise1Weight.Evaluate( NoiseField.Sample( Vect2Pos6 ) ));
		floats[6] += (NoiseField1.Sample( Vect2Pos6 ) * Noise2Weight.Evaluate( NoiseField1.Sample( Vect2Pos6 ) ));
		floats[6] += (NoiseField2.Sample( Vect2Pos6 ) * (Noise3Weight.Evaluate( NoiseField2.Sample( Vect2Pos6 ) )));

		floats[7] = (NoiseField.Sample( Vect2Pos7 ) * Noise1Weight.Evaluate( NoiseField.Sample( Vect2Pos7 ) ));
		floats[7] += (NoiseField1.Sample( Vect2Pos7 ) * Noise2Weight.Evaluate( NoiseField1.Sample( Vect2Pos7 ) ));
		floats[7] += (NoiseField2.Sample( Vect2Pos7 ) * (Noise3Weight.Evaluate( NoiseField2.Sample( Vect2Pos7 ) )));

		floats[8] = (NoiseField.Sample( Vect2Pos8 ) * Noise1Weight.Evaluate( NoiseField.Sample( Vect2Pos8 ) ));
		floats[8] += (NoiseField1.Sample( Vect2Pos8 ) * Noise2Weight.Evaluate( NoiseField1.Sample( Vect2Pos8 ) ));
		floats[8] += (NoiseField2.Sample( Vect2Pos8 ) * (Noise3Weight.Evaluate( NoiseField2.Sample( Vect2Pos8 ) )));

		floats[9] = (NoiseField.Sample( Vect2Pos9 ) * Noise1Weight.Evaluate( NoiseField.Sample( Vect2Pos9 ) ));
		floats[9] += (NoiseField1.Sample( Vect2Pos9 ) * Noise2Weight.Evaluate( NoiseField1.Sample( Vect2Pos9 ) ));
		floats[9] += (NoiseField2.Sample( Vect2Pos9 ) * (Noise3Weight.Evaluate( NoiseField2.Sample( Vect2Pos9 ) )));

		floats[10] = (NoiseField.Sample( Vect2Pos10 ) * Noise1Weight.Evaluate( NoiseField.Sample( Vect2Pos10 ) ));
		floats[10] += (NoiseField1.Sample( Vect2Pos10 ) * Noise2Weight.Evaluate( NoiseField1.Sample( Vect2Pos10 ) ));
		floats[10] += (NoiseField2.Sample( Vect2Pos10 ) * (Noise3Weight.Evaluate( NoiseField2.Sample( Vect2Pos10 ) )));

		floats[11] = (NoiseField.Sample( Vect2Pos11 ) * Noise1Weight.Evaluate( NoiseField.Sample( Vect2Pos11 ) ));
		floats[11] += (NoiseField1.Sample( Vect2Pos11 ) * Noise2Weight.Evaluate( NoiseField1.Sample( Vect2Pos11 ) ));
		floats[11] += (NoiseField2.Sample( Vect2Pos11 ) * (Noise3Weight.Evaluate( NoiseField2.Sample( Vect2Pos11 ) )));

		floats[12] = (NoiseField.Sample( Vect2Pos12 ) * Noise1Weight.Evaluate( NoiseField.Sample( Vect2Pos12 ) ));
		floats[12] += (NoiseField1.Sample( Vect2Pos12 ) * Noise2Weight.Evaluate( NoiseField1.Sample( Vect2Pos12 ) ));
		floats[12] += (NoiseField2.Sample( Vect2Pos12 ) * (Noise3Weight.Evaluate( NoiseField2.Sample( Vect2Pos12 ) )));

		floats[13] = (NoiseField.Sample( Vect2Pos13 ) * Noise1Weight.Evaluate( NoiseField.Sample( Vect2Pos13 ) ));
		floats[13] += (NoiseField1.Sample( Vect2Pos13 ) * Noise2Weight.Evaluate( NoiseField1.Sample( Vect2Pos13 ) ));
		floats[13] += (NoiseField2.Sample( Vect2Pos13 ) * (Noise3Weight.Evaluate( NoiseField2.Sample( Vect2Pos13 ) )));

		floats[14] = (NoiseField.Sample( Vect2Pos14 ) * Noise1Weight.Evaluate( NoiseField.Sample( Vect2Pos14 ) ));
		floats[14] += (NoiseField1.Sample( Vect2Pos14 ) * Noise2Weight.Evaluate( NoiseField1.Sample( Vect2Pos14 ) ));
		floats[14] += (NoiseField2.Sample( Vect2Pos14 ) * (Noise3Weight.Evaluate( NoiseField2.Sample( Vect2Pos14 ) )));

		floats[15] = (NoiseField.Sample( Vect2Pos15 ) * Noise1Weight.Evaluate( NoiseField.Sample( Vect2Pos15 ) ));
		floats[15] += (NoiseField1.Sample( Vect2Pos15 ) * Noise2Weight.Evaluate( NoiseField1.Sample( Vect2Pos15 ) ));
		floats[15] += (NoiseField2.Sample( Vect2Pos15 ) * (Noise3Weight.Evaluate( NoiseField2.Sample( Vect2Pos15 ) )));


		if (FalloffMapSize != 0)
		{

			Vector2 zero = FalloffCenter * 10;

			int multiAmount = 100;

			floats[0] -= (Vector2.DistanceBetween(Vect2Pos, zero) / (FalloffMapSize * multiAmount));
			floats[1] -= (Vector2.DistanceBetween(Vect2Pos1, zero) / (FalloffMapSize * multiAmount));
			floats[2] -= (Vector2.DistanceBetween(Vect2Pos2, zero) / (FalloffMapSize * multiAmount));
			floats[3] -= (Vector2.DistanceBetween(Vect2Pos3, zero) / (FalloffMapSize * multiAmount));
			floats[4] -= (Vector2.DistanceBetween(Vect2Pos4, zero) / (FalloffMapSize * multiAmount));
			floats[5] -= (Vector2.DistanceBetween(Vect2Pos5, zero) / (FalloffMapSize * multiAmount));
			floats[6] -= (Vector2.DistanceBetween(Vect2Pos6, zero) / (FalloffMapSize * multiAmount));
			floats[7] -= (Vector2.DistanceBetween(Vect2Pos7, zero) / (FalloffMapSize * multiAmount));
			floats[8] -= (Vector2.DistanceBetween(Vect2Pos8, zero) / (FalloffMapSize * multiAmount));
			floats[9] -= (Vector2.DistanceBetween(Vect2Pos9, zero) / (FalloffMapSize * multiAmount));
			floats[10] -= (Vector2.DistanceBetween(Vect2Pos10, zero) / (FalloffMapSize * multiAmount));
			floats[11] -= (Vector2.DistanceBetween(Vect2Pos11, zero) / (FalloffMapSize * multiAmount));
			floats[12] -= (Vector2.DistanceBetween(Vect2Pos12, zero) / (FalloffMapSize * multiAmount));
			floats[13] -= (Vector2.DistanceBetween(Vect2Pos13, zero) / (FalloffMapSize * multiAmount));
			floats[14] -= (Vector2.DistanceBetween(Vect2Pos14, zero) / (FalloffMapSize * multiAmount));
			floats[15] -= (Vector2.DistanceBetween(Vect2Pos15, zero) / (FalloffMapSize * multiAmount));

		}



		bytes[0] = (byte)((Math.Clamp( floats[0] * HeightCurve.Evaluate( floats[0] ) * 255, 0, 255)));
		bytes[1] = (byte)((Math.Clamp( floats[1] * HeightCurve.Evaluate( floats[1] ) * 255, 0, 255 )));
		bytes[2] = (byte)((Math.Clamp( floats[2] * HeightCurve.Evaluate( floats[2] ) * 255, 0, 255 )));
		bytes[3] = (byte)((Math.Clamp( floats[3] * HeightCurve.Evaluate( floats[3] ) * 255, 0, 255 )));
		bytes[4] = (byte)((Math.Clamp( floats[4] * HeightCurve.Evaluate( floats[4] ) * 255, 0, 255 )));
		bytes[5] = (byte)((Math.Clamp( floats[5] * HeightCurve.Evaluate( floats[5] ) * 255, 0, 255 )));
		bytes[6] = (byte)((Math.Clamp( floats[6] * HeightCurve.Evaluate( floats[6] ) * 255, 0, 255 )));
		bytes[7] = (byte)((Math.Clamp(floats[7] * HeightCurve.Evaluate(floats[7]) * 255, 0, 255)));
		bytes[8] = (byte)((Math.Clamp(floats[8] * HeightCurve.Evaluate(floats[8]) * 255, 0, 255)));
		bytes[9] = (byte)((Math.Clamp(floats[9] * HeightCurve.Evaluate(floats[9]) * 255, 0, 255)));
		bytes[10] = (byte)((Math.Clamp(floats[10] * HeightCurve.Evaluate(floats[10]) * 255, 0, 255)));
		bytes[11] = (byte)((Math.Clamp(floats[11] * HeightCurve.Evaluate(floats[11]) * 255, 0, 255)));
		bytes[12] = (byte)((Math.Clamp(floats[12] * HeightCurve.Evaluate(floats[12]) * 255, 0, 255)));
		bytes[13] = (byte)((Math.Clamp(floats[13] * HeightCurve.Evaluate(floats[13]) * 255, 0, 255)));
		bytes[14] = (byte)((Math.Clamp(floats[14] * HeightCurve.Evaluate(floats[14]) * 255, 0, 255)));
		bytes[15] = (byte)((Math.Clamp(floats[15] * HeightCurve.Evaluate(floats[15]) * 255, 0, 255)));


		MaterialBytes[0] = (byte)(Math.Clamp( floats[0] * 255 , 0, 255));
		MaterialBytes[1] = (byte)(Math.Clamp( floats[1] * 255, 0, 255 ));
		MaterialBytes[2] = (byte)(Math.Clamp(floats[2] * 255, 0, 255));
		MaterialBytes[3] = (byte)(Math.Clamp(floats[3] * 255, 0, 255));
		MaterialBytes[4] = (byte)(Math.Clamp(floats[4] * 255, 0, 255));
		MaterialBytes[5] = (byte)(Math.Clamp(floats[5] * 255, 0, 255));
		MaterialBytes[6] = (byte)(Math.Clamp(floats[6] * 255, 0, 255));
		MaterialBytes[7] = (byte)(Math.Clamp(floats[7] * 255, 0, 255));
		MaterialBytes[8] = (byte)(Math.Clamp(floats[8] * 255, 0, 255));
		MaterialBytes[9] = (byte)(Math.Clamp(floats[9] * 255, 0, 255));
		MaterialBytes[10] = (byte)(Math.Clamp(floats[10] * 255, 0, 255));
		MaterialBytes[11] = (byte)(Math.Clamp(floats[11] * 255, 0, 255));
		MaterialBytes[12] = (byte)(Math.Clamp(floats[12] * 255, 0, 255));
		MaterialBytes[13] = (byte)(Math.Clamp(floats[13] * 255, 0, 255));
		MaterialBytes[14] = (byte)(Math.Clamp(floats[14] * 255, 0, 255));
		MaterialBytes[15] = (byte)(Math.Clamp(floats[15] * 255, 0, 255));

	} //Samples 16 positions in each noise and combines into one value for .raw exports

	void Format16NoiseVariable( byte[] FuncOutput, byte[] MatFuncOutput, out byte[] NoiseDataSolo, out byte[] NoiseData )
	{


		NoiseDataSolo = new byte[32];
		NoiseData = new byte[64];

		//Formats 16 values into 2 channels

		NoiseDataSolo[0] = FuncOutput[0];
		NoiseDataSolo[1] = FuncOutput[0];

		NoiseDataSolo[2] = FuncOutput[1];
		NoiseDataSolo[3] = FuncOutput[1];

		NoiseDataSolo[4] = FuncOutput[2];
		NoiseDataSolo[5] = FuncOutput[2];

		NoiseDataSolo[6] = FuncOutput[3];
		NoiseDataSolo[7] = FuncOutput[3];

		NoiseDataSolo[8] = FuncOutput[4];
		NoiseDataSolo[9] = FuncOutput[4];

		NoiseDataSolo[10] = FuncOutput[5];
		NoiseDataSolo[11] = FuncOutput[5];

		NoiseDataSolo[12] = FuncOutput[6];
		NoiseDataSolo[13] = FuncOutput[6];

		NoiseDataSolo[14] = FuncOutput[7];
		NoiseDataSolo[15] = FuncOutput[7];

		NoiseDataSolo[16] = FuncOutput[8];
		NoiseDataSolo[17] = FuncOutput[8];

		NoiseDataSolo[18] = FuncOutput[9];
		NoiseDataSolo[19] = FuncOutput[9];

		NoiseDataSolo[20] = FuncOutput[10];
		NoiseDataSolo[21] = FuncOutput[10];

		NoiseDataSolo[22] = FuncOutput[11];
		NoiseDataSolo[23] = FuncOutput[11];

		NoiseDataSolo[24] = FuncOutput[12];
		NoiseDataSolo[25] = FuncOutput[12];

		NoiseDataSolo[26] = FuncOutput[13];
		NoiseDataSolo[27] = FuncOutput[13];

		NoiseDataSolo[28] = FuncOutput[14];
		NoiseDataSolo[29] = FuncOutput[14];

		NoiseDataSolo[30] = FuncOutput[15];
		NoiseDataSolo[31] = FuncOutput[15];


		//Formats 16 values into 4 channels

		NoiseData[0] = MatFuncOutput[0];
		NoiseData[1] = MatFuncOutput[0];
		NoiseData[2] = MatFuncOutput[0];
		NoiseData[3] = MatFuncOutput[0];

		NoiseData[4] = MatFuncOutput[1];
		NoiseData[5] = MatFuncOutput[1];
		NoiseData[6] = MatFuncOutput[1];
		NoiseData[7] = MatFuncOutput[1];

		NoiseData[8] = MatFuncOutput[2];
		NoiseData[9] = MatFuncOutput[2];
		NoiseData[10] = MatFuncOutput[2];
		NoiseData[11] = MatFuncOutput[2];

		NoiseData[12] = MatFuncOutput[3];
		NoiseData[13] = MatFuncOutput[3];
		NoiseData[14] = MatFuncOutput[3];
		NoiseData[15] = MatFuncOutput[3];

		NoiseData[16] = MatFuncOutput[4];
		NoiseData[17] = MatFuncOutput[4];
		NoiseData[18] = MatFuncOutput[4];
		NoiseData[19] = MatFuncOutput[4];

		NoiseData[20] = MatFuncOutput[5];
		NoiseData[21] = MatFuncOutput[5];
		NoiseData[22] = MatFuncOutput[5];
		NoiseData[23] = MatFuncOutput[5];

		NoiseData[24] = MatFuncOutput[6];
		NoiseData[25] = MatFuncOutput[6];
		NoiseData[26] = MatFuncOutput[6];
		NoiseData[27] = MatFuncOutput[6];

		NoiseData[28] = MatFuncOutput[7];
		NoiseData[29] = MatFuncOutput[7];
		NoiseData[30] = MatFuncOutput[7];
		NoiseData[31] = MatFuncOutput[7];

		NoiseData[32] = MatFuncOutput[8];
		NoiseData[33] = MatFuncOutput[8];
		NoiseData[34] = MatFuncOutput[8];
		NoiseData[35] = MatFuncOutput[8];

		NoiseData[36] = MatFuncOutput[9];
		NoiseData[37] = MatFuncOutput[9];
		NoiseData[38] = MatFuncOutput[9];
		NoiseData[39] = MatFuncOutput[9];

		NoiseData[40] = MatFuncOutput[10];
		NoiseData[41] = MatFuncOutput[10];
		NoiseData[42] = MatFuncOutput[10];
		NoiseData[43] = MatFuncOutput[10];

		NoiseData[44] = MatFuncOutput[11];
		NoiseData[45] = MatFuncOutput[11];
		NoiseData[46] = MatFuncOutput[11];
		NoiseData[47] = MatFuncOutput[11];

		NoiseData[48] = MatFuncOutput[12];
		NoiseData[49] = MatFuncOutput[12];
		NoiseData[50] = MatFuncOutput[12];
		NoiseData[51] = MatFuncOutput[12];

		NoiseData[52] = MatFuncOutput[13];
		NoiseData[53] = MatFuncOutput[13];
		NoiseData[54] = MatFuncOutput[13];
		NoiseData[55] = MatFuncOutput[13];

		NoiseData[56] = MatFuncOutput[14];
		NoiseData[57] = MatFuncOutput[14];
		NoiseData[58] = MatFuncOutput[14];
		NoiseData[59] = MatFuncOutput[14];

		NoiseData[60] = MatFuncOutput[15];
		NoiseData[61] = MatFuncOutput[15];
		NoiseData[62] = MatFuncOutput[15];
		NoiseData[63] = MatFuncOutput[15];

	} //Formats the sampled info for terrain

	void SampleAndFormat256( int I, Vector2 Offset, out byte[] NoiseData, out byte[] SoloNoiseData )
	{

		List<byte> SoloNoiseList = new List<byte>();
		List<byte> NoiseList = new List<byte>();

		float interval = ((float)SampleResolution / (float)Resolution);

		float i = (float)(I * interval);

			byte[] temp1 = new byte[32];
			byte[] temp2 = new byte[64];
			byte[] FuncOutput = new byte[32];
			byte[] MatFuncOutput = new byte[64];


			//Samples and formats 16 times and adds each to a list

			Sample16Pixels( i, Offset, out FuncOutput, out MatFuncOutput ); 

			Format16NoiseVariable( FuncOutput, MatFuncOutput, out temp1, out temp2 );

			SoloNoiseList.AddRange( temp1 );
			NoiseList.AddRange( temp2 );


			Sample16Pixels( i + (float)(16 * interval), Offset, out FuncOutput, out MatFuncOutput );

			Format16NoiseVariable( FuncOutput, MatFuncOutput, out temp1, out temp2 );

			SoloNoiseList.AddRange( temp1 );
			NoiseList.AddRange( temp2 );


			Sample16Pixels(i + (float)(32 * interval), Offset, out FuncOutput, out MatFuncOutput);

			Format16NoiseVariable( FuncOutput, MatFuncOutput, out temp1, out temp2 );

			SoloNoiseList.AddRange( temp1 );
			NoiseList.AddRange( temp2 );


			Sample16Pixels( i + (float)(48 * interval), Offset, out FuncOutput, out MatFuncOutput );

			Format16NoiseVariable( FuncOutput, MatFuncOutput, out temp1, out temp2 );

			SoloNoiseList.AddRange( temp1 );
			NoiseList.AddRange( temp2 );


			Sample16Pixels(i + (float)(64 * interval), Offset, out FuncOutput, out MatFuncOutput);

			Format16NoiseVariable( FuncOutput, MatFuncOutput, out temp1, out temp2 );

			SoloNoiseList.AddRange( temp1 );
			NoiseList.AddRange( temp2 );


			Sample16Pixels( i + (float)(80 * interval), Offset, out FuncOutput, out MatFuncOutput );

			Format16NoiseVariable( FuncOutput, MatFuncOutput, out temp1, out temp2 );

			SoloNoiseList.AddRange( temp1 );
			NoiseList.AddRange( temp2 );


			Sample16Pixels(i + (float)(96 * interval), Offset, out FuncOutput, out MatFuncOutput);

			Format16NoiseVariable( FuncOutput, MatFuncOutput, out temp1, out temp2 );

			SoloNoiseList.AddRange( temp1 );
			NoiseList.AddRange( temp2 );


			Sample16Pixels(i + (float)(112 * interval), Offset, out FuncOutput, out MatFuncOutput);

			Format16NoiseVariable( FuncOutput, MatFuncOutput, out temp1, out temp2 );

			SoloNoiseList.AddRange( temp1 );
			NoiseList.AddRange( temp2 );


			Sample16Pixels(i + (float)(128 * interval), Offset, out FuncOutput, out MatFuncOutput);

			Format16NoiseVariable( FuncOutput, MatFuncOutput, out temp1, out temp2 );

			SoloNoiseList.AddRange( temp1 );
			NoiseList.AddRange( temp2 );


			Sample16Pixels(i + (float)(144 * interval), Offset, out FuncOutput, out MatFuncOutput);

			Format16NoiseVariable( FuncOutput, MatFuncOutput, out temp1, out temp2 );

			SoloNoiseList.AddRange( temp1 );
			NoiseList.AddRange( temp2 );


			Sample16Pixels(i + (float)(160 * interval), Offset, out FuncOutput, out MatFuncOutput);

			Format16NoiseVariable( FuncOutput, MatFuncOutput, out temp1, out temp2 );

			SoloNoiseList.AddRange( temp1 );
			NoiseList.AddRange( temp2 );


			Sample16Pixels(i + (float)(176 * interval), Offset, out FuncOutput, out MatFuncOutput);

			Format16NoiseVariable( FuncOutput, MatFuncOutput, out temp1, out temp2 );

			SoloNoiseList.AddRange( temp1 );
			NoiseList.AddRange( temp2 );


			Sample16Pixels(i + (float)(192 * interval), Offset, out FuncOutput, out MatFuncOutput);

			Format16NoiseVariable( FuncOutput, MatFuncOutput, out temp1, out temp2 );

			SoloNoiseList.AddRange( temp1 );
			NoiseList.AddRange( temp2 );


			Sample16Pixels(i + (float)(208 * interval), Offset, out FuncOutput, out MatFuncOutput);

			Format16NoiseVariable( FuncOutput, MatFuncOutput, out temp1, out temp2 );

			SoloNoiseList.AddRange( temp1 );
			NoiseList.AddRange( temp2 );


			Sample16Pixels(i + (float)(224 * interval), Offset, out FuncOutput, out MatFuncOutput);

			Format16NoiseVariable( FuncOutput, MatFuncOutput, out temp1, out temp2 );

			SoloNoiseList.AddRange( temp1 );
			NoiseList.AddRange( temp2 );


			Sample16Pixels(i + (float)(240 * interval), Offset, out FuncOutput, out MatFuncOutput);

			Format16NoiseVariable( FuncOutput, MatFuncOutput, out temp1, out temp2 );

			SoloNoiseList.AddRange( temp1 );
			NoiseList.AddRange( temp2 );


		NoiseData = NoiseList.ToArray();
		SoloNoiseData = SoloNoiseList.ToArray();


	} //Samples and Formats 256 pixels and gives values

	void SetSeed()
	{

		LoadData(); //Loads config from file

		//Sets proper resolution from UI input
		if (SetResolution == 0) { Resolution = 32; }
		if (SetResolution == 1) { Resolution = 64; }
		if (SetResolution == 2) { Resolution = 128; }
		if (SetResolution == 3) { Resolution = 256; }
		if (SetResolution == 4) { Resolution = 512; }
		if (SetResolution == 5) { Resolution = 1024; }



		//Noise type 1
		if ( Noise1Type == 0 ) { NoiseField = Noise.PerlinField( new Noise.FractalParameters( Noise1Seed, Noise1Frequency, Noise1Octave, Noise1Gain, Noise1lacunarity ) ); }
		if ( Noise1Type == 1 ) { NoiseField = Noise.SimplexField( new Noise.FractalParameters( Noise1Seed, Noise1Frequency, Noise1Octave, Noise1Gain, Noise1lacunarity) ); }
		if ( Noise1Type == 2 ) { NoiseField = Noise.ValueField( new Noise.FractalParameters( Noise1Seed, Noise1Frequency, Noise1Octave, Noise1Gain, Noise1lacunarity) ); }

		//Noise type 2
		if ( Noise2Type == 0 ) { NoiseField1 = Noise.PerlinField( new Noise.FractalParameters( Noise2Seed, Noise2Frequency, Noise2Octave, Noise2Gain, Noise2lacunarity ) ); }
		if ( Noise2Type == 1 ) { NoiseField1 = Noise.SimplexField( new Noise.FractalParameters( Noise2Seed, Noise2Frequency, Noise2Octave, Noise2Gain, Noise2lacunarity) ); }
		if ( Noise2Type == 2 ) { NoiseField1 = Noise.ValueField( new Noise.FractalParameters( Noise2Seed, Noise2Frequency, Noise2Octave, Noise2Gain, Noise2lacunarity) ); }

		//Noise type 3
		if ( Noise3Type == 0 ) { NoiseField2 = Noise.PerlinField( new Noise.FractalParameters( Noise3Seed, Noise3Frequency, Noise3Octave, Noise3Gain, Noise3lacunarity ) ); }
		if ( Noise3Type == 1 ) { NoiseField2 = Noise.SimplexField( new Noise.FractalParameters( Noise3Seed, Noise3Frequency, Noise3Octave, Noise3Gain, Noise3lacunarity) ); }
		if ( Noise3Type == 2 ) { NoiseField2 = Noise.ValueField( new Noise.FractalParameters( Noise3Seed, Noise3Frequency, Noise3Octave, Noise3Gain, Noise3lacunarity) ); }




	} //Sets base noise values

	public void SearchForPlayerCameras()
	{

		var players = Scene.GetAllObjects( true ).Where( go => go.Tags.Has( "player" ) ); //Gests every GameObject with tag "player"

		foreach ( var player in players )
		{
			if ( player.Components.Get<CameraComponent>() != null ) //Checks if GameObject has camera

			{

				PlayerCameras.Add( player.Components.Get<CameraComponent>() ); //Adds Camera to list
				LastChunkSpacePOS.Add( FindChunkPOSFromWorldSpace( player.Components.Get<CameraComponent>().WorldPosition ) ); //Set LastChunkSpacePOS

			}

		}

		RenderDistance = 2; //Sets RenderDistance

	} //Scans the game for gameobjects with the tag player and grabs the attached camera

	void RegenChunks()
	{

		SetSeed(); //resets config

		FillCache(); //Regens cache with new data

		foreach ( GameObject chunk in SpawnedChunks ) //Updates each chunks data
		{

			Vector2 pos = FindChunkPOSFromWorldSpace(chunk.WorldPosition);

			pos += new Vector2( 1, 1 );

			pos = ChunkSpaceToDataSpace( pos );

			int DataSpaceIndex = FindIntPosForDataSpace( pos );


			Terrain Chunk = chunk.Components.Get<Terrain>();








			Chunk.SyncGPUTexture();

			//Chunk.Storage.SetResolution( Resolution );

			Chunk.HeightMap.Update( HeightMapDataStorage[DataSpaceIndex], 0, 0, Resolution, Resolution );




			Chunk.SyncCPUTexture( new Terrain.SyncFlags(), new RectInt( new Vector2Int( 0, 0 ), new Vector2Int( Resolution, Resolution ) ) );




			Texture2DBuilder temp = new Texture2DBuilder().WithSize( Resolution, Resolution );

			temp.WithData( MaterialMapDataStorage[DataSpaceIndex] );

			temp.WithMips( 3 );


			Texture Final = temp.Finish();


			Material MatCopy = TerrainMat.CreateCopy();

			MatCopy.Set( "NoiseTexture", Final );


			Chunk.MaterialOverride = MatCopy;



			Chunk.ClipMapLodLevels = 6;
			Chunk.ClipMapLodExtentTexels = 256;



			Chunk.UpdateMaterialsBuffer();

		}

	} //Regenerate the chunks data with current set noise data !Breaks ChunkSpawning ingame!

	void FillCache()
	{

		DataStorageSize = ((RenderDistance * 2) + 1 + 4);


		for ( int i = 0; i < (DataStorageSize * DataStorageSize); i++ ) //Loops through each cache position
		{

			List<byte> NoiseDataSoloList = new List<byte>();
			List<byte> NoiseDataList = new List<byte>();

			Vector2 DataSpaceVec2 = FindDataSpaceVector2FromInt( i );

			Vector2 offset = TerrainOffset;
			Vector2 ChunkSpace = DataSpaceToChunkSpace( DataSpaceVec2, offset );


			Vector2 PosToScan = ChunkSpace * SampleResolution;


			for ( int j = 0; j < (Resolution * Resolution); j += 256 ) //Generates Data For Cache
			{

				byte[] SoloNoiseData = new byte[ Resolution * ( 2 ) ];
				byte[] NoiseData = new byte[ Resolution * ( 4 )];

				SampleAndFormat256( j, PosToScan, out NoiseData, out SoloNoiseData );

				NoiseDataList.AddRange( NoiseData );
				NoiseDataSoloList.AddRange( SoloNoiseData );

			}

			HeightMapDataStorage.Insert( i, NoiseDataSoloList.ToArray() );

			MaterialMapDataStorage.Insert(i, NoiseDataList.ToArray() );

		}



	} //Fills the entire cache of terrain data

	void SpawnChunkWithData(Vector2 ChunkPos, byte[] NoiseDataSolo, byte[] NoiseData )
	{

		


		Vector3 POS = ConvertChunkSpaceToWorldSpace( ChunkPos );

		GameObject NewChunk = TerrainObj.Clone( POS );

		NewChunk.Components.Get<Terrain>().Storage.SetResolution( Resolution );

		SpawnedChunks.Add( NewChunk );

		NewChunk.BreakFromPrefab();

		Terrain Chunk = NewChunk.Components.Get<Terrain>();

		
		


		


		Chunk.SyncGPUTexture();

		//Chunk.Storage.SetResolution( Resolution );

		Chunk.HeightMap.Update( NoiseDataSolo, 0, 0, Resolution, Resolution );

		


		Chunk.SyncCPUTexture( new Terrain.SyncFlags(), new RectInt( new Vector2Int( 0, 0 ), new Vector2Int( Resolution, Resolution ) ) );




		Texture2DBuilder temp = new Texture2DBuilder().WithSize( Resolution, Resolution );

		temp.WithData( NoiseData );

		temp.WithMips( 3 );


		Texture Final = temp.Finish();


		Material MatCopy = TerrainMat.CreateCopy();

		MatCopy.Set( "NoiseTexture", Final );


		Chunk.MaterialOverride = MatCopy;



		Chunk.ClipMapLodLevels = 6;
		Chunk.ClipMapLodExtentTexels = 256;



		Chunk.UpdateMaterialsBuffer();




		




	} //Spawns new chunk with data

	void OffsetCache( Vector2 TravelDirection )
	{

		Vector2 StartChunk = 0;

		bool XAxis = false;

		bool IntIncrease = true;


		if( TravelDirection.y == -1 )
		{

			StartChunk = new Vector2( 0, 0 );
			XAxis = true;
			IntIncrease = true;

		}

		if ( TravelDirection.y == 1 )
		{

			StartChunk = new Vector2( 0, DataStorageSize - 1 );
			XAxis = true;
			IntIncrease = false;

		}

		if ( TravelDirection.x == 1 )
		{

			StartChunk = new Vector2( DataStorageSize - 1, 0 );
			XAxis = false;
			IntIncrease = false;

		}

		if ( TravelDirection.x == -1 )
		{ 
		
			StartChunk = new Vector2( 0, 0 );
			XAxis = false;
			IntIncrease = true;

		}


		for ( int i = 0; i < DataStorageSize; i++ )
		{

			

			if ( XAxis )
			{


				if( IntIncrease )
				{

					if ( i != DataStorageSize - 1 )
					{

						Vector2 Pos0CopyTo = StartChunk;
						Vector2 Pos1CopyTo = StartChunk + new Vector2( 1, 0 );
						Vector2 Pos2CopyTo = StartChunk + new Vector2( 2, 0 );
						Vector2 Pos3CopyTo = StartChunk + new Vector2( 3, 0 );
						Vector2 Pos4CopyTo = StartChunk + new Vector2( 4, 0 );
						Vector2 Pos5CopyTo = StartChunk + new Vector2( 5, 0 );
						Vector2 Pos6CopyTo = StartChunk + new Vector2( 6, 0 );
						Vector2 Pos7CopyTo = StartChunk + new Vector2( 7, 0 );
						Vector2 Pos8CopyTo = StartChunk + new Vector2( 8, 0 );

						Vector2 Pos0CopyFrom = StartChunk + new Vector2( 0, 1 );
						Vector2 Pos1CopyFrom = StartChunk + new Vector2( 1, 1 );
						Vector2 Pos2CopyFrom = StartChunk + new Vector2( 2, 1 );
						Vector2 Pos3CopyFrom = StartChunk + new Vector2( 3, 1 );
						Vector2 Pos4CopyFrom = StartChunk + new Vector2( 4, 1 );
						Vector2 Pos5CopyFrom = StartChunk + new Vector2( 5, 1 );
						Vector2 Pos6CopyFrom = StartChunk + new Vector2( 6, 1 );
						Vector2 Pos7CopyFrom = StartChunk + new Vector2( 7, 1 );
						Vector2 Pos8CopyFrom = StartChunk + new Vector2( 8, 1 );

						HeightMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos0CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos1CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos2CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos3CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos4CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos5CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos6CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos7CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos8CopyFrom )];

						MaterialMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos0CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos1CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos2CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos3CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos4CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos5CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos6CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos7CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos8CopyFrom )];


						if( i == 0 )
						{

							if ( VoidDataStorage.Contains( Pos0CopyTo ) ) { VoidDataStorage.Remove( Pos0CopyTo ); }
							if ( VoidDataStorage.Contains( Pos1CopyTo ) ) { VoidDataStorage.Remove( Pos1CopyTo ); }
							if ( VoidDataStorage.Contains( Pos2CopyTo ) ) { VoidDataStorage.Remove( Pos2CopyTo ); }
							if ( VoidDataStorage.Contains( Pos3CopyTo ) ) { VoidDataStorage.Remove( Pos3CopyTo ); }
							if ( VoidDataStorage.Contains( Pos4CopyTo ) ) { VoidDataStorage.Remove( Pos4CopyTo ); }
							if ( VoidDataStorage.Contains( Pos5CopyTo ) ) { VoidDataStorage.Remove( Pos5CopyTo ); }
							if ( VoidDataStorage.Contains( Pos6CopyTo ) ) { VoidDataStorage.Remove( Pos6CopyTo ); }
							if ( VoidDataStorage.Contains( Pos7CopyTo ) ) { VoidDataStorage.Remove( Pos7CopyTo ); }
							if ( VoidDataStorage.Contains( Pos8CopyTo ) ) { VoidDataStorage.Remove( Pos8CopyTo ); }

						}



						StartChunk += new Vector2( 0, 1 );

					}
					else 
					{

						Vector2 Pos0CopyTo = StartChunk;
						Vector2 Pos1CopyTo = StartChunk + new Vector2( 1, 0 );
						Vector2 Pos2CopyTo = StartChunk + new Vector2( 2, 0 );
						Vector2 Pos3CopyTo = StartChunk + new Vector2( 3, 0 );
						Vector2 Pos4CopyTo = StartChunk + new Vector2( 4, 0 );
						Vector2 Pos5CopyTo = StartChunk + new Vector2( 5, 0 );
						Vector2 Pos6CopyTo = StartChunk + new Vector2( 6, 0 );
						Vector2 Pos7CopyTo = StartChunk + new Vector2( 7, 0 );
						Vector2 Pos8CopyTo = StartChunk + new Vector2( 8, 0 );


						HeightMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )] = new byte[(Resolution * Resolution) * 2];

						MaterialMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )] = new byte[(Resolution * Resolution) * 4];

						VoidDataStorage.Add( Pos0CopyTo );
						VoidDataStorage.Add( Pos1CopyTo );
						VoidDataStorage.Add( Pos2CopyTo );
						VoidDataStorage.Add( Pos3CopyTo );
						VoidDataStorage.Add( Pos4CopyTo );
						VoidDataStorage.Add( Pos5CopyTo );
						VoidDataStorage.Add( Pos6CopyTo );
						VoidDataStorage.Add( Pos7CopyTo );
						VoidDataStorage.Add( Pos8CopyTo );

					}

				}
				else
				{

					if ( i != DataStorageSize - 1 )
					{

						Vector2 Pos0CopyTo = StartChunk;
						Vector2 Pos1CopyTo = StartChunk + new Vector2( 1, 0 );
						Vector2 Pos2CopyTo = StartChunk + new Vector2( 2, 0 );
						Vector2 Pos3CopyTo = StartChunk + new Vector2( 3, 0 );
						Vector2 Pos4CopyTo = StartChunk + new Vector2( 4, 0 );
						Vector2 Pos5CopyTo = StartChunk + new Vector2( 5, 0 );
						Vector2 Pos6CopyTo = StartChunk + new Vector2( 6, 0 );
						Vector2 Pos7CopyTo = StartChunk + new Vector2( 7, 0 );
						Vector2 Pos8CopyTo = StartChunk + new Vector2( 8, 0 );

						Vector2 Pos0CopyFrom = StartChunk + new Vector2( 0, -1 );
						Vector2 Pos1CopyFrom = StartChunk + new Vector2( 1, -1 );
						Vector2 Pos2CopyFrom = StartChunk + new Vector2( 2, -1 );
						Vector2 Pos3CopyFrom = StartChunk + new Vector2( 3, -1 );
						Vector2 Pos4CopyFrom = StartChunk + new Vector2( 4, -1 );
						Vector2 Pos5CopyFrom = StartChunk + new Vector2( 5, -1 );
						Vector2 Pos6CopyFrom = StartChunk + new Vector2( 6, -1 );
						Vector2 Pos7CopyFrom = StartChunk + new Vector2( 7, -1 );
						Vector2 Pos8CopyFrom = StartChunk + new Vector2( 8, -1 );


						HeightMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos0CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos1CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos2CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos3CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos4CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos5CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos6CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos7CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos8CopyFrom )];

						MaterialMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos0CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos1CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos2CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos3CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos4CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos5CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos6CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos7CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos8CopyFrom )];


						if ( i == 0 )
						{

							if ( VoidDataStorage.Contains( Pos0CopyTo ) ) { VoidDataStorage.Remove( Pos0CopyTo ); }
							if ( VoidDataStorage.Contains( Pos1CopyTo ) ) { VoidDataStorage.Remove( Pos1CopyTo ); }
							if ( VoidDataStorage.Contains( Pos2CopyTo ) ) { VoidDataStorage.Remove( Pos2CopyTo ); }
							if ( VoidDataStorage.Contains( Pos3CopyTo ) ) { VoidDataStorage.Remove( Pos3CopyTo ); }
							if ( VoidDataStorage.Contains( Pos4CopyTo ) ) { VoidDataStorage.Remove( Pos4CopyTo ); }
							if ( VoidDataStorage.Contains( Pos5CopyTo ) ) { VoidDataStorage.Remove( Pos5CopyTo ); }
							if ( VoidDataStorage.Contains( Pos6CopyTo ) ) { VoidDataStorage.Remove( Pos6CopyTo ); }
							if ( VoidDataStorage.Contains( Pos7CopyTo ) ) { VoidDataStorage.Remove( Pos7CopyTo ); }
							if ( VoidDataStorage.Contains( Pos8CopyTo ) ) { VoidDataStorage.Remove( Pos8CopyTo ); }

						}


						StartChunk -= new Vector2( 0, 1 );


					}
					else
					{

						Vector2 Pos0CopyTo = StartChunk;
						Vector2 Pos1CopyTo = StartChunk + new Vector2( 1, 0 );
						Vector2 Pos2CopyTo = StartChunk + new Vector2( 2, 0 );
						Vector2 Pos3CopyTo = StartChunk + new Vector2( 3, 0 );
						Vector2 Pos4CopyTo = StartChunk + new Vector2( 4, 0 );
						Vector2 Pos5CopyTo = StartChunk + new Vector2( 5, 0 );
						Vector2 Pos6CopyTo = StartChunk + new Vector2( 6, 0 );
						Vector2 Pos7CopyTo = StartChunk + new Vector2( 7, 0 );
						Vector2 Pos8CopyTo = StartChunk + new Vector2( 8, 0 );


						HeightMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )] = new byte[(Resolution * Resolution) * 2];

						MaterialMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )] = new byte[(Resolution * Resolution) * 4];

						VoidDataStorage.Add( Pos0CopyTo );
						VoidDataStorage.Add( Pos1CopyTo );
						VoidDataStorage.Add( Pos2CopyTo );
						VoidDataStorage.Add( Pos3CopyTo );
						VoidDataStorage.Add( Pos4CopyTo );
						VoidDataStorage.Add( Pos5CopyTo );
						VoidDataStorage.Add( Pos6CopyTo );
						VoidDataStorage.Add( Pos7CopyTo );
						VoidDataStorage.Add( Pos8CopyTo );

					}

				}


			}


			if ( !XAxis )
			{


				if ( IntIncrease )
				{

					if ( i != DataStorageSize - 1 )
					{

						

						Vector2 Pos0CopyTo = StartChunk;
						Vector2 Pos1CopyTo = StartChunk + new Vector2( 0, 1 );
						Vector2 Pos2CopyTo = StartChunk + new Vector2( 0, 2 );
						Vector2 Pos3CopyTo = StartChunk + new Vector2( 0, 3 );
						Vector2 Pos4CopyTo = StartChunk + new Vector2( 0, 4 );
						Vector2 Pos5CopyTo = StartChunk + new Vector2( 0, 5 );
						Vector2 Pos6CopyTo = StartChunk + new Vector2( 0, 6 );
						Vector2 Pos7CopyTo = StartChunk + new Vector2( 0, 7 );
						Vector2 Pos8CopyTo = StartChunk + new Vector2( 0, 8 );

						Vector2 Pos0CopyFrom = StartChunk + new Vector2( 1, 0 );
						Vector2 Pos1CopyFrom = StartChunk + new Vector2( 1, 1 );
						Vector2 Pos2CopyFrom = StartChunk + new Vector2( 1, 2 );
						Vector2 Pos3CopyFrom = StartChunk + new Vector2( 1, 3 );
						Vector2 Pos4CopyFrom = StartChunk + new Vector2( 1, 4 );
						Vector2 Pos5CopyFrom = StartChunk + new Vector2( 1, 5 );
						Vector2 Pos6CopyFrom = StartChunk + new Vector2( 1, 6 );
						Vector2 Pos7CopyFrom = StartChunk + new Vector2( 1, 7 );
						Vector2 Pos8CopyFrom = StartChunk + new Vector2( 1, 8 );

						HeightMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos0CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos1CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos2CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos3CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos4CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos5CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos6CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos7CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos8CopyFrom )];

						MaterialMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos0CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos1CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos2CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos3CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos4CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos5CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos6CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos7CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos8CopyFrom )];


						if ( i == 0 )
						{

							if ( VoidDataStorage.Contains( Pos0CopyTo ) ) { VoidDataStorage.Remove( Pos0CopyTo ); }
							if ( VoidDataStorage.Contains( Pos1CopyTo ) ) { VoidDataStorage.Remove( Pos1CopyTo ); }
							if ( VoidDataStorage.Contains( Pos2CopyTo ) ) { VoidDataStorage.Remove( Pos2CopyTo ); }
							if ( VoidDataStorage.Contains( Pos3CopyTo ) ) { VoidDataStorage.Remove( Pos3CopyTo ); }
							if ( VoidDataStorage.Contains( Pos4CopyTo ) ) { VoidDataStorage.Remove( Pos4CopyTo ); }
							if ( VoidDataStorage.Contains( Pos5CopyTo ) ) { VoidDataStorage.Remove( Pos5CopyTo ); }
							if ( VoidDataStorage.Contains( Pos6CopyTo ) ) { VoidDataStorage.Remove( Pos6CopyTo ); }
							if ( VoidDataStorage.Contains( Pos7CopyTo ) ) { VoidDataStorage.Remove( Pos7CopyTo ); }
							if ( VoidDataStorage.Contains( Pos8CopyTo ) ) { VoidDataStorage.Remove( Pos8CopyTo ); }

						}


						StartChunk += new Vector2( 1, 0 );


					}
					else
					{

						

						Vector2 Pos0CopyTo = StartChunk;
						Vector2 Pos1CopyTo = StartChunk + new Vector2( 0, 1 );
						Vector2 Pos2CopyTo = StartChunk + new Vector2( 0, 2 );
						Vector2 Pos3CopyTo = StartChunk + new Vector2( 0, 3 );
						Vector2 Pos4CopyTo = StartChunk + new Vector2( 0, 4 );
						Vector2 Pos5CopyTo = StartChunk + new Vector2( 0, 5 );
						Vector2 Pos6CopyTo = StartChunk + new Vector2( 0, 6 );
						Vector2 Pos7CopyTo = StartChunk + new Vector2( 0, 7 );
						Vector2 Pos8CopyTo = StartChunk + new Vector2( 0, 8 );


						HeightMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )] = new byte[(Resolution * Resolution) * 2];

						MaterialMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )] = new byte[(Resolution * Resolution) * 4];

						VoidDataStorage.Add( Pos0CopyTo );
						VoidDataStorage.Add( Pos1CopyTo );
						VoidDataStorage.Add( Pos2CopyTo );
						VoidDataStorage.Add( Pos3CopyTo );
						VoidDataStorage.Add( Pos4CopyTo );
						VoidDataStorage.Add( Pos5CopyTo );
						VoidDataStorage.Add( Pos6CopyTo );
						VoidDataStorage.Add( Pos7CopyTo );
						VoidDataStorage.Add( Pos8CopyTo );

					}

				}
				else
				{

					if ( i != DataStorageSize - 1 )
					{

						

						Vector2 Pos0CopyTo = StartChunk;
						Vector2 Pos1CopyTo = StartChunk + new Vector2( 0, 1 );
						Vector2 Pos2CopyTo = StartChunk + new Vector2( 0, 2 );
						Vector2 Pos3CopyTo = StartChunk + new Vector2( 0, 3 );
						Vector2 Pos4CopyTo = StartChunk + new Vector2( 0, 4 );
						Vector2 Pos5CopyTo = StartChunk + new Vector2( 0, 5 );
						Vector2 Pos6CopyTo = StartChunk + new Vector2( 0, 6 );
						Vector2 Pos7CopyTo = StartChunk + new Vector2( 0, 7 );
						Vector2 Pos8CopyTo = StartChunk + new Vector2( 0, 8 );

						Vector2 Pos0CopyFrom = StartChunk + new Vector2( -1, 0 );
						Vector2 Pos1CopyFrom = StartChunk + new Vector2( -1, 1 );
						Vector2 Pos2CopyFrom = StartChunk + new Vector2( -1, 2 );
						Vector2 Pos3CopyFrom = StartChunk + new Vector2( -1, 3 );
						Vector2 Pos4CopyFrom = StartChunk + new Vector2( -1, 4 );
						Vector2 Pos5CopyFrom = StartChunk + new Vector2( -1, 5 );
						Vector2 Pos6CopyFrom = StartChunk + new Vector2( -1, 6 );
						Vector2 Pos7CopyFrom = StartChunk + new Vector2( -1, 7 );
						Vector2 Pos8CopyFrom = StartChunk + new Vector2( -1, 8 );

						HeightMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos0CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos1CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos2CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos3CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos4CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos5CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos6CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos7CopyFrom )];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )] = HeightMapDataStorage[FindIntPosForDataSpace( Pos8CopyFrom )];

						MaterialMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos0CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos1CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos2CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos3CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos4CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos5CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos6CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos7CopyFrom )];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )] = MaterialMapDataStorage[FindIntPosForDataSpace( Pos8CopyFrom )];


						if ( i == 0 )
						{

							if ( VoidDataStorage.Contains( Pos0CopyTo ) ) { VoidDataStorage.Remove( Pos0CopyTo ); }
							if ( VoidDataStorage.Contains( Pos1CopyTo ) ) { VoidDataStorage.Remove( Pos1CopyTo ); }
							if ( VoidDataStorage.Contains( Pos2CopyTo ) ) { VoidDataStorage.Remove( Pos2CopyTo ); }
							if ( VoidDataStorage.Contains( Pos3CopyTo ) ) { VoidDataStorage.Remove( Pos3CopyTo ); }
							if ( VoidDataStorage.Contains( Pos4CopyTo ) ) { VoidDataStorage.Remove( Pos4CopyTo ); }
							if ( VoidDataStorage.Contains( Pos5CopyTo ) ) { VoidDataStorage.Remove( Pos5CopyTo ); }
							if ( VoidDataStorage.Contains( Pos6CopyTo ) ) { VoidDataStorage.Remove( Pos6CopyTo ); }
							if ( VoidDataStorage.Contains( Pos7CopyTo ) ) { VoidDataStorage.Remove( Pos7CopyTo ); }
							if ( VoidDataStorage.Contains( Pos8CopyTo ) ) { VoidDataStorage.Remove( Pos8CopyTo ); }

						}


						StartChunk -= new Vector2( 1, 0 );


					}
					else
					{

						

						Vector2 Pos0CopyTo = StartChunk;
						Vector2 Pos1CopyTo = StartChunk + new Vector2( 0, 1 );
						Vector2 Pos2CopyTo = StartChunk + new Vector2( 0, 2 );
						Vector2 Pos3CopyTo = StartChunk + new Vector2( 0, 3 );
						Vector2 Pos4CopyTo = StartChunk + new Vector2( 0, 4 );
						Vector2 Pos5CopyTo = StartChunk + new Vector2( 0, 5 );
						Vector2 Pos6CopyTo = StartChunk + new Vector2( 0, 6 );
						Vector2 Pos7CopyTo = StartChunk + new Vector2( 0, 7 );
						Vector2 Pos8CopyTo = StartChunk + new Vector2( 0, 8 );


						HeightMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )] = new byte[(Resolution * Resolution) * 2];
						HeightMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )] = new byte[(Resolution * Resolution) * 2];

						MaterialMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )] = new byte[(Resolution * Resolution) * 4];
						MaterialMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )] = new byte[(Resolution * Resolution) * 4];

						VoidDataStorage.Add( Pos0CopyTo );
						VoidDataStorage.Add( Pos1CopyTo );
						VoidDataStorage.Add( Pos2CopyTo );
						VoidDataStorage.Add( Pos3CopyTo );
						VoidDataStorage.Add( Pos4CopyTo );
						VoidDataStorage.Add( Pos5CopyTo );
						VoidDataStorage.Add( Pos6CopyTo );
						VoidDataStorage.Add( Pos7CopyTo );
						VoidDataStorage.Add( Pos8CopyTo );

					}

				}


			}

			

		}


		for( int i = 0; i < VoidDataStorage.Count; i++ )
		{

			Vector2 NewVal = new Vector2( VoidDataStorage[i].x, VoidDataStorage[i].y ) + TravelDirection;


			VoidDataStorage[i] = new Vector4(NewVal.x, NewVal.y, 0, 0);

		}


	} //Offsets entires cache data by one chunk in direction

	void LoadChunksFromCache( Vector2 OldChunkPos, Vector2 NewChunkPos, int PlayerIndex)
	{

		Vector2 TravelDirection = OldChunkPos - NewChunkPos;

		

		Vector2 MidPoint = new Vector2( 5, 5 );

		Vector2 RowToSpawn = NewChunkPos - (TravelDirection * RenderDistance);
		Vector2 RowToCache = OldChunkPos + (TravelDirection * RenderDistance);

		Vector2 RowToData = (-TravelDirection * (RenderDistance + 1));
		Vector2 RowToDataCache = (TravelDirection * (RenderDistance + 1));


		bool XAxis = TravelDirection.x == 0;

		int AmountInRow = (RenderDistance * 2) + 1;


		for ( int i = 0; i < AmountInRow; i++ )
		{

			Vector2 PosToSpawn = 0;
			Vector2 PosToCache = 0;
			
			Vector2 PosToData = 0;

			if ( XAxis )
			{

				PosToSpawn = new Vector2( (-RenderDistance + i), 0 ) + RowToSpawn;
				PosToCache = new Vector2( (-RenderDistance + i), 0 ) + RowToCache;
				
				PosToData = new Vector2( (-RenderDistance + i), 0 ) + RowToData;


			}else
			{

				PosToSpawn = new Vector2( 0, (-RenderDistance + i) ) + RowToSpawn;
				PosToCache = new Vector2( 0, (-RenderDistance + i) ) + RowToCache;

				PosToData = new Vector2( 0, (-RenderDistance + i) ) + RowToData;


			}


			int DataIndex = FindIntPosForDataSpace( ChunkSpaceToDataSpace( PosToData ));

			GameObject ChunkToMove = CheckIfChunkExists( PosToCache );

			ChunkToMove.WorldPosition = ConvertChunkSpaceToWorldSpace( PosToSpawn );

			Terrain Chunk = ChunkToMove.Components.Get<Terrain>();








			Chunk.SyncGPUTexture();

			//Chunk.Storage.SetResolution( Resolution );

			Chunk.HeightMap.Update( HeightMapDataStorage[DataIndex], 0, 0, Resolution, Resolution );




			Chunk.SyncCPUTexture( new Terrain.SyncFlags(), new RectInt( new Vector2Int( 0, 0 ), new Vector2Int( Resolution, Resolution ) ) );




			Texture2DBuilder temp = new Texture2DBuilder().WithSize( Resolution, Resolution );

			temp.WithData( MaterialMapDataStorage[DataIndex] );

			temp.WithMips( 3 );


			Texture Final = temp.Finish();


			Material MatCopy = TerrainMat.CreateCopy();

			MatCopy.Set( "NoiseTexture", Final );


			Chunk.MaterialOverride = MatCopy;



			Chunk.ClipMapLodLevels = 6;
			Chunk.ClipMapLodExtentTexels = 256;



			Chunk.UpdateMaterialsBuffer();


		}

		OffsetCache( TravelDirection );


	} //Moves Chunks from row exiting renderdistance to upcoming row and updates terrain data

	void SetChunkSetting()
	{

		SpawnChunkWithData( new Vector2(0 ,0), HeightMapDataStorage[0], MaterialMapDataStorage[0] );

		SpawnedChunks[0].DestroyImmediate();
		SpawnedChunks.Clear();

	} //Fixes bug by presetting chunk setting and removing the temp chunk

	void StartGameSpawnChunksFromData()
	{

		FillCache();

		SetChunkSetting();
		


		for ( int i = 0; i < ((RenderDistance + RenderDistance + 1) * (RenderDistance + RenderDistance + 1)); i++)
		{


			Vector2 PosToSpawn = IntToChunkSpace( i );

			Vector2 DataSpaceVec = ChunkSpaceToDataSpace( PosToSpawn );

			int DataSpaceIndex = FindIntPosForDataSpace( DataSpaceVec );

			SpawnChunkWithData( PosToSpawn, HeightMapDataStorage[DataSpaceIndex], MaterialMapDataStorage[DataSpaceIndex] );

		}



	} //Spawns 5x5 chunks around origin ( 0 x 0 ) with data

	void DebugData()
	{

		Log.Info( "_________" );

		for( int i = 0; i < DataStorageSize; i++ )
		{

			Vector2 StartChunk = new Vector2( 0, i );

			Vector2 Pos0CopyTo = StartChunk;
			Vector2 Pos1CopyTo = StartChunk + new Vector2( 1, 0 );
			Vector2 Pos2CopyTo = StartChunk + new Vector2( 2, 0 );
			Vector2 Pos3CopyTo = StartChunk + new Vector2( 3, 0 );
			Vector2 Pos4CopyTo = StartChunk + new Vector2( 4, 0 );
			Vector2 Pos5CopyTo = StartChunk + new Vector2( 5, 0 );
			Vector2 Pos6CopyTo = StartChunk + new Vector2( 6, 0 );
			Vector2 Pos7CopyTo = StartChunk + new Vector2( 7, 0 );
			Vector2 Pos8CopyTo = StartChunk + new Vector2( 8, 0 );

			int value0 = HeightMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )][0] + HeightMapDataStorage[FindIntPosForDataSpace( Pos0CopyTo )][1];
			int value1 = HeightMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )][0] + HeightMapDataStorage[FindIntPosForDataSpace( Pos1CopyTo )][1];
			int value2 = HeightMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )][0] + HeightMapDataStorage[FindIntPosForDataSpace( Pos2CopyTo )][1];
			int value3 = HeightMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )][0] + HeightMapDataStorage[FindIntPosForDataSpace( Pos3CopyTo )][1];
			int value4 = HeightMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )][0] + HeightMapDataStorage[FindIntPosForDataSpace( Pos4CopyTo )][1];
			int value5 = HeightMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )][0] + HeightMapDataStorage[FindIntPosForDataSpace( Pos5CopyTo )][1];
			int value6 = HeightMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )][0] + HeightMapDataStorage[FindIntPosForDataSpace( Pos6CopyTo )][1];
			int value7 = HeightMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )][0] + HeightMapDataStorage[FindIntPosForDataSpace( Pos7CopyTo )][1];
			int value8 = HeightMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )][0] + HeightMapDataStorage[FindIntPosForDataSpace( Pos8CopyTo )][1];

			Log.Info( value0.ToString() + "_" + value1.ToString() + "_" + value2.ToString() + "_" + value3.ToString() + "_" + value4.ToString() + "_" + value5.ToString() + "_" + value6.ToString() + "_" + value7.ToString() + "_" + value8.ToString());

		}

		Log.Info( "_________" );

	} //Used to log cache data

	void CheckPlayerDistanceToVoidChunk(int PlayerIndex)
	{

		
		

		List<Vector4> IndexToCull = new List<Vector4>();

		for( int i = 0; i < VoidDataStorage.Count; i++ )
		{

			Vector2 PlayerPOS = PlayerCameras[PlayerIndex].WorldPosition;

			Vector2 PosA = 0;

			if ( VoidDataStorage[i].z == 0 && VoidDataStorage[i].w == 0 )
			{


				Vector2 StorageVal = new Vector2( VoidDataStorage[i].x, VoidDataStorage[i].y );

				Vector2 tempPOS = ConvertChunkSpaceToWorldSpace( DataSpaceToChunkSpace( StorageVal, new Vector2( 1, 1 ) ) );

				Vector2 temp = FindChunkPOSFromWorldSpace( PlayerPOS );

				Vector2 MidPoint = CheckIfChunkExists( temp ).WorldPosition;

				tempPOS += MidPoint;

				PosA = tempPOS;

				VoidDataStorage[i] = new Vector4( VoidDataStorage[i].x, VoidDataStorage[i].y, tempPOS.x, tempPOS.y );

			}else { PosA = new Vector2( VoidDataStorage[i].z, VoidDataStorage[i].w ); }




			float Distance = Vector2.Distance( PlayerPOS, PosA );


			

			
			if ( Distance < 70000 )
			{

				int temp = (int)VoidDataStorage[i].x;

				temp = (8 - temp); 

				Vector2 Storage = new Vector2( VoidDataStorage[i].x, VoidDataStorage[i].y );

				Vector2 PlayerPositon = PlayerCameras[PlayerIndex].WorldPosition;
				Vector2 ChunkSpace = DataSpaceToChunkSpace( new Vector2( VoidDataStorage[i].y, VoidDataStorage[i].x ), TerrainOffset );
				PlayerPositon = FindChunkPOSFromWorldSpace( PlayerPositon );


				Vector2 DataSpaceChunkToLoad = ChunkSpace + new Vector2( PlayerPositon.y, PlayerPositon.x );

				int DataIndex = FindIntPosForDataSpace( Storage );

				List<byte> NoiseDataSoloList = new List<byte>();
				List<byte> NoiseDataList = new List<byte>();

				DataSpaceChunkToLoad = new Vector2(DataSpaceChunkToLoad.y, DataSpaceChunkToLoad.x);

				Vector2 PosToScan = DataSpaceChunkToLoad * SampleResolution;

				for ( int j = 0; j < (Resolution * Resolution); j += 256 )
				{

					byte[] SoloNoiseData = new byte[Resolution * (2)];
					byte[] NoiseData = new byte[Resolution * (4)];


					SampleAndFormat256( j, PosToScan, out NoiseData, out SoloNoiseData );

					NoiseDataList.AddRange( NoiseData );
					NoiseDataSoloList.AddRange( SoloNoiseData );

				}
				

				HeightMapDataStorage[DataIndex] = NoiseDataSoloList.ToArray();

				MaterialMapDataStorage[DataIndex] = NoiseDataList.ToArray();


				

				IndexToCull.Add( VoidDataStorage[i] );

			}
			if ( Distance > 35355f ) { IndexToCull.Add( VoidDataStorage[i] ); }

		}

		for( int j = 0; j < IndexToCull.Count(); j++ )
		{

			VoidDataStorage.Remove( IndexToCull[j] );

		}


		


	} //Checks if cache data needs to be generated

	void ExportHeightMap()
	{


		List<byte> NoiseDataSoloList = new List<byte>();
		List<byte> NoiseDataList = new List<byte>();

		Vector2 PosToScan = new Vector2( -(( MathF.Floor((ExportHeightMapSize / 2) + 1) )), -((MathF.Floor((ExportHeightMapSize / 2) + 1))));

		float interval = ((float)SampleResolution / (float)Resolution);

		PosToScan = TerrainOffset + PosToScan;

		PosToScan = new Vector2( PosToScan.y, PosToScan.x );

		PosToScan *= SampleResolution;


		for ( float i = 0; i < (((ExportHeightMapSize * Resolution) * (interval / 2)) * (ExportHeightMapSize * Resolution) * (interval / 2) ); )
		{

			byte[] SoloNoiseData = new byte[16 * 2];
			byte[] NoiseData = new byte[16 * 2];
			byte[] tempNoiseData = new byte[16];
			byte[] tempSoloNoiseData = new byte[16];

			Sample16PixelsForExport( i, PosToScan, out tempSoloNoiseData, out tempNoiseData );


			SoloNoiseData[0] = tempSoloNoiseData[0];
			SoloNoiseData[1] = tempSoloNoiseData[0];
			SoloNoiseData[2] = tempSoloNoiseData[1];
			SoloNoiseData[3] = tempSoloNoiseData[1];
			SoloNoiseData[4] = tempSoloNoiseData[2];
			SoloNoiseData[5] = tempSoloNoiseData[2];
			SoloNoiseData[6] = tempSoloNoiseData[3];
			SoloNoiseData[7] = tempSoloNoiseData[3];
			SoloNoiseData[8] = tempSoloNoiseData[4];
			SoloNoiseData[9] = tempSoloNoiseData[4];
			SoloNoiseData[10] = tempSoloNoiseData[5];
			SoloNoiseData[11] = tempSoloNoiseData[5];
			SoloNoiseData[12] = tempSoloNoiseData[6];
			SoloNoiseData[13] = tempSoloNoiseData[6];
			SoloNoiseData[14] = tempSoloNoiseData[7];
			SoloNoiseData[15] = tempSoloNoiseData[7];
			SoloNoiseData[16] = tempSoloNoiseData[8];
			SoloNoiseData[17] = tempSoloNoiseData[8];
			SoloNoiseData[18] = tempSoloNoiseData[9];
			SoloNoiseData[19] = tempSoloNoiseData[9];
			SoloNoiseData[20] = tempSoloNoiseData[10];
			SoloNoiseData[21] = tempSoloNoiseData[10];
			SoloNoiseData[22] = tempSoloNoiseData[11];
			SoloNoiseData[23] = tempSoloNoiseData[11];
			SoloNoiseData[24] = tempSoloNoiseData[12];
			SoloNoiseData[25] = tempSoloNoiseData[12];
			SoloNoiseData[26] = tempSoloNoiseData[13];
			SoloNoiseData[27] = tempSoloNoiseData[13];
			SoloNoiseData[28] = tempSoloNoiseData[14];
			SoloNoiseData[29] = tempSoloNoiseData[14];
			SoloNoiseData[30] = tempSoloNoiseData[15];
			SoloNoiseData[31] = tempSoloNoiseData[15];


			NoiseData[0] = tempNoiseData[0];
			NoiseData[1] = tempNoiseData[0];
			NoiseData[2] = tempNoiseData[1];
			NoiseData[3] = tempNoiseData[1];
			NoiseData[4] = tempNoiseData[2];
			NoiseData[5] = tempNoiseData[2];
			NoiseData[6] = tempNoiseData[3];
			NoiseData[7] = tempNoiseData[3];
			NoiseData[8] = tempNoiseData[4];
			NoiseData[9] = tempNoiseData[4];
			NoiseData[10] = tempNoiseData[5];
			NoiseData[11] = tempNoiseData[5];
			NoiseData[12] = tempNoiseData[6];
			NoiseData[13] = tempNoiseData[6];
			NoiseData[14] = tempNoiseData[7];
			NoiseData[15] = tempNoiseData[7];
			NoiseData[16] = tempNoiseData[8];
			NoiseData[17] = tempNoiseData[8];
			NoiseData[18] = tempNoiseData[9];
			NoiseData[19] = tempNoiseData[9];
			NoiseData[20] = tempNoiseData[10];
			NoiseData[21] = tempNoiseData[10];
			NoiseData[22] = tempNoiseData[11];
			NoiseData[23] = tempNoiseData[11];
			NoiseData[24] = tempNoiseData[12];
			NoiseData[25] = tempNoiseData[12];
			NoiseData[26] = tempNoiseData[13];
			NoiseData[27] = tempNoiseData[13];
			NoiseData[28] = tempNoiseData[14];
			NoiseData[29] = tempNoiseData[14];
			NoiseData[30] = tempNoiseData[15];
			NoiseData[31] = tempNoiseData[15];


			NoiseDataList.AddRange( NoiseData );
			NoiseDataSoloList.AddRange( SoloNoiseData );


			i += (float)(interval * 16f);

			

		}



		

		string HeightFileName = ExportHeightMapName;

		HeightFileName = HeightFileName + " HeightMap.raw";

		BaseFileSystem baseFileSystem = FileSystem.Data;

		System.IO.Stream stream = baseFileSystem.OpenWrite( HeightFileName, FileMode.Create );

		stream.Write( NoiseDataSoloList.ToArray(), 0, NoiseDataSoloList.Count() );

		stream.Close();

		Log.Info( "Written: " + HeightFileName + " Resolution = " + ((Resolution * ExportHeightMapSize)).ToString() + " X " + ((Resolution * ExportHeightMapSize)).ToString() + "  DIR:" + FileSystem.Data.GetFullPath(HeightFileName) );


		

		string TextureFileName = ExportHeightMapName + " TextureMap.raw";

		stream = baseFileSystem.OpenWrite( TextureFileName, FileMode.Create );

		stream.Write( NoiseDataList.ToArray(), 0, NoiseDataList.Count() );

		stream.Close();

		Log.Info( "Written: " + TextureFileName + " Resolution = " + (( Resolution * ExportHeightMapSize )).ToString() + " X " + ((Resolution * ExportHeightMapSize)).ToString() + "  DIR:" + FileSystem.Data.GetFullPath(TextureFileName));


		ExportAsHeightMapNow = false;

	} //Exports the terrain as a .raw

	protected override void OnStart()
	{

		LocalFile = GameObject.Components.Get<LocalFile>();

		SearchForPlayerCameras();
		
		SetSeed();

		

		StartGameSpawnChunksFromData();




	}

	protected override void OnUpdate()
	{


		if ( ConfigureMode ) 
		{
			if (RegenNoise) { RegenChunks(); RegenNoise = false; }
			if ( ExportToConfigFileNow ) { WriteToFile(); } 
			if ( ExportAsHeightMapNow ) { ExportHeightMap(); }
			if ( ExportAsHeightMapOptions ) { ExportToConfigFileOptions = false; }
			if ( ExportToConfigFileOptions ) { ExportAsHeightMapOptions = false; }
		}


		
		


		for ( int i = 0; i < PlayerCameras.Count; i++ )
		{

			Vector2 temp = FindChunkPOSFromWorldSpace( PlayerCameras[i].WorldPosition );

			if ( LastChunkSpacePOS[i] != temp )
			{

				LoadChunksFromCache( LastChunkSpacePOS[i], temp, i );

				LastChunkSpacePOS[i] = temp;

			}

			

		}

		if ( FramesCount > 30 ) { CheckPlayerDistanceToVoidChunk( 0 ); FramesCount = 0; }

		FramesCount++;

	}

	protected override void OnDestroy()
	{


		VoidDataStorage.Clear();
		SpawnedChunks.Clear();
		PlayerCameras.Clear();
		LastChunkSpacePOS.Clear();
		MaterialMapDataStorage.Clear();
		HeightMapDataStorage.Clear();
		NoiseField = null;
		NoiseField1 = null;
		NoiseField2 = null;

	}

}
