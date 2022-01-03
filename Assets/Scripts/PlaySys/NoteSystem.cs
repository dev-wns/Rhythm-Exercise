using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSystem : MonoBehaviour
{
    private InGame scene;

    // 60bpm�� �д� 1/4���� 60��, ��ũ�� �ӵ��� 1�϶� �ѹ���(1/4) �ð��� 1��
    public ObjectPool<NoteRenderer> nPool;
    public NoteRenderer nPrefab;

    private List<Note> notes = new List<Note>();
    private int curIdx;
    private InputSystem[] ISystem;

    private void Awake()
    {
        scene   = GameObject.FindGameObjectWithTag( "Scene" ).GetComponent<InGame>();
        nPool   = new ObjectPool<NoteRenderer>( nPrefab );
        ISystem = GetComponentsInChildren<InputSystem>();

        scene.SystemInitialized += Initialized;
        scene.StartGame += () => StartCoroutine( Process() );
    }

    private void Initialized( Chart _chart )
    {
        notes = _chart.notes;
    }

    private IEnumerator Process()
    {
        while ( curIdx < notes.Count )
        {
            Note curNote = notes[curIdx];
            yield return new WaitUntil( () => curNote.calcTime <= InGame.PlaybackChanged + InGame.PreLoadTime );

            NoteRenderer note = nPool.Spawn();
            note.Initialized( curNote );
            ISystem[curNote.line].notes.Enqueue( note );
            curIdx++;
        }
    } 
}
