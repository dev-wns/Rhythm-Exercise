using System;
using System.IO;

public abstract class FileReader : IDisposable
{
    private StreamReader streamReader;
    public string line { get; private set; }

    public void Initialize( string _path )
    {
        if ( !ReferenceEquals( null, streamReader ) ) 
            Dispose();

        try { streamReader = new StreamReader( _path ); }
        catch ( Exception error ) { UnityEngine.Debug.Log( $"The file could not be read : {error.Message}" ); }
    }

    // ���� �б�
    public string ReadLine()
    {
        return line = streamReader.ReadLine();
    }

    // ���� ���ο��� �ܾ� ã��
    public bool Contains( string _str )
    {
        if ( line == null )
            return false;

        return line.Contains( _str );
    }

    // ��ū �ڸ��� ������� �� ��ȯ
    public string SplitAndTrim( char _separator )
    {
        if ( line == null || line == string.Empty ) 
            return string.Empty;

        return line.Split( _separator )[1].Trim();
    }

    // Ư�� �ܾ� ���ö����� Read
    public string ReadContainsLine( string _str )
    {
        if ( _str == string.Empty ) 
            return string.Empty;

        while ( Contains( _str ) ) 
            ReadLine();

        return line;
    }

    public abstract void Read();

    public void Dispose() => streamReader?.Dispose();
}