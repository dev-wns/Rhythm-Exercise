using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSystem : MonoBehaviour
{
    private InGame scene;

    // 60bpm�� �д� 1/4���� 60��, ��ũ�� �ӵ��� 1�϶� �ѹ���(1/4) �ð��� 1��
    private ObjectPool<NoteRenderer> nPool;
    public NoteRenderer nPrefab;

    private List<Note> notes = new List<Note>();
    private int currentIndex;

    private InputSystem inputSystem;

    private void Awake()
    {
        scene   = GameObject.FindGameObjectWithTag( "Scene" ).GetComponent<InGame>();
        nPool   = new ObjectPool<NoteRenderer>( nPrefab, 10 );
        inputSystem = GetComponent<InputSystem>();

        scene.OnGameStart += () => StartCoroutine( Process() );
    }

    public void AddNote( Note _note ) => notes.Add( _note );

    public void Despawn( NoteRenderer _note ) => nPool.Despawn( _note );

    private IEnumerator Process()
    {
        while ( currentIndex < notes.Count )
        {
            Note curNote = notes[currentIndex];
            yield return new WaitUntil( () => curNote.calcTime <= NowPlaying.PlaybackChanged + GameSetting.PreLoadTime );

            NoteRenderer note = nPool.Spawn();
            note.SetInfo( curNote );
            inputSystem.Enqueue( note );
            currentIndex++;
        }
    } 
}
