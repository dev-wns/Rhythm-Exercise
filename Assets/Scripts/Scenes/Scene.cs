using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;


// Build Index
public enum SceneType : int { Lobby = 1, FreeStyle, Game, Result };

[RequireComponent( typeof( SpriteRenderer ) )]
public abstract class Scene : SceneKeyAction, IKeyBind
{
    public event Action OnScrollChanged;

    private bool isPressed = false;
    private float pressWaitTime = .5f;
    private float pressUpdateTime = .05f;
    private float presstime;

    private SpriteRenderer blackSprite;
    private readonly float FadeTime = .65f;

    #region Unity Callback
    protected virtual void Awake()
    {
        Cursor.visible = false;

        CreateFadeSprite();
        Camera.main.orthographicSize = ( Screen.height / ( GameSetting.PPU * 2f ) ) * GameSetting.PPU;
        
        KeyBind();

        NowPlaying.CurrentScene = this;
        ChangeAction( SceneAction.Main );
    }

    protected virtual void Start()
    {
        StartCoroutine( FadeIn() );
    }

    protected virtual void Update() => ActionCheck();
    #endregion

    #region Scene Load
    public void LoadScene( SceneType _type )
    {
        StopAllCoroutines();
        StartCoroutine( SceneChange( _type ) );
    }

    private IEnumerator SceneChange( SceneType _type )
    {
        DOTween.KillAll();
        InputLock( true );
        
        yield return StartCoroutine( FadeOut() );

        SoundManager.Inst.AllStop();
        SceneManager.LoadScene( ( int )_type );

        //AsyncOperation oper = SceneManager.LoadSceneAsync( ( int )_type );
        //if ( !oper.isDone ) yield return null;
    }
    #endregion

    #region Fade
    private void CreateFadeSprite()
    {
        //gameObject.layer = 6; // 3d

        Texture2D tex = Texture2D.whiteTexture;
        blackSprite = GetComponent<SpriteRenderer>();
        blackSprite.sprite = Sprite.Create( tex, new Rect( 0f, 0f, tex.width, tex.height ), new Vector2( .5f, .5f ), GameSetting.PPU, 0, SpriteMeshType.FullRect );

        blackSprite.drawMode = SpriteDrawMode.Sliced;
        blackSprite.size = new Vector2( 10000, 10000 );
        blackSprite.sortingOrder = 100;

        transform.localScale = Vector3.one;
    }

    private IEnumerator FadeIn()
    {
        blackSprite.color = Color.black;
        blackSprite.enabled = true;
        blackSprite.DOFade( 0f, FadeTime );
        yield return YieldCache.WaitForSeconds( FadeTime + .1f );
        blackSprite.enabled = false;
    }

    private IEnumerator FadeOut()
    {
        blackSprite.color = Color.clear;
        blackSprite.enabled = true;
        blackSprite.DOFade( 1f, FadeTime );
        yield return YieldCache.WaitForSeconds( FadeTime + .1f );
    }
    #endregion

    #region Input
    public abstract void KeyBind();

    protected void PressdSpeedControl( bool _isPlus )
    {
        presstime += Time.deltaTime;
        if ( presstime >= pressWaitTime )
            isPressed = true;

        if ( isPressed && presstime >= pressUpdateTime )
        {
            presstime = 0f;
            SpeedControlProcess( _isPlus );
        }
    }

    protected void UpedSpeedControl()
    {
        presstime = 0f;
        isPressed = false;
    }

    protected void SpeedControlProcess( bool _isPlus )
    {
        if ( _isPlus )
        {
            SoundManager.Inst.Play( SoundSfxType.Slider );
            GameSetting.ScrollSpeed += .1d;
        }
        else
        {
            if ( GameSetting.ScrollSpeed > 1.0001d )
                 SoundManager.Inst.Play( SoundSfxType.Slider );

            GameSetting.ScrollSpeed -= .1d;
        }

        OnScrollChanged?.Invoke();
    }
    #endregion
}
