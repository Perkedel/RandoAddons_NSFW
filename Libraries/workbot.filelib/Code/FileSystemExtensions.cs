using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Sandbox
{
	public enum CompressionType
	{
		GZIP = 0,
		DEFLATE = 1,
	}

	public static class FileSystemExtensions
	{
		/// <summary>
		/// Starts writing a binary file at the provided path
		/// </summary>
		public static BinaryWriter StartWriteBinary(this BaseFileSystem fs, string path)
		{
			var write = fs.OpenWrite(path);
			return new BinaryWriter(write);
		}

		/// <summary>
		/// Starts reading a binary file at the provided path
		/// </summary>
		public static BinaryReader StartReadBinary( this BaseFileSystem fs, string path)
		{
			var read = fs.OpenRead(path);
			return new BinaryReader(read);
		}

		/// <summary>
		/// Starts writing a binary file at the provided path
		/// </summary>
		public static SafeStreamWriter StartWriteText( this BaseFileSystem fs, string path )
		{
			var write = fs.OpenWrite( path );
			return new SafeStreamWriter( write );
		}

		/// <summary>
		/// Starts reading a binary file at the provided path
		/// </summary>
		public static StreamReader StartReadText( this BaseFileSystem fs, string path )
		{
			var read = fs.OpenRead( path );
			return new StreamReader( read );
		}

		/// <summary>
		/// Write the contents to the path. The file will be over-written if the file exists
		/// </summary>
		public static void WriteAllBytes( this BaseFileSystem fs, string path, byte[] data )
		{
			var write = fs.OpenWrite( path );
			write.Write( data );
			write.Close();
		}

		/// <summary>
		/// Write the contents to the path. The file will be over-written if the file exists
		/// </summary>
		public static void WriteAllCompressedBytes(this BaseFileSystem fs, string path, byte[] data, CompressionLevel level, CompressionType compressionMethod = CompressionType.GZIP )
		{
			switch(compressionMethod)
			{
				case CompressionType.GZIP:
					fs.WriteAllBytes( path, DataCompression.CompressUsingGZip( data, level ) );
					break;
				case CompressionType.DEFLATE:
					fs.WriteAllBytes( path, DataCompression.CompressUsingDeflate( data, level ) );
					break;
				default:
					throw new Exception( "Compression method not supported: " + compressionMethod );
			}
		}

		/// <summary>
		/// Read the contents of path and return it as a byte array
		/// </summary>
		public static Span<byte> ReadAllCompressedBytes(this BaseFileSystem fs, string path, CompressionType compressionMethod = CompressionType.GZIP )
		{
			switch(compressionMethod)
			{
				case CompressionType.GZIP:
					return DataCompression.DecompressUsingGZip( fs.ReadAllBytes( path ).ToArray() );
				case CompressionType.DEFLATE:
					return DataCompression.DecompressUsingDeflate( fs.ReadAllBytes( path ).ToArray() );
				default:
					throw new Exception("Compression method not supported: " + compressionMethod);
			}
		}

		/// <summary>
		/// Write the contents to the path. The file will be over-written if the file exists
		/// </summary>
		public static void WriteAllCompressedText( this BaseFileSystem fs, string path, string data, CompressionLevel level, CompressionType compressionMethod = CompressionType.GZIP )
		{
			switch ( compressionMethod )
			{
				case CompressionType.GZIP:
					fs.WriteAllBytes( path, DataCompression.CompressUsingGZip( Encoding.ASCII.GetBytes( data ), level ) );
					break;
				case CompressionType.DEFLATE:
					fs.WriteAllBytes( path, DataCompression.CompressUsingDeflate( Encoding.ASCII.GetBytes( data ), level ) );
					break;
				default:
					throw new Exception( "Compression method not supported: " + compressionMethod );
			}
		}

		/// <summary>
		/// Read the contents of path and return it as a string
		/// </summary>
		public static string ReadAllCompressedText( this BaseFileSystem fs, string path, CompressionType compressionMethod = CompressionType.GZIP )
		{
			switch ( compressionMethod )
			{
				case CompressionType.GZIP:
					return Encoding.ASCII.GetString( DataCompression.DecompressUsingGZip( fs.ReadAllBytes( path ).ToArray() ) );
				case CompressionType.DEFLATE:
					return Encoding.ASCII.GetString( DataCompression.DecompressUsingDeflate( fs.ReadAllBytes( path ).ToArray() ) );
				default:
					throw new Exception( "Compression method not supported: " + compressionMethod );
			}
		}

		/// <summary>
		/// Creates a new zip file that you can write to
		/// </summary>
		public static ArchiveFile CreateZipFile( this BaseFileSystem fs )
		{
			return ArchiveFile.Create( fs );
		}

		/// <summary>
		/// Creates a new zip file that you can write to
		/// </summary>
		public static ArchiveFile CreateZipFile( this BaseFileSystem fs, string outputPath )
		{
			if ( outputPath.Replace( "\\", "/" ).Contains( "/" ) )
			{
				if ( !fs.DirectoryExists( outputPath.Substring( 0, outputPath.Replace( "\\", "/" ).LastIndexOf( "/" ) ) ) )
				{
					fs.CreateDirectory( outputPath.Substring( 0, outputPath.Replace( "\\", "/" ).LastIndexOf( "/" ) ) );
				}
			}
			return ArchiveFile.Create(fs, outputPath );
		}

		/// <summary>
		/// Reads a zip file that you can read from or modify
		/// </summary>
		public static ArchiveFile ReadZipFile( this BaseFileSystem fs, byte[] zipData, string path = "" )
		{
			ArchiveFile zip = ArchiveFile.Read( fs, zipData, path );
			return zip;
		}

		/// <summary>
		/// Reads a zip file that you can read from or modify
		/// </summary>
		public static ArchiveFile ReadZipFile( this BaseFileSystem fs, string path )
		{
			ArchiveFile zip = ArchiveFile.Read( fs, fs.ReadAllBytes(path).ToArray(), path );
			return zip;
		}
	}
}
