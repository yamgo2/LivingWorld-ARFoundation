using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class PlaneClickHandler : MonoBehaviour
{
    public DataManager dataManager;

    public AudioClip enterSelectionAudioClip;

    public AudioClip exitSelectionAudioClip;

    public AudioSource AudioManager;

    public VideoPlayer videoPlayer;

    public Image playIcon;

    public GameObject spinner;

    public bool wasPlaying;

    public float videoAspectRatioWidth = 1;

    public float videoAspectRatioHeight = 1;

    public string videoName;

    public ObjectShake bgShake;

    private ImageVideo lastPlayedVideo;

    private ImageVideo mostLikedVideo;

    public void ResetLastPlayVideo()
    {
        lastPlayedVideo = null;
    }

    // Only play video after VideoPlayer is prepared
    private IEnumerator VideoPlayCoroutine(string url)
    {
        // Show loading spinner until video is prepared
        spinner.SetActive(true);

        videoPlayer.url = url;

        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        videoPlayer.Play();

        spinner.SetActive(false);
    }

    // Called when image or background plane is clicked
    public void PlaneClicked()
    {
        // Check if video player is enabled or not
        if (!videoPlayer.enabled)
        {
            AudioManager.PlayOneShot(enterSelectionAudioClip);
            DisablePlayIcon();

            // If a video was already playing on the plane, play that video, otherwise load the highest rated video
            if (lastPlayedVideo != null)
            {
                StartCoroutine(dataManager.DisableLastActiveVideoPlayer(returnValue =>
                {
                    if (returnValue == "finished")
                        NewVideoPlayer(lastPlayedVideo);
                }));
            }
            else
            {
                // Check if local mode, otherwise get highest rated video from server
                if (dataManager.SessionData.localMode)
                {
                    mostLikedVideo = dataManager.GetImageData(gameObject.name).imageVideos.OrderByDescending(x => x.likes).ToList()[0];
                    StartCoroutine(dataManager.DisableLastActiveVideoPlayer(returnValue =>
                    {
                        if (returnValue == "finished")
                            NewVideoPlayer(mostLikedVideo);
                    }));
                }
                else
                {
                    // Get highest rated video from server
                    StartCoroutine(dataManager.RequestHandler($"{dataManager.apiUrl}/images/{gameObject.name}/videos", "GET", "", (response, errorMessage) =>
                    {
                        if (response.Equals("failure"))
                        {
                            videoPlayer.Pause();
                            EnablePlayIcon();
                        }
                        else
                        {
                            ImageVideoList videos = JsonUtility.FromJson<ImageVideoList>("{\"imageVideos\":" + response + "}");
                            mostLikedVideo = videos.imageVideos.OrderByDescending(x => x.likes).ToList()[0];
                            StartCoroutine(dataManager.DisableLastActiveVideoPlayer(returnValue =>
                            {
                                if (returnValue == "finished")
                                    NewVideoPlayer(mostLikedVideo);
                            }));
                        }
                    }));
                }
            }
        }
        else
        {
            // If VideoPlayer is enabled and currently playing, pause video on click
            if (videoPlayer.isPlaying)
            {
                if (bgShake != null)
                    bgShake.Shake(0.15f);

                AudioManager.PlayOneShot(exitSelectionAudioClip);
                videoPlayer.Pause();
                wasPlaying = false;
                EnablePlayIcon();
            }
            // If video is paused, play video
            else
            {
                if (bgShake != null)
                    bgShake.Shake(0.15f);

                AudioManager.PlayOneShot(enterSelectionAudioClip);
                videoPlayer.Play();
                wasPlaying = true;
                DisablePlayIcon();
            }
        }
    }

    public void NewVideoPlayer(ImageVideo video)
    {
        // Set clicked plane as the last active plane
        dataManager.lastActivePlane = gameObject;

        // New videos are played automatically, so wasPlaying is true
        wasPlaying = true;

        // Set public variables that are accessed by TrackedImageInforManager
        videoAspectRatioWidth = video.width;
        videoAspectRatioHeight = video.height;
        videoName = video.title;

        videoPlayer.enabled = true;

        // Remmeber last played video so that it can be played instead of highest rated video
        lastPlayedVideo = video;

        StartCoroutine(VideoPlayCoroutine(dataManager.GetCutVideoLink(video.link)));
    }

    public void DisablePlayIcon()
    {
        playIcon.enabled = false;
    }

    public void EnablePlayIcon()
    {
        playIcon.enabled = true;
    }
}
