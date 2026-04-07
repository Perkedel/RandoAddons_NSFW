using System;
using System.Collections.Generic;
using System.IO;

// TODO: KeyValuePair and Dictionary BLOW THE ENTIRE THING!!!
namespace Sandbox
{
	public interface ISavable
	{
		abstract void Write( BinaryWriter writer );
		abstract void Read( BinaryReader reader );
	}

	public static class BinaryExtensions
	{
		/// <summary>
		/// Writes a chunk prefixed by a header string
		/// </summary>
		public static void WriteFormattedChunk(this BinaryWriter writer, string header, Action innerFunction)
		{
			writer.Write( header.ToCharArray() );
			innerFunction( );
		}

		/// <summary>
		/// Reads a chunks prefixed by a header string, returns true if the header matches the chunk prefix, false if it doesn't, or the chunk is too small
		/// </summary>
		public static bool ReadFormattedChunk(this BinaryReader reader, string header, Action innerFunction)
		{
			try
			{
				string canidate = new string( reader.ReadChars( header.Length ) );
				if ( canidate == header )
				{
					try
					{
						innerFunction();
					} catch(Exception ex)
					{
						Log.Error( ex );
						return false;
					}
					return true;
				} else
				{
					Log.Error( "Incorrect header! (Expected \"" + header + "\", got \"" + canidate + "\")" );
					return false;
				}
			}
			catch
			{
				Log.Error( "Stream not big enough to contain expected header (needs at least " + header.Length + " bytes, got " + reader.BaseStream.Length + " bytes)" );
				return false;
			}
		}

		/// <summary>
		/// Writes a chunk prefixed by a version number
		/// </summary>
		public static void WriteVersionedChunk(this BinaryWriter writer, int version, Action innerFunction )
		{
			writer.Write( version );
			innerFunction( );
		}

		/// <summary>
		/// Enumerates over <see cref="List{T}"/>, allowing you to write to the binary stream (prefixed with the number of elements in the list)
		/// </summary>
		public static void WriteList<T>(this BinaryWriter writer, List<T> array, Action<T,BinaryWriter> ArrayAction ) where T : new()
		{
			writer.Write( array.Count );
			foreach(var obj in array)
			{
				ArrayAction( obj, writer );
			}
		}

		/// <summary>
		/// Enumerates over <see cref="List{T}"/>, (prefixed with the number of elements in the list)
		/// </summary>
		public static void WriteList<T>( this BinaryWriter writer, List<T> array ) where T : ISavable
		{
			writer.Write( array.Count );
			foreach ( var obj in array )
			{
				obj.Write( writer );
			}
		}

		/// <summary>
		/// Writes a <see cref="Vector2"/> to the binary stream
		/// </summary>
		public static void WriteVector2(this BinaryWriter writer, Vector2 vector)
		{
			writer.Write( vector.x );
			writer.Write( vector.y );
		}

		/// <summary>
		/// Writes a <see cref="Vector2Int"/> to the binary stream
		/// </summary>
		public static void WriteVector2Int( this BinaryWriter writer, Vector2Int vector )
		{
			writer.Write( vector.x );
			writer.Write( vector.y );
		}

		/// <summary>
		/// Writes a <see cref="Vector3"/> to the binary stream
		/// </summary>
		public static void WriteVector3( this BinaryWriter writer, Vector3 vector )
		{
			writer.Write( vector.x );
			writer.Write( vector.y );
			writer.Write( vector.z );
		}

		/// <summary>
		/// Writes a <see cref="Vector3Int"/> to the binary stream
		/// </summary>
		public static void WriteVector3Int( this BinaryWriter writer, Vector3Int vector )
		{
			writer.Write( vector.x );
			writer.Write( vector.y );
			writer.Write( vector.z );
		}

		/// <summary>
		/// Writes a <see cref="Vector4"/> to the binary stream
		/// </summary>
		public static void WriteVector4( this BinaryWriter writer, Vector4 vector )
		{
			writer.Write( vector.x );
			writer.Write( vector.y );
			writer.Write( vector.z );
			writer.Write( vector.w );
		}

		/// <summary>
		/// Writes a <see cref="Rotation"/> to the binary stream
		/// </summary>
		public static void WriteRotation( this BinaryWriter writer, Rotation rotation )
		{
			writer.Write( rotation.x );
			writer.Write( rotation.y );
			writer.Write( rotation.z );
			writer.Write( rotation.w );
		}

		/// <summary>
		/// Writes an <see cref="Angles"/> to the binary stream
		/// </summary>
		public static void WriteAngles( this BinaryWriter writer, Angles rotation )
		{
			writer.Write( rotation.pitch );
			writer.Write( rotation.yaw );
			writer.Write( rotation.roll );
		}

		/// <summary>
		/// Writes an <see cref="object"> of a specified type to the binary stream
		/// </summary>
		public static void WriteObject<T>( this BinaryWriter writer, T target )
		{
			BinaryClass.WriteObject( writer, target, TypeLibrary.GetType<T>() );
		}

		/// <summary>
		/// Reads a chunk prefixed by a version number, returns true if the version can be read, false if it can't
		/// </summary>
		public static bool ReadVersionedChunk(this BinaryReader reader, Dictionary<int, Action<int, BinaryReader>> VersionSwitch)
		{
			int version = reader.ReadInt32();
			if( VersionSwitch.ContainsKey(version) )
			{
				try
				{
					VersionSwitch[version](version, reader );
				}
				catch ( Exception ex )
				{
					Log.Error( ex );
					return false;
				}
				return true;
			} else
			{
				Log.Error( "Unknown chunk version: " + version );
				return false;
			}
		}

