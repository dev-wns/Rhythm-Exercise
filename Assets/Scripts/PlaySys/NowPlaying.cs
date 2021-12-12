using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NowPlaying : Singleton<NowPlaying>
{
    public static Song CurrentSong   { get; private set; }
    
    // Bpm
    public static float BPM       { get; private set; } // ���� BPM
    public static float Weight // BPM ��ȭ�� ��ũ�� �ӵ��� ����� ������Ʈ �ӵ� ����ġ
    {
        get
        {
            if ( !GlobalSetting.IsFixedScroll ) return 3f / BPM * GlobalSetting.ScrollSpeed; 
            else                                return 3f / MedianBpm * GlobalSetting.ScrollSpeed;          
        }
    }
    public static float MedianBpm;
    public delegate void BPMChangeDel();
    public static event BPMChangeDel BPMChangeEvent;

    // time ( millisecond )
    public static float Playback        { get; private set; } // �뷡 ��� �ð�
    public static float PlaybackChanged { get; private set; } // BPM ��ȭ�� ���� �뷡 ��� �ð�
    private Timer timer = new Timer();
    
    // 60bpm�� �д� 1/4���� 60��, ��ũ�� �ӵ��� 1�϶� �ѹ���(1/4) �ð��� 1��
    public static float PreLoadTime     { get { return ( 150f / GlobalSetting.ScrollSpeed ) * 1000f; } } // 5���� �ð� ( ���� ��ũ�� �϶� )
    public static uint EndTime          { get; private set; } // �뷡 �� �ð� 

    public static readonly float InitWaitTime = 1f;      // ���� �� ���ð�

    public static bool IsPlaying        { get; private set; } = false;
    private int timingIdx;
    private Coroutine curCoroutine = null;
    class BPMS
    {
        public float bpm, time;
        public BPMS( float _bpm, float _time )
        { bpm = _bpm; time = _time; }
    }
    class MedianCac
    {
        public float time; public double bpm; public int key;
        public MedianCac( float time, double bpm )
        {
            this.time = time;
            this.bpm = bpm;
            key = Mathf.FloorToInt( ( float )bpm );
        }
    }

    private void Awake()
    {
        DontDestroyOnLoad( this );
    }

    public void Initialized( Song _data )
    {
        if ( !ReferenceEquals( curCoroutine, null ) ) StopCoroutine( curCoroutine );

        CurrentSong = _data;

        //List<BPMS> bpms = new List<BPMS>();
        //for( int i = 0; i < Data.timings.Count; i++  )
        //{
        //    bpms.Add( new BPMS( Data.timings[i].bpm, Data.timings[i].changeTime ) );
        //}

        //List<MedianCac> medianCalc = new List<MedianCac>();
        //for ( int i = 0; i < bpms.Count; i++ )
        //{
        //    float t;
        //    double b;
        //    if ( i == 0 )
        //    {
        //        t = 0;
        //        b = bpms[0].bpm;
        //    }
        //    else
        //    {
        //        t = bpms[i - 1].time;
        //        b = bpms[i - 1].bpm;
        //    }
        //    bool find = false;
        //    for ( int j = 0; j < medianCalc.Count; j++ )
        //    {
        //        if ( Mathf.Abs( ( float )( b - medianCalc[j].bpm ) ) < 0.1f )
        //        {
        //            find = true;
        //            medianCalc[j].time += bpms[i].time - t;
        //        }
        //    }
        //    if ( !find ) medianCalc.Add( new MedianCac( bpms[i].time - t, (float)b ) );
        //}

        //for ( int i = 0; i < medianCalc.Count; i++ )
        //    if ( medianCalc[i].bpm <= 30f ) medianCalc.RemoveAt( i ); //�ʹ� ���� ��ġ�Ͻ� �������

        //medianCalc.Sort( delegate ( MedianCac A, MedianCac B )
        //{

        //    if ( A.time >= B.time ) return -1;
        //    else return 1;
        //}
        //);
        //MedianBpm = 1 / ( ( float )medianCalc[0].bpm / 60000f );

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

        //SoundManager.Inst.LoadAndPlay( Data.audioPath );
        //EndTime  = SoundManager.Inst.Length;
        StartCoroutine( TimeUpdate() );
    }

    private IEnumerator BpmChange()
    {
        //BPM = Data.timings[0].bpm;
        //BPMChangeEvent();

        //while ( timingIdx < Data.timings.Count )
        //{
        //    float changeTime = Data.timings[timingIdx].changeTime;
        //    yield return new WaitUntil( () => Playback >= changeTime );

        //    BPM = Data.timings[timingIdx].bpm;
        //    BPMChangeEvent();
        //    timingIdx++;
        //}
        yield return null;
    }

    public static float GetChangedTime( float _time ) // BPM ��ȭ�� ���� �ð� ���
    {
        double newTime = _time;
        double prevBpm = 0d;
        //for ( int i = 0; i < Data.timings.Count; i++ )
        //{
        //    double time = Data.timings[i].changeTime;
        //    double bpm = Data.timings[i].bpm;

        //    if ( time > _time ) break;
        //    bpm = MedianBpm / bpm;
        //    newTime += ( bpm - prevBpm ) * ( _time - time );
        //    prevBpm = bpm;
        //}
        return ( float )newTime;
    }

    private IEnumerator TimeUpdate()
    {
        Playback = 0;
        timer.Start();
        //SoundManager.Inst.Position = 0;
        while ( Playback <= EndTime )
        {
            Playback = timer.elapsedMilliSeconds;
            //Playback += Time.deltaTime * 1000f;
            PlaybackChanged = GetChangedTime( Playback );
            yield return null;
        }

        SoundManager.Inst.AllStop();
    }
}
