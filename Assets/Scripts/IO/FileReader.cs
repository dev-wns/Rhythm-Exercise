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

    // 한줄 읽기
    protected string ReadLine()
    {
        return line = streamReader.ReadLine();
    }

    // 현재 라인에서 단어 찾기
    protected bool Contains( string _str )
    {
        if ( line == null )
            return false;

        return line.Contains( _str );
    }

    // 토큰 자르고 공백없앤 후 반환
    protected string SplitAndTrim( char _separator )
    {
        if ( line == null || line == string.Empty ) 
            return string.Empty;

        return line.Split( _separator )[1].Trim();
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
            // 대부분 폴더가 없는 경우.
            Debug.LogWarning( $"{_error}, {_dirPath}" );
        }

        return paths.ToArray();
    }
}