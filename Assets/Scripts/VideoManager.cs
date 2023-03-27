using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoControl : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public VideoPlayer player;

    public Image progress;

    public Camera worldCamera;

    public RectTransform videoControlRect;

    public AudioSource audioManager;
    public AudioClip clickUp;

    public NotificationLogic ntfLogic;

    private bool wasPlaying;

    // Update video progress every frame
    void Update()
    {
        if (player.frameCount > 0)
            progress.fillAmount = (float)player.frame / (float)player.frameCount;
    }
    public void OnDrag(PointerEventData eventData)
    {
    }
    public void OnPointerDown(PointerEventData eventData)
    {
    }

    // Progress bar clicked
    public void OnPointerUp(PointerEventData eventData)
    {
        audioManager.PlayOneShot(clickUp);
        TrySkip(eventData);
    }

    private void TrySkip(PointerEventData eventData)
    {
        Vector2 localPoint;
        // Check if the screen point of the pointer event can be converted to a local point on the video control rectangle using the world camera and assign the resulting point
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(videoControlRect, eventData.position, worldCamera, out localPoint))
        {
            // Calculate the percentage of progress based on the local point on the progress bar using the InverseLerp method
            float pct = Mathf.InverseLerp(progress.rectTransform.rect.xMin, progress.rectTransform.rect.xMax, localPoint.x);
            StartCoroutine(SkipToPercent(pct));
        }
    }

    // Skip to frame in video based on given percent
    private IEnumerator SkipToPercent(float pct)
    {
        player = gameObject.transform.parent.GetChild(0).GetChild(0).GetComponent<VideoPlayer>();

        yield return null;

        player.Prepare();

        while (!player.isPrepared)
        {
            yield return null;
        }

        wasPlaying = player.isPlaying;

        var frame = player.frameCount * pct;
        player.Pause();

        // Potential workaround for application crash
        player.GetComponent<MeshRenderer>().enabled = false;
        player.frame = (long)frame;

        yield return null;

        player.Prepare();

        while (!player.isPrepared)
        {
            yield return null;
        }

        player.GetComponent<MeshRenderer>().enabled = true;
        if (wasPlaying)
        {
            player.Play();
        }
    }
}

