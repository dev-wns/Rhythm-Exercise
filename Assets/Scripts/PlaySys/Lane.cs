using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lane : MonoBehaviour
{
    public int Key { get; private set; }
    public NoteSystem  NoteSys  { get; private set; }
    public InputSystem InputSys { get; private set; }

    private SpriteRenderer laneEffect;
    private static readonly Color LaneColorRed  = new Color( 1f, 0f, 0f, .25f );
    private static readonly Color LaneColorBlue = new Color( 0f, 0f, 1f, .25f );

    private Color color;

    private void Awake()
    {
        NoteSys  = GetComponent<NoteSystem>();
        InputSys = GetComponent<InputSystem>();

        laneEffect = GetComponent<SpriteRenderer>();
    }

    private void LaneEffectEnabled( bool _isEnable )
    {
        // sprite renderer Enable�� Ȱ��ȭ ��Ű�°ͺ���
        // color �� �����ϴ°� 6������ ����.

        // laneEffect.enabled = _isEnable;
        laneEffect.color = _isEnable ? color : Color.clear;
    }

    public void SetLane( int _key )
    {
        Key = _key;
        NoteSys.lane = InputSys.lane = this;
        InputSys.OnInputEvent += LaneEffectEnabled;

        if ( Key == 1 || Key == 4 ) color = LaneColorBlue;
        else                        color = LaneColorRed;

        transform.position = new Vector3( GameSetting.NoteStartPos + ( GameSetting.NoteWidth * Key ) +
                                        ( GameSetting.NoteBlank * Key ) + GameSetting.NoteBlank,
                                          GameSetting.JudgePos, 0f );

        transform.localScale = new Vector3( GameSetting.NoteWidth, ( Screen.height * .5f ) - GameSetting.JudgePos, 1f );
    }
}
