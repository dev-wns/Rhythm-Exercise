using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InGame : Scene
{
    // ui
    public TextMeshProUGUI timeText, comboText, frameText;

    public delegate void DelSystemInitialize( in Chart _chart );
    public event DelSystemInitialize OnSystemInitialize;

    public delegate void DelGameStart();
    public event DelGameStart OnGameStart;

    public delegate void DelSpeedChanged();
    public event DelSpeedChanged OnScrollChanged;

    float delta;

    private void Start()
    {
        OnSystemInitialize( NowPlaying.Inst.CurrentChart );
        OnGameStart();

        NowPlaying.Inst.Play();
    }

    protected override void Update()
    {
        base.Update();

        timeText.text = string.Format( "{0:F1} ��", NowPlaying.Playback * 0.001f );
        delta += ( Time.unscaledDeltaTime - delta ) * .1f;
        frameText.text = string.Format( "{0:F1}", 1f / delta );

        //comboText.text = string.Format( "{0}", GameManager.Combo );
    }

    public override void KeyBind()
    {
        Bind( SceneAction.Main, KeyCode.Escape, () => SceneChanger.Inst.LoadScene( SceneType.FreeStyle ) );

        Bind( SceneAction.Main, KeyCode.Alpha1, () => GameSetting.ScrollSpeed -= 1 );
        Bind( SceneAction.Main, KeyCode.Alpha1, () => OnScrollChanged?.Invoke() );

        Bind( SceneAction.Main, KeyCode.Alpha2, () => GameSetting.ScrollSpeed += 1 );
        Bind( SceneAction.Main, KeyCode.Alpha2, () => OnScrollChanged?.Invoke() );
    }
}
