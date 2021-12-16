using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InGame : Scene
{
    // ui
    public TextMeshProUGUI timeText, bpmText, comboText, frameText, medianText;

    public delegate void DelSystemInitialized( Chart _chart );
    public event DelSystemInitialized SystemInitialized;

    private bool isStart = false;

    public delegate void DelStartGame();
    public event DelStartGame StartGame;
    private Chart chart;

    float delta;

    // Bpm
    public static float BPM { get; private set; } // ���� BPM

    public static float PreLoadTime { get { return ( 1250f / GlobalSetting.ScrollSpeed ); } }
    // 60bpm�� �д� 1/4���� 60��, ��ũ�� �ӵ��� 1�϶� �ѹ���(1/4) �ð��� 1��
    public static float Weight { get { return .1f * GlobalSetting.ScrollSpeed; } }
    private static float MedianBpm;

    // time ( millisecond )
    public static float Playback { get; private set; } // �뷡 ��� �ð�
    public static float PlaybackChanged { get; private set; } // BPM ��ȭ�� ���� �뷡 ��� �ð�

    public static float GetChangedTime( float _time, Chart chart ) // BPM ��ȭ�� ���� �ð� ���
    {
        double newTime = _time;
        double prevBpm = 0d;
        for ( int i = 0; i < chart.timings.Count; i++ )
        {
            double time = chart.timings[i].time;
            double bpm = chart.timings[i].bpm;

            if ( time > _time ) break;
            bpm = bpm / chart.medianBpm;
            newTime += ( bpm - prevBpm ) * ( _time - time );
            prevBpm = bpm;
        }
        return ( float )newTime;
    }

    protected override void Awake()
    {
        base.Awake();

        Playback = 0f;

        Parser parser;
        switch( GlobalSoundInfo.CurrentSound.type )
        {
            case ParseType.Osu:
            {
                using ( parser = new OsuParser( GlobalSoundInfo.CurrentSound.filePath ) )
                    chart = parser.PostRead( GlobalSoundInfo.CurrentSound );
            } break;
            case ParseType.Bms:
            {
                using ( parser = new BmsParser( GlobalSoundInfo.CurrentSound.filePath ) )
                    chart = parser.PostRead( GlobalSoundInfo.CurrentSound );
            } break;
        }
        MedianBpm = chart.medianBpm;
        SystemInitialized( chart );

        SoundManager.Inst.Load( GlobalSoundInfo.CurrentSound.audioPath );
        StartGame();
        isStart = true;
        SoundManager.Inst.Play();
    }

    protected override void Update()
    {
        base.Update();

        if ( !isStart ) return;
        
        Playback += Time.deltaTime * 1000f;
        PlaybackChanged = GetChangedTime( Playback, chart );

        if ( Input.GetKeyDown( KeyCode.Escape ) ) { ChangeScene( SceneType.FreeStyle ); }

        timeText.text = string.Format( "{0:F1} ��", Playback * 0.001f );
        //comboText.text = string.Format( "{0}", GameManager.Combo );
        delta += ( Time.unscaledDeltaTime - delta ) * .1f;
        frameText.text = string.Format( "{0:F1}", 1f / delta );
        //medianText.text = string.Format( "{0:F1}", MedianBpm ); 
    }

    protected override void KeyBind()
    {
        StaticSceneKeyAction scene = new StaticSceneKeyAction();
        scene.Bind( KeyCode.Escape, KeyType.Down, () => ChangeScene( SceneType.FreeStyle ) );

        keyAction.Bind( SceneAction.InGame, scene );
        keyAction.ChangeAction( SceneAction.InGame );
    }
}
