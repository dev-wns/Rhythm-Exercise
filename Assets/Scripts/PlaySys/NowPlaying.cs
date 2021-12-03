using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NowPlaying : Singleton<NowPlaying>
{
    public static MetaData Data   { get; private set; }

    // Bpm
    public static float BPM       { get; private set; } // ���� BPM
    public static float Weight // BPM ��ȭ�� ��ũ�� �ӵ��� ����� ������Ʈ �ӵ� ����ġ
    {
        get
        {
            if ( GlobalSetting.IsFixedScroll ) return .25f * GlobalSetting.ScrollSpeed;          // 60bpm 1/4 ���� ����ġ
            else                               return ( BPM / 60f ) * GlobalSetting.ScrollSpeed; // ����bpm 1/4 ���� ����ġ
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

    public static readonly float InitWaitTime = 3f;      // ���� �� ���ð�

    public static bool IsPlaying        { get; private set; } = false;
    private int timingIdx;
    private Coroutine curCoroutine = null;

    public void Initialized( MetaData _data )
    {
        if ( !ReferenceEquals( curCoroutine, null ) ) StopCoroutine( curCoroutine );

        Data = _data;
        InitializedVariables();
    }

    private void InitializedVariables() 
    {
        Playback = 0f; PlaybackChanged = 0f;
        timingIdx = 0; EndTime = 0; BPM = 0f;
        IsPlaying = false; 
    }

    public void Play( bool _isSimpleMode = false ) 
    {
        curCoroutine = StartCoroutine( PlayMusic( _isSimpleMode ) ); 
    }

    private IEnumerator PlayMusic( bool _isSimpleMode )
    {
        if ( !_isSimpleMode )
        {
            StartCoroutine( BpmChange() );
            IsPlaying = true;
            // yield return new WaitUntil( () => Playback >= 0 );
            yield return YieldCache.WaitForSeconds( InitWaitTime );
        }
        else yield return null;

        // first sync
        SoundManager.Inst.LoadAndPlay( Data.audioPath );
        EndTime  = SoundManager.Inst.Length;
        //Playback = SoundManager.Inst.Position;

        IsPlaying = true;
    }

    private IEnumerator BpmChange()
    {
        BPM = Data.timings[0].bpm;
        BPMChangeEvent();

        while ( timingIdx < Data.timings.Count )
        {
            float changeTime = Data.timings[timingIdx].changeTime;
            yield return new WaitUntil( () => Playback >= changeTime );

            BPM = Data.timings[timingIdx].bpm;
            BPMChangeEvent();
            timingIdx++;
        }
    }

    public static float GetChangedTime( float _time ) // BPM ��ȭ�� ���� �ð� ���
    {
        double newTime = _time;
        double prevBpm = 0d;
        for ( int i = 0; i < Data.timings.Count; i++ )
        {
            double time = Data.timings[i].changeTime;
            double bpm = Data.timings[i].bpm;

            if ( time > _time ) break;
            newTime += ( bpm - prevBpm ) * ( _time - time );
            prevBpm = bpm;
        }
        return ( float )newTime;
    }

    private void Update()
    {
        if ( !IsPlaying ) return;

        Playback += Time.deltaTime * 1000f;
        PlaybackChanged = GetChangedTime( Playback );

        if ( Playback >= EndTime )
        {
            IsPlaying = false;
            SoundManager.Inst.Stop();
        }
    }
}
