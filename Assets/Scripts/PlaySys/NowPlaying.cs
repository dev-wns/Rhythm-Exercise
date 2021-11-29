using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NowPlaying : Singleton<NowPlaying>
{
    public static MetaData Data   { get; private set; }

    // Bpm
    public static float BPM       { get; private set; } // ���� BPM
    public static float MedianBPM { get; private set; } // BPM�� ���� �� �߰���
    public static float Weight // BPM ��ȭ�� ��ũ�� �ӵ��� ����� ������Ʈ �ӵ� ����ġ
    {
        get
        {
            if ( GlobalSetting.IsFixedScroll ) return 0.25f * GlobalSetting.ScrollSpeed;              // 60bpm 1/4 ���� ����ġ ( 60bpm / 60( bpm -> bps ) / 4 ( 1beat = 4/4���� -> 1/4���� ����� ) )
            else                               return ( BPM / 60f / 4f ) * GlobalSetting.ScrollSpeed; // ����bpm 1/4 ���� ����ġ
        }
    }
    public delegate void BPMChangeDel();
    public static event BPMChangeDel BPMChangeEvent;


    // time ( millisecond )
    public static float Playback        { get; private set; } // �뷡 ��� �ð�
    public static float PlaybackChanged { get; private set; } // BPM ��ȭ�� ���� �뷡 ��� �ð�
    
    // 60bpm�� �д� 1/4���� 60��, ��ũ�� �ӵ��� 1�϶� �ѹ���(1/4) �ð��� 1��
    public static float PreLoadTime     { get { return ( 5f / GlobalSetting.ScrollSpeed * 1000f ); } } // 5���� �ð� ( ���� ��ũ�� �϶� )
    public static uint EndTime          { get; private set; } // �뷡 �� �ð� 

    private static readonly float InitWaitTime = -3000f;      // ���� �� ���ð�

    public static bool IsPlaying        { get; private set; } = false;
    private bool IsInitializing                               = true;
    private bool isSimpleMode                                 = false;
    private int TimingIdx;

    public void Initialized( MetaData _data )
    {
        Data = _data;
        InitializedValiables();

        // Find Median BPM
        List<float> bpmList = new List<float>();
        foreach ( var data in Data.timings )
        {
            float bpm = data.bpm;
            if ( !bpmList.Contains( bpm ) )
            {
                bpmList.Add( bpm );
            }
        }
        bpmList.Sort();
        MedianBPM = bpmList[Mathf.FloorToInt( bpmList.Count / 2f )];

        // Sound Work
        uint endTimeTemp;
        Data.sound.getLength( out endTimeTemp, FMOD.TIMEUNIT.MS );
        EndTime = endTimeTemp;
        
        Playback = InitWaitTime;
        IsInitializing = false;
    }

    private void InitializedValiables()
    {
        TimingIdx = 0;
        IsPlaying = false; IsInitializing = true;
    }

    public void Play( bool _isSimpleMode = false ) 
    {
        isSimpleMode = _isSimpleMode;
        StartCoroutine( StartProcess( _isSimpleMode ) ); 
    }

    private IEnumerator StartProcess( bool _isSimpleMode )
    {
        if ( !_isSimpleMode ) StartCoroutine( BpmChange() );
        else                  Playback = 0f;

        yield return new WaitUntil( () => !IsInitializing && Playback >= 0f );

        SoundManager.Inst.Play( Data.sound );

        uint playbackPos = 0;
        FMOD.Channel channel;
        FMOD.RESULT res = SoundManager.Inst.channelGroup.getChannel( 0, out channel );
        channel.getPosition( out playbackPos, FMOD.TIMEUNIT.MS );

        // first sync
        Playback = playbackPos;
        IsPlaying = true;
    }

    private IEnumerator BpmChange()
    {
        BPM = Data.timings[0].bpm;
        BPMChangeEvent();

        while ( TimingIdx < Data.timings.Count )
        {
            float changeTime = Data.timings[TimingIdx].changeTime;
            yield return new WaitUntil( () => Playback >= changeTime );

            BPM = Data.timings[TimingIdx].bpm;
            BPMChangeEvent();
            TimingIdx++;
        }
    }

    public static float GetChangedTime( float _time ) // BPM ��ȭ�� ���� �ð� ���
    {
        double newTime = _time;
        double prevBpm = 1;
        for ( int i = 0; i < Data.timings.Count - 1; i++ )
        {
            double time = Data.timings[i].changeTime;
            double listBpm = Data.timings[i].bpm;
            double bpm;
            if ( time > _time ) break; //������ Ÿ�̹��� �ƴϸ� ����������
            bpm = MedianBPM / listBpm;
            newTime += ( double )( bpm - prevBpm ) * ( _time - time ); // �ð� ���
            prevBpm = bpm; //����bpm��
        }
        return ( float )newTime;
    }

    private void Update()
    {
        if ( IsInitializing ) return;
        
        Playback += Time.deltaTime * 1000f; // second to millisecond

        if ( !isSimpleMode )
        {
            PlaybackChanged = GetChangedTime( Playback );
        }

        if ( Playback >= EndTime )
        {
            IsPlaying = false;
            IsInitializing = true;
            SoundManager.Inst.Stop();
        }
    }
}
