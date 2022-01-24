using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaneEffect : MonoBehaviour
{
    private InputSystem inputSystem;
    private SpriteRenderer rdr;
    private Color color; 
    
    private void Awake()
    {
        rdr = GetComponent<SpriteRenderer>();
        inputSystem = GetComponent<InputSystem>();
        inputSystem.OnInputEvent += LaneEffectEnabled;

        transform.localScale = new Vector3( GameSetting.NoteWidth, ( Screen.height * .5f ) - GameSetting.JudgePos, 1f );
        color     = rdr.color;
        color.a   = .3f;
        rdr.color = Color.clear;
    }

    private void LaneEffectEnabled( bool _isEnable )
    {
        // sprite renderer Enable�� Ȱ��ȭ ��Ű�°ͺ���
        // color �� �����ϴ°� 6������ ����.

        // laneEffect.enabled = _isEnable;
        rdr.color = _isEnable ? color : Color.clear;
    }
}
