using System;
using System.IO;

public class OsuPreReader : SubDirectoriesReader
{
    public OsuPreReader( string _dir ) : base( _dir ) { }

    protected override void ReadSubDirectories( string _subDir )
    {
        DirectoryInfo dirInfo = new DirectoryInfo( _subDir );

        // ä�� ����
        FileInfo[] charts = dirInfo.GetFiles( "*.osu" );
        if ( charts.Length == 0 ) return; // ä�� ������ ����

        // �Ľ�
        try
        {
            for ( int i = 0; i < charts.Length; i++ )
            {
                // �� ���� �� ���� ä�������� �ִ� ��찡 ����
                Initialize( charts[i].FullName );

                Song newSong = new Song();

                // [General] ~ [Editor]
                while ( ReadLine() != "[Metadata]" )
                {
                    if ( Contains( "AudioFilename" ) ) newSong.AudioPath   = Path.Combine( _subDir, SplitAndTrim( ':' ) );
                    if ( Contains( "PreviewTime" ) )   newSong.PreviewTime = int.Parse( SplitAndTrim( ':' ) );
                }

                // [Metadata] ~ [Difficulty]
                while ( ReadLine() != "[Events]" )
                {
                    if ( Contains( "Title" ) )   newSong.Title   = SplitAndTrim( ':' );
                    if ( Contains( "Artist" ) )  newSong.Artist  = SplitAndTrim( ':' );
                    if ( Contains( "Creator" ) ) newSong.Creator = SplitAndTrim( ':' );
                    if ( Contains( "Version" ) ) newSong.Version = SplitAndTrim( ':' );
                }

                // [Events]
                while ( ReadLine() != "[TimingPoints]" )
                {
                    if ( Contains( ".avi" ) || Contains( ".mp4" ) || Contains( ".mpg" ) )
                    {
                        newSong.VideoPath = Path.Combine( _subDir, SplitAndTrim( '"' ) );
                        newSong.HasVideo  = true;

                        FileInfo videoInfo = new FileInfo( newSong.VideoPath );
                        if ( !videoInfo.Exists ) newSong.HasVideo = false;
                    }

                    if ( Contains( ".jpg" ) || Contains( ".png" ) )
                    {
                        newSong.ImagePath = Path.Combine( _subDir, SplitAndTrim( '"' ) );

                        FileInfo imageInfo = new FileInfo( newSong.ImagePath );
                        if ( !imageInfo.Exists ) newSong.ImagePath = GlobalSetting.DefaultImagePath;
                    }
                }

                NowPlaying.Songs.Add( newSong );
            }
        }
        catch ( Exception _error )
        {
            UnityEngine.Debug.Log( _error.Message );
            Dispose();
        }
    }
}
