using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingPage_Manager : DefaultPage_Manager
{
    public Slider BgmVolumeSlider, SfxVolumeSlider;
    public Toggle fullScreenToggle;

    protected override void Start()
    {
        base.Start();

        if (BgmVolumeSlider != null)
        {
            BgmVolumeSlider.value = Audio.bgmVolumeMult * BgmVolumeSlider.maxValue;
        }
        if (SfxVolumeSlider != null)
        {
            SfxVolumeSlider.value = Audio.sfxVolumeMult * SfxVolumeSlider.maxValue;
        }
        if(fullScreenToggle != null)
        {
            fullScreenToggle.isOn = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;
        }
    }

    public void OnChangeBgmVolumeSlider()
    {
        float Vol = BgmVolumeSlider.value / BgmVolumeSlider.maxValue;
        Audio.SetBgmVolumeMult(Vol);
    }

    // SFXÒôÁ¿»Øµ÷
    public void OnChangeSfxVolumeSlider()
    {
        float Vol = SfxVolumeSlider.value / SfxVolumeSlider.maxValue;
        Audio.SetSfxVolumeMult(Vol);
    }

    public void OnChangeFullScreen()
    {
        system_Controller.SetFullScreen(fullScreenToggle.isOn);
    }
}
