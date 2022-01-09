using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum OPTION_BOOL { Off, On, Count }

public enum ALIGNMENT
{
    Left, Center, Right, Count,
}

public enum MOD
{
    None,
    Mirror,
    Random,
    Half_Random,
    Max_Random,
    Count,
}
public enum FADER
{
    None,
    Fade_In,
    Fade_Out,
    Count,
}

public class GameSetting : MonoBehaviour
{
    public static MOD GameMod             = MOD.None;
    public static FADER GameFader         = FADER.None;
    public static ALIGNMENT GearAlignment = ALIGNMENT.Center;

    private static int OriginScrollSpeed = 25;
    public static float ScrollSpeed
    {
        get { return OriginScrollSpeed * .0015f; }
        set
        {
            var speed = OriginScrollSpeed + Mathf.FloorToInt( value );
            if ( speed <= 1 )
            {
                Debug.Log( $"ScrollSpeed : {OriginScrollSpeed}" );
                return;
            }

            OriginScrollSpeed = speed;
            Debug.Log( $"ScrollSpeed : {OriginScrollSpeed}" );
        }
    }

    public static float PreLoadTime { get { return ( 1250f / Weight ); } }
    
    // 60bpm�� �д� 1/4���� 60��, ��ũ�� �ӵ��� 1�϶� �ѹ���(1/4) �ð��� 1��
    public static float Weight { get { return ( 60f / NowPlaying.Inst.CurrentSong.medianBpm ) * ScrollSpeed; } }


    public static float SoundPitch = 1f;

    public static float JudgePos = -540f;

    public static float BGAOpacity = 0f;
    public static float PanelOpacity = 0f;

    public static bool IsBGAPlay       = true;
    public static bool IsTouchEffect   = true;
    public static bool IsLineEffect    = true;
    public static bool IsCreateMeasure = true;
}
