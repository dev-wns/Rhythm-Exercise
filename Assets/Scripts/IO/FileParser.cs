using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System;
using System.IO;

public class FileParser : FileReader
{
    public void ParseFilesInDirectories( out ReadOnlyCollection<Song> _songs )
    {
        List<Song> songs = new List<Song>();

        var files = GetFilesInSubDirectories( GameSetting.SoundDirectoryPath, "*.wns" );
        for ( int i = 0; i < files.Length; i++ )
        {
            Song newSong = new Song();
            if ( TryParse( files[i], out newSong ) )
                 songs.Add( newSong );
        }

        _songs = new ReadOnlyCollection<Song>( songs );
    }

    public bool TryParse( string _path, out Song _song )
    {
        _song = new Song();

        try
        {
            OpenFile( _path );

            string directory = Path.GetDirectoryName( _path );
            _song.filePath   = _path;

            while ( ReadLine() != "[Timings]" )
            {
                if ( line == string.Empty ) break;

                if ( Contains( "AudioPath:" ) )
                {
                    var soundName = Split( ':' );
                    if ( soundName == string.Empty )
                        _song.audioPath = string.Empty;
                    else
                        _song.audioPath = Path.Combine( directory, soundName );
                }
                if ( Contains( "ImagePath:" ) )
                {
                    var imageName = Split( ':' );
                    if ( imageName == string.Empty )
                        _song.imagePath = string.Empty;
                    else
                        _song.imagePath = Path.Combine( directory, imageName );
                }
                if ( Contains( "VideoPath:" ) )
                {
                    string videoName = Split( ':' );

                    if ( videoName == string.Empty )
                    {
                        _song.videoPath = string.Empty;
                        _song.hasVideo = false;
                    }
                    else
                    {
                        _song.videoPath = Path.Combine( directory, videoName );
                        _song.hasVideo = true;
                    }
                }
                if ( Contains( "VideoOffset:" ) ) _song.videoOffset = int.Parse( Split( ':' ) );

                if ( Contains( "Title:" ) )   _song.title   = Replace( "Title:",   string.Empty );
                if ( Contains( "Artist:" ) )  _song.artist  = Replace( "Artist:",  string.Empty );
                if ( Contains( "Creator:" ) ) _song.creator = Replace( "Creator:", string.Empty );
                if ( Contains( "Version:" ) ) _song.version = Replace( "Version:", string.Empty );

                if ( Contains( "PreviewTime:" ) ) _song.previewTime = int.Parse( Split( ':' ) );
                if ( Contains( "TotalTime:" ) )   _song.totalTime   = int.Parse( Split( ':' ) );

                if ( Contains( "NumNote:" ) )   _song.noteCount   = int.Parse( Split( ':' ) );
                if ( Contains( "NumSlider:" ) ) _song.sliderCount = int.Parse( Split( ':' ) );

                if ( Contains( "MinBPM:" ) ) _song.minBpm    = int.Parse( Split( ':' ) );
                if ( Contains( "MaxBPM:" ) ) _song.maxBpm    = int.Parse( Split( ':' ) );
                if ( Contains( "Median:" ) ) _song.medianBpm = double.Parse( Split( ':' ) );
                if ( Contains( "Virtual:" ) ) _song.isOnlyKeySound = int.Parse( Split( ':' ) ) == 1 ? true : false;
            }
        }
        catch ( Exception _error )
        {
            Debug.LogError( $"{_error}, {_path}" );
            Dispose();
            return false;
        }

        return true;
    }

    public bool TryParse( string _path, out Chart _chart )
    {
        _chart = new Chart();

        try
        {
            OpenFile( _path );
            while ( ReadLine() != "[Timings]" ) { }

#region Timings
            List<Timing> timings = new List<Timing>();

            while ( ReadLine() != "[Sprites]" )
            {
                Timing timing = new Timing();
                var split = line.Split( ',' );

                timing.time        = double.Parse( split[0] ) * .001d;
                timing.beatLength  = double.Parse( split[1] );
                timing.bpm         = 1d / timing.beatLength * 60000d;

                timings.Add( timing );
            }

            if ( timings.Count == 0 )
                 throw new Exception( "Timing Parsing Error" );

            _chart.timings = new ReadOnlyCollection<Timing>( timings );
#endregion
            
#region Sprite Samples
            List<SpriteSample> sprites = new List<SpriteSample>();
            while ( ReadLine() != "[Samples]" )
            {
                SpriteSample sprite;
                var split = line.Split( ',' );

                sprite.type  = ( SpriteType )int.Parse( split[0] );
                sprite.start = double.Parse( split[1] ) * .001d;
                sprite.end   = double.Parse( split[2] ) * .001d;
                sprite.name  = split[3];

                sprites.Add( sprite );
            }
            _chart.sprites = new ReadOnlyCollection<SpriteSample>( sprites );
#endregion

#region Key Samples
            List<KeySound> keySounds = new List<KeySound>();
            while ( ReadLine() != "[Notes]" )
            {
                KeySound sample;
                var split = line.Split( ',' );

                sample.time = double.Parse( split[0] ) * .001d;
                sample.volume = float.Parse( split[1] ) * .01f;
                sample.name = split[2];
                sample.sound = new FMOD.Sound();
                sample.hasSound = sample.name == string.Empty ? false : true;

                keySounds.Add( sample );
            }
            _chart.samples = new ReadOnlyCollection<KeySound>( keySounds );
#endregion

#region Notes
            List<Note> notes = new List<Note>();

            while ( ReadLineEndOfStream() )
            {
                Note note = new Note();
                var split = line.Split( ',' );

                note.lane           = int.Parse( split[0] );
                note.time           = double.Parse( split[1] ) * .001d;
                note.sliderTime     = double.Parse( split[2] ) * .001d;
                note.isSlider       = note.sliderTime > 0d ? true : false;

                var keySoundSplit = split[3].Split( ':' );
                note.keySound.volume   = float.Parse( keySoundSplit[0] ) * .01f;
                note.keySound.name     = keySoundSplit[1];
                note.keySound.hasSound = note.keySound.name == string.Empty ? false : true;

                notes.Add( note );
            }

            if ( timings.Count == 0 )
                throw new Exception( "Note Parsing Error" );

            _chart.notes = new ReadOnlyCollection<Note>( notes );
#endregion
        }
        catch ( Exception _error )
        {
            Debug.LogError( _error.Message );
            Dispose();
            return false;
        }

        return true;
    }
}
