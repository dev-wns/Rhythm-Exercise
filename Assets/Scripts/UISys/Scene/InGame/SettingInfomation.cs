using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SettingInfomation : MonoBehaviour
{
    private InGame game;
    public TextMeshProUGUI speed, offset, random, auto, noSlider, noFail;

    private void Awake()
    {
        game = GameObject.FindGameObjectWithTag( "Scene" ).GetComponent<InGame>();
        game.OnScrollChanged += () => speed.text = $"{GameSetting.ScrollSpeed:F1}";

        speed.text  = $"{GameSetting.ScrollSpeed:F1}";
        offset.text = $"{Globals.Round( GameSetting.SoundOffset )}";
        random.text = $"{GameSetting.CurrentRandom.ToString().Split( '_' )[0]}";
        
        string temp = ( GameSetting.CurrentGameMode & GameMode.AutoPlay ) != 0 ? "On" : "Off";
        auto.text = $"{temp}";

        temp = ( GameSetting.CurrentGameMode & GameMode.NoSlider ) != 0 ? "On" : "Off";
        noSlider.text = $"{temp}";

        temp = ( GameSetting.CurrentGameMode & GameMode.NoFail ) != 0 ? "On" : "Off";
        noFail.text = $"{temp}";
    }
}
