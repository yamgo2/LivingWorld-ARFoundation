using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotificationLogic : MonoBehaviour
{
    public TMP_Text notificationTitle;
    public TMP_Text notificationDescription;

    public Image errorIcon;

    public Animator sliderAnimator;

    private IEnumerator co;

    public void DisplayNotification(string title, string description, string type)
    {
        // Slide in
        sliderAnimator.Play("Out", 0, 0f);
        sliderAnimator.Play("In");

        // Switch statement to set icon color depending on notification type
        switch (type)
        {
            case "error":
                errorIcon.color = new Color32(220, 75, 75, 255);
                break;
            case "warning":
                errorIcon.color = new Color32(255, 235, 80, 255);
                break;
            case "information":
                errorIcon.color = Color.white;
                break;
            default:
                errorIcon.color = Color.white;
                break;
        }

        notificationTitle.text = title;
        notificationDescription.text = description;

        // Stop current notification animator if new notification pops up
        if (co != null)
            StopCoroutine(co);

        co = SlideOutAfterDelay();
        StartCoroutine(co);
    }

    public IEnumerator SlideOutAfterDelay()
    {
        yield return new WaitForSeconds(4);
        sliderAnimator.Play("Out");
    }
}