using System.IO.Compression;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Sandbox
{
	public class ArchiveFile
	{
		MemoryStream outputStream;
		BaseFileSystem outFileSystem;
		string outPath;

		Dictionary<BinaryWriter, KeyValuePair<MemoryStream, KeyValuePair<string, CompressionLevel>>> Writers = new();
		Dictionary<BinaryReader, string> Readers = new();

		public string Comment
		{
			get
			{
				using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
				{
					return compressionStream.Comment;
				}
			}
			set
			{
				using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
				{
					compressionStream.Comment = value;
				}
			}
		}

		public int EntryCount
		{
			get
			{
				using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
				{
					return compressionStream.Entries.Count();
				}
			}
		}

		~ArchiveFile()
		{
			outputStream.Close();
			foreach ( var writer in Writers.Keys.ToList() )
			{
				EndWrite( writer );
			}

			foreach ( var reader in Readers.ToList() )
			{
				EndRead( reader.Key );
			}
		}

		internal static ArchiveFile Create( BaseFileSystem fs, string path = "" )
		{
			ArchiveFile zip = new();
			zip.outputStream = new MemoryStream();
			if ( path != "" )
			{
				zip.outFileSystem = fs;
				zip.outPath = path;
			}
			return zip;
		}

		internal static ArchiveFile Read( BaseFileSystem fs, byte[] data, string path = "" )
		{
			ArchiveFile zip = new();
			zip.outputStream = new MemoryStream( data );
			if ( path != "" )
			{
				zip.outFileSystem = fs;
				zip.outPath = path;
			}
			return zip;
		}

		public void WriteFile(string path)
		{
			string outTemp = outPath;
			outPath = path;
			Flush();
			outPath = outTemp;
		}

		/// <summary>
		/// Write the contents to the path. The file will be over-written if the file exists
		/// </summary>
		public void Flush()
		{
			if ( outPath != "" )
			{
				outFileSystem.WriteAllBytes( outPath, outputStream.ToArray() );
			}
		}

		/// <summary>
		/// Read the contents of path and return it as a byte array
		/// </summary>
		public byte[] ReadAllBytes( string path )
		{
			path = path.Replace( "\\", "/" );
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Read, true ) )
			{
				using ( Stream EntryStream = compressionStream.GetEntry( path ).Open() )
				{
					using ( MemoryStream EntryBytes = new MemoryStream() )
					{
						EntryStream.CopyTo( EntryBytes );
						return EntryBytes.ToArray();
					}
				}
			}
		}

		/// <summary>
		/// Create a zip element
		/// </summary>
		/// <param name="path"></param>
		public void CreateEntry( string path )
		{
			path = path.Replace( "\\", "/" );
			WriteEntry( path, new byte[] { } );
		}

		/// <summary>
		/// Write the contents to the zip element. The file will be over-written if the file exists
		/// </summary>
		public void WriteEntry( string path, byte[] data, CompressionLevel compression = CompressionLevel.Optimal )
		{
			path = path.Replace( "\\", "/" );
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
			{
				var entry = compressionStream.CreateEntry( path, compression );
				using ( var entryStream = entry.Open() )
				{
					entryStream.Write( data );
				}
			}
			Flush();
		}

		/// <summary>
		/// Returns true if the given path exists in the zip file, false if it doesn't
		/// </summary>
		/// <param name="path"></param>
		public bool EntryExists( string path )
		{
			path = path.Replace( "\\", "/" );
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
			{
				return compressionStream.GetEntry( path ) != null;
			}
		}

		/// <summary>
		/// Gets the comment of a zip element
		/// </summary>
		/// <param name="path"></param>
		public string GetComment( string path )
		{
			path = path.Replace( "\\", "/" );
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
			{
				return compressionStream.GetEntry( path ).Comment;
			}
		}

		/// <summary>
		/// Sets the comment of a zip element
		/// </summary>
		/// <param name="path"></param>
		public void SetComment( string path, string comment )
		{
			path = path.Replace( "\\", "/" );
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
			{
				compressionStream.GetEntry( path ).Comment = comment;
			}
			Flush();
		}

		/// <summary>
		/// Returns the compressed length of a zip element
		/// </summary>
		public long GetCompressedLength( string path )
		{
			path = path.Replace( "\\", "/" );
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
			{
				return compressionStream.GetEntry( path ).CompressedLength;
			}
		}

		/// <summary>
		/// Returns the uncompressed length of a zip element
		/// </summary>
		public long GetLength( string path )
		{
			path = path.Replace( "\\", "/" );
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
			{
				return compressionStream.GetEntry( path ).Length;
			}
		}

		/// <summary>
		/// Gets the attributes of a zip element
		/// </summary>
		public int GetAttributes( string path )
		{
			path = path.Replace( "\\", "/" );
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
			{
				return compressionStream.GetEntry( path ).ExternalAttributes;
			}
		}

		/// <summary>
		/// Sets the attributes of a zip element
		/// </summary>
		public void SetAttributes( string path, int attributes )
		{
			path = path.Replace( "\\", "/" );
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
			{
				compressionStream.GetEntry( path ).ExternalAttributes = attributes;
			}
			Flush();
		}

		/// <summary>
		/// Returns the name of a zip element
		/// </summary>
		public string GetName( string path )
		{
			path = path.Replace( "\\", "/" );
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
			{
				return compressionStream.GetEntry( path ).Name;
			}
		}

		/// <summary>
		/// Returns the full name of a zip element
		/// </summary>
		public string GetFullName( string path )
		{
			path = path.Replace( "\\", "/" );
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
			{
				return compressionStream.GetEntry( path ).FullName;
			}
		}

		/// <summary>
		/// Returns the CRC32 of a zip element
		/// </summary>
		public uint GetCrc32( string path )
		{
			path = path.Replace( "\\", "/" );
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
			{
				return compressionStream.GetEntry( path ).Crc32;
			}
		}

		/// <summary>
		/// Returns true if the zip element is encrypted
		/// </summary>
		public bool IsEncrypted( string path )
		{
			path = path.Replace( "\\", "/" );
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
			{
				return compressionStream.GetEntry( path ).IsEncrypted;
			}
		}

		/// <summary>
		/// Returns the time this zip element was last written to
		/// </summary>
		public DateTimeOffset GetLastWriteTime( string path )
		{
			path = path.Replace( "\\", "/" );
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
			{
				return compressionStream.GetEntry( path ).LastWriteTime;
			}
		}

		/// <summary>
		/// Delete a zip element
		/// </summary>
		/// <param name="path"></param>
		public void DeleteEntry( string path )
		{
			path = path.Replace( "\\", "/" );
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
			{
				compressionStream.GetEntry( path ).Delete();
			}
			Flush();
		}

		/// <summary>
		/// Starts writing to the specified zip element
		/// </summary>
		public BinaryWriter StartWrite(string path, CompressionLevel compression = CompressionLevel.Optimal )
		{
			path = path.Replace( "\\", "/" );
			MemoryStream outStream = new MemoryStream();
			BinaryWriter writer = new( outStream );
			Writers.Add( writer, KeyValuePair.Create( outStream, KeyValuePair.Create(path, compression) ) );
			return writer;
		}

		/// <summary>
		/// Stops writing to the specified zip element
		/// </summary>
		public void EndWrite( BinaryWriter writer)
		{
			writer.Close();
			byte[] data = Writers[writer].Key.ToArray();
			WriteEntry( Writers[writer].Value.Key, data, Writers[writer].Value.Value );
			Writers.Remove( writer );
		}

		/// <summary>
		/// Starts reading from the specified zip element
		/// </summary>
		public BinaryReader StartRead(string path)
		{
			path = path.Replace( "\\", "/" );
			BinaryReader reader = new( new MemoryStream( ReadAllBytes( path ) ) );
			Readers.Add( reader, path );
			return reader;
		}

		/// <summary>
		/// Stops reading from the specified zip element
		/// </summary>
		public void EndRead(BinaryReader reader)
		{
			reader.Close();
			Readers.Remove( reader );
		}

		/// <summary>
		/// Returns the zip file as a byte array
		/// </summary>
		/// <returns></returns>
		public byte[] GetBytes()
		{
			return outputStream.ToArray();
		}

		/// <summary>
		/// Enumerates all elements in the archive, with the option for recursive searching, searching within a folder, or both
		/// </summary>
		public string[] EnumerateEntries(string searchPath = "", bool recursive = true)
		{
			searchPath = searchPath.Replace( "\\", "/" );
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
			{
				List<string> paths = new();
				foreach(var entry in compressionStream.Entries)
				{
					string path = entry.ToString();
					if (searchPath == "")
					{
						if(recursive || !path.Contains("/"))
						{
							paths.Add( path );
						}
					} else
					{
						if(!searchPath.EndsWith("/") )
						{
							searchPath = searchPath + "/";
						}
						if( path.StartsWith(searchPath) )
						{
							if(recursive)
							{
								paths.Add( path );
							}
							else
							{
								if(!path.Replace(searchPath, "").Contains("/"))
								{
									paths.Add( path );
								}
							}
						}
					}
				}
				return paths.ToArray();
			}
		}

		/// <summary>
		/// Enumerate all files in the archive, with the option for recursive searching, searching within a folder, or both
		/// </summary>
		public string[] EnumerateFiles( string searchPath, bool recursive = false )
		{
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
			{
				string Path = "";
				if ( !string.IsNullOrEmpty( searchPath ) )
				{
					Path = searchPath.Replace( "\\", "/" );
					if ( !Path.Contains( "/" ) )
					{
						Path += "/";
					}
				}

				List<string> files = new();
				foreach ( var entry in compressionStream.Entries )
				{
					string SlashCheck = entry.FullName;
					if ( !string.IsNullOrEmpty( Path ) )
					{
						SlashCheck = entry.FullName.Replace( Path, "" );
					}
					if ( !string.IsNullOrEmpty( entry.Name ) && entry.FullName.StartsWith( Path ) && (recursive || !SlashCheck.Contains( "/" )) )
					{
						files.Add( entry.FullName );
					}
				}

				return files.ToArray();
			}
		}

		/// <summary>
		/// Enumerate all directories in the archive, with the option for recursive searching, searching within a folder, or both
		/// </summary>
		public string[] EnumerateDirectories( string searchPath, bool recursive = false )
		{
			using ( var compressionStream = new ZipArchive( outputStream, ZipArchiveMode.Update, true ) )
			{
				string Path = "";
				if ( !string.IsNullOrEmpty( searchPath ) )
				{
					Path = searchPath.Replace( "\\", "/" );
					if ( !Path.Contains( "/" ) )
					{
						Path += "/";
					}
				}

				List<string> files = new();
				foreach ( var entry in compressionStream.Entries )
				{
					string SlashCheck = entry.FullName;
					if ( !string.IsNullOrEmpty( Path ) )
					{
						SlashCheck = entry.FullName.Replace( Path, "" );
					}
					if ( string.IsNullOrEmpty( entry.Name ) && entry.FullName.StartsWith( Path ) && (recursive || (SlashCheck.Contains( "/" ) && SlashCheck.IndexOf( "/" ) == SlashCheck.LastIndexOf( "/" ))) )
					{
						files.Add( entry.FullName );
					}
				}

				return files.ToArray();
			}
		}

		/// <summary>
		/// Adds a zip element based on an input file
		/// </summary>
		public void Import(BaseFileSystem fileSystem, string filePath, string entryPath, CompressionLevel compression = CompressionLevel.Optimal )
		{
			entryPath = entryPath.Replace( "\\", "/" );
			var write = StartWrite( entryPath, compression );
			write.Write(fileSystem.ReadAllBytes(filePath));
			EndWrite( write );
			Flush();
		}

		/// <summary>
		/// Extracts a zip element to a specified location
		/// </summary>
		public void Extract(BaseFileSystem fileSystem, string entryPath, string outputPath)
		{
			entryPath = entryPath.Replace( "\\", "/" );
			fileSystem.WriteAllBytes( outputPath, ReadAllBytes( entryPath ) );
		}

		/// <summary>
		/// Extracts all zip elements to a specified folder
		/// </summary>
		public void Extract( BaseFileSystem fileSystem, string outputFolder )
		{
			outputFolder = outputFolder.Replace( "\\", "/" );
			if (!outputFolder.EndsWith("/") )
			{
				outputFolder = outputFolder + "/";
			}
			foreach(string path in EnumerateEntries())
			{
				fileSystem.WriteAllBytes( outputFolder + path, ReadAllBytes( path ) );
			}
		}
	}
}
