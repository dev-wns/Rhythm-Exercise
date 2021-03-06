using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public abstract class FileReader : IDisposable
{
    private StreamReader streamReader;
    protected string path { get; private set; }
    protected string dir { get; private set; }
    protected string line { get; private set; }

    public void Dispose() => streamReader?.Dispose();

    public void OpenFile( string _path )
    {
        path = _path;
        try
        {
            Dispose();

            streamReader = new StreamReader( @$"\\?\{_path}" );
            dir = Path.GetDirectoryName( _path );
        }
        catch ( Exception _error )
        {
            throw _error;
        }
    }

    protected bool ReadLineEndOfStream()
    {
        if ( streamReader.EndOfStream ) return false;
        else
        {
            line = streamReader.ReadLine();
            return true;
        }
    }

    protected string ReadLine()
    {
        return line = streamReader.ReadLine();
    }

    protected bool Contains( string _str )
    {
        if ( line == null )
            return false;

        return line.Contains( _str );
    }

    protected string Split( char _separator )
    {
        if ( line == null || line == string.Empty ) 
            return string.Empty;

        return line.Split( _separator )[1].Trim();
    }

    protected string Replace( string _old, string _new )
    {
        if ( line == null || line == string.Empty )
            return string.Empty;
        
        return line.Replace( _old, _new ).Trim();
    }

    protected string[] GetFilesInSubDirectories( string _dirPath, string _extension )
    {
        List<string> paths = new List<string>();

        try 
        {
            string[] subDirectories = Directory.GetDirectories( _dirPath );

            paths.Capacity = subDirectories.Length;
            for ( int i = 0; i < subDirectories.Length; i++ )
            {
                DirectoryInfo dirInfo = new DirectoryInfo( subDirectories[i] );
                FileInfo[] files      = dirInfo.GetFiles( _extension );

                for ( int j = 0; j < files.Length; j++ )
                      paths.Add( files[j].FullName );
            }
        }
        catch ( Exception _error )
        {
            // ?????? ?????? ???? ????.
            Debug.LogWarning( $"{_error}, {_dirPath}" );
        }

        return paths.ToArray();
    }
}