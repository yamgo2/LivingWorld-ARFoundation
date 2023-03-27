using UnityEngine;
using UnityEngine.UI;

public class SettingsLogic : MonoBehaviour
{
    public GameObject settingsScreen;
    public Toggle stretchToggle;
    public Toggle holoToggle;
    public DataManager dataManager;
    public Animator confirmButtonAnimator;
    public Animator cancelButtonAnimator;

    // Enable settings screen, reset button animations and set toggle status
    public void ShowSettingsScreen()
    {
        settingsScreen.SetActive(true);
        cancelButtonAnimator.Play("Normal", 0, 0f);
        confirmButtonAnimator.Play("Normal", 0, 0f);
        stretchToggle.isOn = dataManager.SessionData.stretchVideos;
        holoToggle.isOn = dataManager.SessionData.holoMode;
    }

    // Set settings based on toggles
    public void ConfirmSettings()
    {
        dataManager.SessionData.stretchVideos = stretchToggle.isOn;
        dataManager.SessionData.holoMode = holoToggle.isOn;
        dataManager.SaveJsonSessionData();
        CloseSettingsScreen();
    }

    public void CloseSettingsScreen()
    {
        settingsScreen.SetActive(false);
    }
}
