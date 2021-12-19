using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lobby : Scene
{
    public ParticleSystem particle;

    protected override void Awake()
    {
        base.Awake();

        SoundManager.Inst.Load( System.IO.Path.Combine( Application.streamingAssetsPath, "Default", "Sylrica  Sweet Victory.mp3" ), 
                                Sound.LoadType.Default, Sound.Mode.Loop );
        SoundManager.Inst.Play();
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void KeyBind()
    {
        StaticSceneKeyAction scene = new StaticSceneKeyAction();
        scene.Bind( KeyCode.Return, KeyType.Down, () => particle.gameObject.SetActive( false ) );
        scene.Bind( KeyCode.Return, KeyType.Down, () => SceneChanger.Inst.LoadScene( SceneType.FreeStyle ) );

        keyAction.Bind( SceneAction.Lobby, scene );
        keyAction.ChangeAction( SceneAction.Lobby );
    }
}
