using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectContentOrientationChangeHandler : MonoBehaviour
{
    private DeviceOrientation lastOrientation;

    public RectTransform listViewRect;

    // Start is called before the first frame update
    void Start()
    {
        lastOrientation = Input.deviceOrientation;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.deviceOrientation != lastOrientation)
        {
            if (Input.deviceOrientation == DeviceOrientation.Portrait || Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown)
            {
                listViewRect.SetBottom(500);
            }
            else if (Input.deviceOrientation == DeviceOrientation.LandscapeLeft || Input.deviceOrientation == DeviceOrientation.LandscapeRight)
            {
                listViewRect.SetBottom(175);
            }
            lastOrientation = Input.deviceOrientation;
        }
    }
}
