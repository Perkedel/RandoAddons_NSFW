using System;
using System.Collections.Generic;
using Sandbox;

[Title( "Radio Manager" ), Icon( "radio" ), Description( "Manages radio station playback, switching, and audio settings. Handles playing, pausing, stopping, and navigating through a list of RadioStation components." )]
public class RadioManager : Component
{
	[Property, Title( "Stations" ), Group( "Stations" )]
	public List<RadioStation> Stations { get; set; } = new();

	[Property, Title( "Auto Play on Start" ), Group( "Playback" )]
	public bool AutoPlay { get; set; } = true;

	[Property, Title( "Default Volume" ), Group( "Playback" )]
	[Range( 0, 1 )]
	public float DefaultVolume { get; set; } = 0.5f;

	[Property, Title( "Shuffle" ), Group( "Playback" )]
	public bool Shuffle { get; set; } = false;

	[Property, Title( "Loop Stations" ), Group( "Playback" )]
	public bool LoopStations { get; set; } = true;

	private MusicPlayer? _currentPlayer;
	private int _currentTrackIndex = -1;
	private bool _isPlaying;
	private float _volume;
	private List<int> _shuffledIndices = new();
	private Random _random = new Random();

	public bool IsPlaying => _isPlaying;
	public int CurrentTrackIndex => _currentTrackIndex;
	public int TotalTracks => Stations.Count;

	public float Volume
	{
		get => _volume;
		set
		{
			_volume = Math.Clamp( value, 0f, 1f );
			if ( _currentPlayer != null )
			{
				_currentPlayer.Volume = _volume;
			}
		}
	}

	public event Action OnPlayStateChanged;
	public event Action OnTrackChanged;

	protected override void OnStart()
	{
		Volume = DefaultVolume;

		if ( AutoPlay && Stations.Count > 0 )
		{
			Play();
		}
	}

	protected override void OnDestroy()
	{
		Stop();
	}

	private void GenerateShuffledIndices()
	{
		_shuffledIndices.Clear();
		for ( int i = 0; i < Stations.Count; i++ )
		{
			_shuffledIndices.Add( i );
		}

		for ( int i = _shuffledIndices.Count - 1; i > 0; i-- )
		{
			int j = _random.Next( i + 1 );
			int temp = _shuffledIndices[i];
			_shuffledIndices[i] = _shuffledIndices[j];
			_shuffledIndices[j] = temp;
		}
	}

	private int GetTrackIndex( int index )
	{
		if ( Shuffle && _shuffledIndices.Count > 0 )
		{
			return _shuffledIndices[index];
		}
		return index;
	}

	[Button]
	public void Play()
	{
		if ( Stations.Count == 0 )
		{
			Log.Warning( "RadioManager: Stations is empty." );
			return;
		}

		Stop();

		if ( _currentTrackIndex < 0 || _currentTrackIndex >= Stations.Count )
		{
			_currentTrackIndex = 0;
		}

		PlayCurrentTrack();
	}

	public void PlayTrack( int index )
	{
		if ( index < 0 || index >= Stations.Count )
		{
			Log.Warning( $"RadioManager: Invalid track index {index}." );
			return;
		}

		Stop();
		_currentTrackIndex = index;
		PlayCurrentTrack();
	}

	private void PlayCurrentTrack()
	{
		if ( _currentTrackIndex < 0 || _currentTrackIndex >= Stations.Count )
			return;

		var trackIndex = GetTrackIndex( _currentTrackIndex );
		var station = Stations[trackIndex];

		if ( station == null || string.IsNullOrEmpty( station.StreamUrl ) )
		{
			Log.Warning( $"RadioManager: Station at index {trackIndex} is null or empty." );
			return;
		}

		_currentPlayer = MusicPlayer.PlayUrl( station.StreamUrl );
		if ( _currentPlayer != null )
		{
			_currentPlayer.Volume = _volume;
			_isPlaying = true;
			Log.Info( $"[RadioManager] Now playing: {station.StationName} ({station.StreamUrl})" );
			OnPlayStateChanged?.Invoke();
			OnTrackChanged?.Invoke();
		}
		else
		{
			Log.Warning( $"RadioManager: Failed to play URL: {station.StreamUrl}" );
		}
	}

	[Button]
	public void PlayNext()
	{
		if ( Stations.Count == 0 ) return;

		Stop();

		_currentTrackIndex++;
		if ( _currentTrackIndex >= Stations.Count )
		{
			if ( LoopStations )
			{
				_currentTrackIndex = 0;
				if ( Shuffle )
					GenerateShuffledIndices();
			}
			else
			{
				_currentTrackIndex = Stations.Count - 1;
				return;
			}
		}

		PlayCurrentTrack();
	}

	[Button]
	public void PlayPrevious()
	{
		if ( Stations.Count == 0 ) return;

		Stop();

		_currentTrackIndex--;
		if ( _currentTrackIndex < 0 )
		{
			if ( LoopStations )
			{
				_currentTrackIndex = Stations.Count - 1;
			}
			else
			{
				_currentTrackIndex = 0;
			}
		}

		PlayCurrentTrack();
	}

	[Button]
	public void Stop()
	{
		if ( _currentPlayer != null )
		{
			_currentPlayer.Stop();
			_currentPlayer = null;
		}
		_isPlaying = false;
		Log.Info( "[RadioManager] Playback stopped." );
		OnPlayStateChanged?.Invoke();
	}

	public void TogglePlayPause()
	{
		if ( _currentPlayer == null )
		{
			Play();
			return;
		}

		// Toggle the Paused property
		_currentPlayer.Paused = !_currentPlayer.Paused;
		_isPlaying = !_currentPlayer.Paused;
		OnPlayStateChanged?.Invoke();
	}

	public string GetCurrentTrackUrl()
	{
		if ( _currentTrackIndex < 0 || _currentTrackIndex >= Stations.Count )
			return "";

		var trackIndex = GetTrackIndex( _currentTrackIndex );
		var station = Stations[trackIndex];
		return station?.StreamUrl ?? "";
	}

	public string GetCurrentStationName()
	{
		if ( _currentTrackIndex < 0 || _currentTrackIndex >= Stations.Count )
			return "No Station";

		var trackIndex = GetTrackIndex( _currentTrackIndex );
		var station = Stations[trackIndex];
		return station?.StationName ?? "Unknown Station";
	}

	public string GetCurrentStationSource()
	{
		if ( _currentTrackIndex < 0 || _currentTrackIndex >= Stations.Count )
			return "Unknown";

		var trackIndex = GetTrackIndex( _currentTrackIndex );
		var station = Stations[trackIndex];
		return station?.Source ?? "Unknown";
	}

	public string GetCurrentTrackName()
	{
		return GetCurrentStationName();
	}

	public string GetCurrentTrackAuthor()
	{
		return GetCurrentStationSource();
	}

	public void SetStations( List<RadioStation> stations )
	{
		if ( stations == null || stations.Count == 0 ) return;

		Stations = stations;
		if ( Shuffle )
			GenerateShuffledIndices();

		_currentTrackIndex = 0;
		if ( _isPlaying )
		{
			Play();
		}
	}
}
