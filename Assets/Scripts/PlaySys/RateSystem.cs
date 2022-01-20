using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RateSystem : NumberAtlasBase
{
    [Header( "System" )]
    private Judgement judge;

    private int maxCount;
    private float currentRate;

    protected override void Awake()
    {
        base.Awake();
        judge = GameObject.FindGameObjectWithTag( "Judgement" ).GetComponent<Judgement>();
        judge.OnJudge += RateImageUpdate;
    }

    private void RateImageUpdate( JudgeType _type )
    {
        if ( _type == JudgeType.None ) return;

        int addRate = 0;
        switch ( _type )
        {
            case JudgeType.Kool: addRate = 10000; break; // 100%
            case JudgeType.Cool: addRate = 9000;  break; // 90%
            case JudgeType.Good: addRate = 8000;  break; // 80%
            case JudgeType.Bad:  addRate = 7000;  break; // 70%
            case JudgeType.Miss: addRate = 0;     break; // 0%
        }
        ++maxCount;

        currentRate += addRate;

        float calcRate = currentRate / maxCount;
        int num = ( int )Mathf.Log10( calcRate ) + 1;

        for ( int i = 0; i < images.Count; i++ )
        {
            if ( i > 2 && i >= num )
            {
                if ( images[i].gameObject.activeInHierarchy )
                     images[i].gameObject.SetActive( false );
            }
            else
            {
                if ( !images[i].gameObject.activeInHierarchy )
                     images[i].gameObject.SetActive( true );

                images[i].sprite = sprites[( int )calcRate % 10];
                calcRate *= .1f;
            }
        }
    }
}
