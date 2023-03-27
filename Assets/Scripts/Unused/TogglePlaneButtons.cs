using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TogglePlaneButtons : MonoBehaviour
{
    public GameObject buttonBackground;
    public Image toogleImage;
    public Sprite open;
    public Sprite close;
    private bool toggled = false;

    public void Toggle()
    {
        if (!toggled)
        {
            buttonBackground.SetActive(true);
            toogleImage.sprite = close;
            toggled = true;
        }
        else
        {
            buttonBackground.SetActive(false);
            toogleImage.sprite = open;
            toggled = false;
        }
    }
}