		/// <summary>
		/// Reads a chunk using a predetermined version, returns true if the version can be read, false if it can't
		/// </summary>
		public static bool ReadVersionedChunk( this BinaryReader reader, int version, Dictionary<int, Action<int, BinaryReader>> VersionSwitch )
		{
			if ( VersionSwitch.ContainsKey( version ) )
			{
				try
				{
					VersionSwitch[version]( version, reader );
				}
				catch ( Exception ex )
				{
					Log.Error( ex );
					return false;
				}
				return true;
			}
			else
			{
				Log.Error( "Unknown chunk version: " + version );
				return false;
			}
		}

		/// <summary>
		/// Writes a chunk prefixed with the writers position after the chunk
		/// </summary>
		public static void WriteSafeBlock( this BinaryWriter bw, Action innerFunction )
		{
			var PrePos = bw.BaseStream.Position;
			bw.Write( (long)0 );
			innerFunction.Invoke();
			var PostPos = bw.BaseStream.Position;
			bw.BaseStream.Position = PrePos;
			bw.Write( PostPos );
			bw.BaseStream.Position = PostPos;
		}

		/// <summary>
		/// Attempts to read a chunk, and jumps to the end of the chunk after reading
		/// </summary>
		public static void ReadSafeBlock( this BinaryReader br, Action innerFunction )
		{
			long postPos = br.ReadInt64();
			try
			{
				innerFunction.Invoke();
			}
			catch ( Exception ex )
			{
				Log.Error( "Reading error:\r\n" + ex.ToString() + "\r\nCorrecting stream position..." );
			}
			br.BaseStream.Position = postPos;
		}

		/// <summary>
		/// Enumerates over <see cref="List{T}"/>, allowing you to read from the binary stream
		/// </summary>
		public static List<T> ReadList<T>( this BinaryReader br, Action<T, BinaryReader> ArrayAction ) where T : new()
		{
			List<T> retVal = new();
			int count = br.ReadInt32();
			for ( int i = 0; i < count; i++ )
			{
				T item = new();
				ArrayAction( item, br );
				retVal.Add( item );
			}
			return retVal;
		}

		/// <summary>
		/// Enumerates over <see cref="List{T}"/>
		/// </summary>
		public static List<T> ReadList<T>( this BinaryReader br ) where T : ISavable, new()
		{
			List<T> retVal = new();
			int count = br.ReadInt32();
			for ( int i = 0; i < count; i++ )
			{
				T item = new();
				item.Read(br);
				retVal.Add( item );
			}
			return retVal;
		}

		/// <summary>
		/// Reads a <see cref="Vector2"/> from the binary stream
		/// </summary>
		public static Vector2 ReadVector2( this BinaryReader br )
		{
			float x = br.ReadSingle();
			float y = br.ReadSingle();
			return new Vector2( x, y );
		}

		/// <summary>
		/// Reads a <see cref="Vector2Int"/> from the binary stream
		/// </summary>
		public static Vector2Int ReadVector2Int( this BinaryReader br )
		{
			int x = br.ReadInt32();
			int y = br.ReadInt32();
			return new Vector2Int( x, y );
		}

		/// <summary>
		/// Reads a <see cref="Vector3"/> from the binary stream
		/// </summary>
		public static Vector3 ReadVector3( this BinaryReader br )
		{
			float x = br.ReadSingle();
			float y = br.ReadSingle();
			float z = br.ReadSingle();
			return new Vector3( x, y, z );
		}

		/// <summary>
		/// Reads a <see cref="Vector3Int"/> from the binary stream
		/// </summary>
		public static Vector3Int ReadVector3Int( this BinaryReader br )
		{
			int x = br.ReadInt32();
			int y = br.ReadInt32();
			int z = br.ReadInt32();
			return new Vector3Int( x, y, z );
		}

		/// <summary>
		/// Reads a <see cref="Vector4"/> from the binary stream
		/// </summary>
		public static Vector4 ReadVector4( this BinaryReader br )
		{
			float x = br.ReadSingle();
			float y = br.ReadSingle();
			float z = br.ReadSingle();
			float w = br.ReadSingle();
			return new Vector4( x, y, z, w );
		}

		/// <summary>
		/// Reads a <see cref="Rotation"/> from the binary stream
		/// </summary>
		public static Rotation ReadRotation( this BinaryReader br )
		{
			float x = br.ReadSingle();
			float y = br.ReadSingle();
			float z = br.ReadSingle();
			float w = br.ReadSingle();
			return new Rotation( x, y, z, w );
		}

		/// <summary>
		/// Reads an <see cref="Angles"/> from the binary stream
		/// </summary>
		public static Angles ReadAngles( this BinaryReader br )
		{
			float x = br.ReadSingle();
			float y = br.ReadSingle();
			float z = br.ReadSingle();
			return new Angles( x, y, z );
		}

		public static void ReadObject<T>(this BinaryReader br, out T target)
		{
			target = ReadObject<T>( br );
		}

		public static T ReadObject<T>( this BinaryReader br )
		{
			T target = TypeLibrary.GetType<T>().Create<T>();
			BinaryClass.ReadObject( br, ref target, TypeLibrary.GetType<T>() );
			return target;
		}
	}
}
