using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSystem : MonoBehaviour
{
    public InGame CurrentScene { get; private set; }
    private Lane lane;

    private ObjectPool<NoteRenderer> nPool;
    public NoteRenderer nPrefab;

    private List<Note> notes = new List<Note>();
    private Note currentNote;
    private int currentIndex;

    private void Awake()
    {
        lane = GetComponent<Lane>();
        CurrentScene = GameObject.FindGameObjectWithTag( "Scene" ).GetComponent<InGame>();
        CurrentScene.OnGameStart += () => StartCoroutine( Process() );

        nPool = new ObjectPool<NoteRenderer>( nPrefab, 10 );
    }

    public void AddNote( Note _note ) => notes.Add( _note );

    public void Despawn( NoteRenderer _note ) => nPool.Despawn( _note );

    private IEnumerator Process()
    {
        float slidertime = 0f;
        float notetime = 0f;

        if( notes.Count > 0 )
            currentNote = notes[currentIndex];

        while ( currentIndex < notes.Count )
        {
            if ( currentNote.calcTime <= NowPlaying.PlaybackChanged + GameSetting.PreLoadTime )
            {
                if ( currentNote.isSlider ) slidertime = currentNote.sliderTime;

                if ( currentIndex + 1 < notes.Count && ( slidertime > notes[currentIndex + 1].time ||
                                                         notetime == notes[currentIndex + 1].time ) )
                {
                    Debug.LogError( "overlab " );
                }

                NoteRenderer note = nPool.Spawn();
                note.SetInfo( lane.Key, this, in currentNote );

                lane.InputSys.Enqueue( note );

                if ( ++currentIndex < notes.Count )
                     currentNote = notes[currentIndex];
            }

            yield return null;
        }
    } 
}
