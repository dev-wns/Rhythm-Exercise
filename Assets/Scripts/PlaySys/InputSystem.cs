using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputSystem : MonoBehaviour
{
    private Lane lane;
    private InGame scene;
    private Judgement judge;

    private Queue<NoteRenderer> notes = new Queue<NoteRenderer>();
    private NoteRenderer curNote;

    private Queue<NoteRenderer> sliderMissQueue = new Queue<NoteRenderer>();
    public event Action<bool> OnInputEvent;
    public event Action<NoteType, bool/*Key Up*/> OnHitNote;

    private GameKeyAction key;
    private float playback;

    private bool isAuto, isReady;

    private KeySound curSound;

    public void Enqueue( NoteRenderer _note ) => notes.Enqueue( _note );
    public void SetSound( in KeySound _sound ) => curSound = _sound;

    private void Awake()
    {
        scene = GameObject.FindGameObjectWithTag( "Scene" ).GetComponent<InGame>();
        scene.OnGameStart += () => StartCoroutine( NoteSelect() );
        scene.OnReLoad += ReLoad;
        NowPlaying.Inst.OnPause += DuringPauseProcess;

        judge = GameObject.FindGameObjectWithTag( "Judgement" ).GetComponent<Judgement>();

        lane  = GetComponent<Lane>(); 
        lane.OnLaneInitialize += Initialize;

        isReady = false;
        isAuto = GameSetting.CurrentGameMode.HasFlag( GameMode.AutoPlay );
    }

    public void Initialize( int _key )
    {
        key = ( GameKeyAction )_key; 
        isReady = true;
    }
    private void ReLoad()
    {
        StopAllCoroutines();
        playback = 0f;
        while ( sliderMissQueue.Count > 0 )
        {
            var note = sliderMissQueue.Dequeue();
            note.Despawn();
        }

        while ( notes.Count > 0 )
        {
            var note = notes.Dequeue();
            note.Despawn();
        }

        curNote?.Despawn();
        curNote = null;
    }

    /// <summary>
    /// process the slider when pausing, it will be judged immediately.
    /// </summary>
    private void DuringPauseProcess( bool _isPause )
    {
        if ( !_isPause || curNote == null || !curNote.IsSlider || !curNote.IsPressed ) 
             return;

        if ( isAuto )
        {
            OnHitNote?.Invoke( NoteType.Default, false );
            judge.ResultUpdate( HitResult.Perfect );
            SelectNextNote();
        }
        else
        {
            OnHitNote?.Invoke( NoteType.Default, false );
            judge.ResultUpdate( curNote.SliderTime - NowPlaying.Playback );
            SelectNextNote();
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        NowPlaying.Inst.OnPause -= DuringPauseProcess;
    }

    /// <summary>
    /// Find the next note in the current lane.
    /// </summary>
    /// <returns></returns>
    private IEnumerator NoteSelect()
    {
        var WaitNote = new WaitUntil( () => curNote == null && notes.Count > 0 );
        while ( true )
        {
            yield return WaitNote;
            curNote = notes.Dequeue();
            curSound = curNote.Sound;
        }
    }

    private void SelectNextNote( bool _isDespawn = true )
    {
        playback = 0f;

        if ( _isDespawn )
        {
            curNote.gameObject.SetActive( false );
            curNote.Despawn();
        }
        curNote = null;
    }

    private void CheckNote()
    {
        if ( curNote == null ) return;

        double startDiff = curNote.Time - NowPlaying.Playback;
        if ( isAuto )
        {
            if ( startDiff <= 0d )
            {
                OnHitNote?.Invoke( NoteType.Default, false );
                judge.ResultUpdate( startDiff );
                SoundManager.Inst.Play( curSound );
                SelectNextNote();
            }
        }
        else
        {
            if ( judge.CanBeHit( startDiff ) && Input.GetKeyDown( GameSetting.Inst.Keys[key] ) )
            {
                OnHitNote?.Invoke( NoteType.Default, false );
                judge.ResultUpdate( startDiff );
                SelectNextNote();
                return;
            }

            if ( judge.IsMiss( startDiff ) )
            {
                judge.ResultUpdate( HitResult.Miss );
                SelectNextNote();
            }
        }
    }

    private void CheckSlider()
    {
        if ( curNote == null ) return;

        double startDiff = curNote.Time       - NowPlaying.Playback;
        double endDiff   = curNote.SliderTime - NowPlaying.Playback;
        if ( isAuto )
        {
            if ( !curNote.IsPressed )
            {
                if ( startDiff <= 0d )
                {
                    curNote.IsPressed = true;
                    OnHitNote?.Invoke( NoteType.Slider, false );
                    SoundManager.Inst.Play( curSound );
                    judge.ResultUpdate( startDiff );
                }
            }
            else
            {
                if ( endDiff <= 0d )
                {
                    OnHitNote?.Invoke( NoteType.Slider, true );
                    judge.ResultUpdate( endDiff );
                    SelectNextNote();
                }

                playback += Time.deltaTime;
                if ( playback > .1f )
                {
                    judge.ResultUpdate( HitResult.None );
                    playback = 0f;
                }
            }
        }
        else
        {
            if ( !curNote.IsPressed )
            {
                if ( judge.CanBeHit( startDiff ) && Input.GetKeyDown( GameSetting.Inst.Keys[key] ) )
                {
                    curNote.IsPressed = true;
                    OnHitNote?.Invoke( NoteType.Slider, false );
                    judge.ResultUpdate( startDiff );
                    return;
                }

                if ( judge.IsMiss( startDiff ) )
                {
                    curNote.SetBodyFail();
                    judge.ResultUpdate( HitResult.Miss );
                    sliderMissQueue.Enqueue( curNote );
                    SelectNextNote( false );
                }
            }
            else
            {
                if ( Input.GetKey( GameSetting.Inst.Keys[key] ) )
                {
                    if ( endDiff <= 0d )
                    {
                        curNote.IsPressed = false;
                        judge.ResultUpdate( endDiff );
                        OnHitNote?.Invoke( NoteType.Slider, true );
                        SelectNextNote();
                        return;
                    }

                    playback += Time.deltaTime;
                    if ( playback > .1f )
                    {
                        judge.ResultUpdate( HitResult.None );
                        playback = 0f;
                    }
                }

                if ( Input.GetKeyUp( GameSetting.Inst.Keys[key] ) )
                {
                    OnHitNote?.Invoke( NoteType.Slider, true );

                    if ( judge.CanBeHit( endDiff ) )
                    {
                        judge.ResultUpdate( endDiff );
                        SelectNextNote();
                    }
                    else
                    {
                        curNote.IsPressed = false;
                        curNote.SetBodyFail();
                        judge.ResultUpdate( HitResult.Miss );
                        sliderMissQueue.Enqueue( curNote );
                        SelectNextNote( false );
                    }
                }
            }
        }
    }

    private void LateUpdate()
    {
        if ( !isReady )
            return;

        if ( sliderMissQueue.Count > 0 )
        {
            var note = sliderMissQueue.Peek();
            if ( judge.IsMiss( note.SliderTime - NowPlaying.Playback ) )
            {
                note.Despawn();
                sliderMissQueue.Dequeue();
            }
        }

        if ( !isAuto )
        {
            if ( Input.GetKeyDown( GameSetting.Inst.Keys[key] ) )
            {
                OnInputEvent?.Invoke( true );
                SoundManager.Inst.Play( curSound );
            }
            else if ( Input.GetKeyUp( GameSetting.Inst.Keys[key] ) )
            {
                OnInputEvent?.Invoke( false );
            }
        }

        if ( curNote != null )
        {
            if ( curNote.IsSlider ) CheckSlider();
            else CheckNote();
        }
    }
}
