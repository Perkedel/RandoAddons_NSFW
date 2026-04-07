using System.IO.Compression;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Sandbox
{
	public class DataCompression
	{
		/// <summary>
		/// Compresses the provided data using a GZipStream and returns it as a byte array
		/// </summary>
		public static byte[] CompressUsingGZip( byte[] data, CompressionLevel compression )
		{
			using ( var outputStream = new MemoryStream() )
			{
				using ( var compressionStream = new GZipStream( outputStream, compression ) )
				{
					compressionStream.Write( data, 0, data.Length );
				}
				return outputStream.ToArray();
			}
		}

		/// <summary>
		/// Decomresses the provided data using a GZipStream and returns it as a byte array
		/// </summary>
		public static byte[] DecompressUsingGZip( byte[] data )
		{
			using ( var inputStream = new MemoryStream( data ) )
			{
				using ( var outputStream = new MemoryStream() )
				{
					using ( var compressionStream = new GZipStream( inputStream, CompressionMode.Decompress ) )
					{
						compressionStream.CopyTo( outputStream );
					}
					return outputStream.ToArray();
				}
			}
		}

		/// <summary>
		/// Compresses the provided data using a DeflateStream and returns it as a byte array
		/// </summary>
		public static byte[] CompressUsingDeflate( byte[] data, CompressionLevel compression )
		{
			using ( var outputStream = new MemoryStream() )
			{
				using ( var compressionStream = new DeflateStream( outputStream, compression ) )
				{
					compressionStream.Write( data, 0, data.Length );
				}
				return outputStream.ToArray();
			}
		}

		/// <summary>
		/// Decomresses the provided data using a DeflateStream and returns it as a byte array
		/// </summary>
		public static byte[] DecompressUsingDeflate( byte[] data )
		{
			using ( var inputStream = new MemoryStream( data ) )
			{
				using ( var outputStream = new MemoryStream() )
				{
					using ( var compressionStream = new DeflateStream( inputStream, CompressionMode.Decompress ) )
					{
						compressionStream.CopyTo( outputStream );
					}
					return outputStream.ToArray();
				}
			}
		}

		/// <summary>
		/// Compresses the provided data based on repeated elements and returns it as a byte array
		/// </summary>
		public static byte[] Pack( byte[] data, int bytesPerElement )
		{
			List<byte> compressed = new();
			int i = 0;
			int numIds = 1;
			int currentCount = 0;
			byte[] tester = data.Skip( i ).Take( bytesPerElement ).ToArray();
			while ( i < data.Length )
			{
				if ( tester.SequenceEqual( data.Skip( i ).Take( bytesPerElement ).ToArray() ) )
				{
					currentCount++;
				}
				else
				{
					numIds++;
					compressed.AddRange( BitConverter.GetBytes( currentCount ) );
					compressed.AddRange( tester );
					tester = data.Skip( i ).Take( bytesPerElement ).ToArray();
					currentCount = 1;
				}
				i += bytesPerElement;
			}
			compressed.InsertRange( 0, BitConverter.GetBytes( numIds ) );
			compressed.AddRange( BitConverter.GetBytes( currentCount ) );
			compressed.AddRange( tester );
			return compressed.ToArray();
		}

		/// <summary>
		/// Decompresses the provided data based on repeated elements and returns it as a byte array
		/// </summary>
		public static byte[] Unpack( BinaryReader reader, int bytesPerElement )
		{
			int numIds = reader.ReadInt32();
			List<byte> result = new();
			for ( int j = 0; j < numIds; j++ )
			{
				int elemCount = reader.ReadInt32();
				byte[] toFill = reader.ReadBytes( bytesPerElement );
				result.AddRange( Enumerable.Repeat( toFill, elemCount ).SelectMany( col => col ) );
			}
			var newValue = result.ToArray();
			return newValue;
		}

		/// <summary>
		/// Decompresses the provided data based on repeated elements and returns it as a byte array
		/// </summary>
		public static byte[] Unpack( byte[] data, int bytesPerElement )
		{
			int numIds = Convert.ToInt32( data[0..4] );
			int i = 4;
			List<byte> result = new();
			for ( int j = 0; j < numIds; j++ )
			{
				int elemCount = Convert.ToInt32( data[i..(i + 4)] );
				i += 4;
				byte[] toFill = data[i..(i + bytesPerElement)];
				i += bytesPerElement;
				result.AddRange( Enumerable.Repeat( toFill, elemCount ).SelectMany( col => col ) );
			}
			var newValue = result.ToArray();
			return newValue;
		}
	}
}
