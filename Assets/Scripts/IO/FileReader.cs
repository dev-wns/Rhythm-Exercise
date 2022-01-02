using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public abstract class FileReader : IDisposable
{
    private StreamReader streamReader;
    protected string path { get; private set; }
    protected string directory { get; private set; }
    protected string line { get; private set; }

    protected bool ReadLineEndOfStream()
    {
        if ( streamReader.EndOfStream ) return false;
        else
        {
            line = streamReader.ReadLine();
            return true;
        }
    }

    public FileReader() { }

    protected FileReader( string _path )
    {
        path = _path;
        try 
        {
            streamReader = new StreamReader( _path );
            directory    = Path.GetDirectoryName( _path );
        }
        catch ( Exception error ) { UnityEngine.Debug.Log( $"The file could not be read : { error.Message }" ); }
    }

    public void OpenFile( string _path )
    {
        streamReader?.Dispose();

        path = _path;
        try
        {
            streamReader = new StreamReader( _path );
            directory = Path.GetDirectoryName( _path );
        }
        catch ( Exception error ) { UnityEngine.Debug.Log( $"The file could not be read : { error.Message }" ); }
    }

    // ���� �б�
    protected string ReadLine()
    {
        return line = streamReader.ReadLine();
    }

    // ���� ���ο��� �ܾ� ã��
    protected bool Contains( string _str )
    {
        if ( line == null )
            return false;

        return line.Contains( _str );
    }

    // ��ū �ڸ��� ������� �� ��ȯ
    protected string SplitAndTrim( char _separator )
    {
        if ( line == null || line == string.Empty ) 
            return string.Empty;

        return line.Split( _separator )[1].Trim();
    }

    public void Dispose() => streamReader?.Dispose();

    protected string[] GetFilesInSubDirectories( string _dirPath, string _extension )
    {
        List<string> path = new List<string>();

        string[] subDirectories;
        try { subDirectories = Directory.GetDirectories( _dirPath ); }
        catch ( Exception e )
        {
            // ��κ� ������ ���� ���.
            Debug.Log( e.ToString() );
            return path.ToArray();
        }

        foreach ( string subDir in subDirectories )
        {
            DirectoryInfo dirInfo = new DirectoryInfo( subDir );
            FileInfo[] files = dirInfo.GetFiles( _extension );
            for ( int i = 0; i < files.Length; i++ )
                path.Add( files[i].FullName );
        }

        return path.ToArray();
    }
}