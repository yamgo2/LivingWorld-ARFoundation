using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class SelectContentLogic : MonoBehaviour
{
    public GameObject selectContentScreen;

    public GameObject loadingScreen;
    public TMP_Text loadingMessage;

    public DataManager dataManager;

    public NotificationLogic notificationLogic;

    public GameObject listContentPrefab;

    public GameObject listItemPrefab;

    public GameObject selectionInformation;

    public GameObject selectionInformationButton;

    public TMP_Text informationVideoName;
    public TMP_Text informationVideoLink;
    public TMP_Text informationVideoCreator;

    public GameObject deleteButton;
    public GameObject likeButton;

    public Image deleteButtonIcon;
    public Image likeButtonIcon;

    public Sprite spriteVideo;
    public Sprite likeOutline;
    public Sprite likeFilled;

    public Animator cancelButtonAnimator;
    public Animator confirmButtonAnimator;

    public Image confirmButtonBackgound;

    public AudioSource audioManager;

    public AudioClip deleteFirst;
    public AudioClip deleteSecond;

    public Image videoProgressBar;

    private bool deletePressedOnce = false;

    private GameObject activeSelection;

    private VideoPlayer activeVideoPlayer;

    private GameObject activePlane;

    private ReferenceImage activeImage;

    private GameObject activeContentParent;

    private ImageVideo selectedVideo;

    private bool liked;

    public void ShowImageContent(GameObject clickedImagePlane)
    {
        selectContentScreen.SetActive(true);

        // Fix stuck button animation
        cancelButtonAnimator.Play("Normal", 0, 0f);
        confirmButtonAnimator.Play("Normal", 0, 0f);
        confirmButtonBackgound.color = new Color32(255, 255, 255, 0);

        // Set active clicked image, then load content
        activeImage = dataManager.GetImageData(clickedImagePlane.name);
        activeVideoPlayer = clickedImagePlane.GetComponent<VideoPlayer>();
        activePlane = clickedImagePlane;
        activeContentParent = clickedImagePlane.transform.parent.parent.gameObject;
        LoadImageContentList();
    }

    public void LoadImageContentList()
    {
        // Get content list from local json or from server
        if (dataManager.SessionData.localMode)
        {
            activeImage.imageVideos = activeImage.imageVideos.OrderByDescending(x => x.likes).ToList();
            foreach (ImageVideo video in activeImage.imageVideos)
            {
                CreateVideoListItem(video);
            }
        }
        else
        {
            loadingMessage.text = "Fetching content";
            loadingScreen.SetActive(true);

            // Get content list for image
            StartCoroutine(dataManager.RequestHandler($"{dataManager.apiUrl}/images/{activeImage.id}/videos", "GET", "", (response, errorMessage) =>
            {
                if (response.Equals("failure"))
                    notificationLogic.DisplayNotification("Fetch Content Failed", errorMessage, "error");
                else
                {
                    ImageVideoList videos = JsonUtility.FromJson<ImageVideoList>("{\"imageVideos\":" + response + "}");
                    videos.imageVideos = videos.imageVideos.OrderByDescending(x => x.likes).ToList();
                    // Create a list item for every available video
                    foreach (var vid in videos.imageVideos)
                    {
                        CreateVideoListItem(vid);
                    }
                }
                loadingScreen.SetActive(false);
            }));
        }
    }

    // Instantiate a new list item from a prefab
    public void CreateVideoListItem(ImageVideo video)
    {
        GameObject newListItem = Instantiate(listItemPrefab);
        newListItem.transform.SetParent(listContentPrefab.transform);
        newListItem.transform.localScale = new Vector3(1, 1, 1);
        newListItem.GetComponent<VideoListItem>().itemVideo = video;
        newListItem.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = spriteVideo;
        newListItem.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text = video.title;
        newListItem.transform.GetChild(2).GetChild(0).GetComponent<TMP_Text>().text = video.likes.ToString();
    }

    // Called on click of a list item to set that item active
    public void SetActiveSelection(GameObject listItem)
    {
        activeSelection = listItem;
        selectedVideo = activeSelection.GetComponent<VideoListItem>().itemVideo;

        if (!selectionInformationButton.activeSelf)
            selectionInformationButton.SetActive(true);

        if (selectionInformation.activeSelf)
            SetSelectionInformation();
    }

    // Called on information button click
    public void ToggleSelectionInformation()
    {
        if (selectionInformation.activeSelf)
            DisableSelectionInformation();
        else
        {
            selectionInformation.SetActive(true);
            SetSelectionInformation();
        }
    }

    // Sets video information in the information window to current selection
    public void SetSelectionInformation()
    {
        informationVideoName.text = selectedVideo.title;
        informationVideoLink.text = selectedVideo.link;
        informationVideoCreator.text = "by " + selectedVideo.creator;

        // Enable delete button if current user is creator of the video
        if (selectedVideo.creator == dataManager.SessionData.activeUser)
        {
            deleteButton.SetActive(true);
            DeleteButtonPressReset();
        }
        else
            deleteButton.SetActive(false);

        // Disable like button in local mode
        if (dataManager.SessionData.localMode)
            likeButton.SetActive(false);
        else
        {
            // Check if video was already liked from this device
            if (dataManager.SessionData.likedVideos.Contains(selectedVideo.videoid))
            {
                likeButtonIcon.sprite = likeFilled;
                liked = true;
            }
            else
            {
                likeButtonIcon.sprite = likeOutline;
                liked = false;
            }
        }
    }

    // Delete button in information window clicked
    public void InitiateDelete()
    {
        // Check if delete button was pressed for the first or second time
        if (!deletePressedOnce)
            DeleteButtonFirstPress();
        // Delete video if delete button was pressed twice
        else
        {
            audioManager.PlayOneShot(deleteSecond);
            // If video is currently running, disable its video player
            if (activeVideoPlayer.url == dataManager.GetCutVideoLink(selectedVideo.link))
            {
                StartCoroutine(dataManager.DisableLastActiveVideoPlayer(returnValue =>
                {
                    if (returnValue == "finished")
                    {
                        activePlane.GetComponent<PlaneClickHandler>().ResetLastPlayVideo();
                        DeleteContinue();
                    }
                }));
            }
            else
                DeleteContinue();
        }
    }

    public void DeleteContinue()
    {
        // Remove video entry from image's video list in local mode or from database
        if (dataManager.SessionData.localMode)
        {
            dataManager.RemoveVideoFromImageLocal(activeImage.id, selectedVideo);
            DeleteSelection();
        }
        else
        {
            loadingMessage.text = "Deleting video";
            loadingScreen.SetActive(true);

            // Send delete request for selected video
            StartCoroutine(dataManager.RequestHandler($"{dataManager.apiUrl}/videos/{selectedVideo.videoid}", "DELETE", "", (response, errorMessage) =>
            {
                if (response.Equals("failure"))
                    notificationLogic.DisplayNotification("Video Deletion Failed", errorMessage, "error");
                else
                    DeleteSelection();

                loadingScreen.SetActive(false);
            }));
        }
    }

    // If video was successfully deleted from database delete selected item and check if it was last video for image
    public void DeleteSelection()
    {
        // Destroy the currently selected ListViewItem
        Destroy(activeSelection);
        // Close selection information box, since the selected item is deleted and therefore the selection lost
        selectionInformationButton.SetActive(false);
        DisableSelectionInformation();

        // Logic to delete image if deleted video was last video for that image
        if (listContentPrefab.transform.childCount == 1)
        {
            if (dataManager.SessionData.localMode)
            {
                DeleteActiveImage();
                notificationLogic.DisplayNotification("Image Deleted", "Image target without content was deleted.", "information");
                CloseSelectContentScreen();
            }
            else
            {
                loadingMessage.text = "Deleting image";
                loadingScreen.SetActive(true);

                // Send delete request for active image
                StartCoroutine(dataManager.RequestHandler($"{dataManager.apiUrl}/images/{activeImage.id}", "DELETE", "", (response, errorMessage) =>
                {
                    if (response.Equals("failure"))
                        notificationLogic.DisplayNotification("Image Deletion Failed", errorMessage, "error");
                    else
                    {
                        notificationLogic.DisplayNotification("Image Deleted", "Image target without content was deleted.", "information");
                        // If image was successfully deleted from database, delete image locally
                        DeleteActiveImage();
                        CloseSelectContentScreen();
                    }
                    loadingScreen.SetActive(false);
                }));
            }
        }
    }

    // Remove deleted image locally and remember ID of deleted images to not track them
    // Workaround for inability to remove image targets from a MutableRuntimeReferenceImageLibrary
    public void DeleteActiveImage()
    {
        dataManager.RemoveImageLocal(activeImage.id);
        dataManager.AddDeletedImage(activeImage.id);
        if (dataManager.SessionData.localMode || dataManager.SessionData.casheImages)
        {
            File.Delete(dataManager.GetImageFilePath(activeImage.id));
        }
        activePlane.transform.parent.parent.parent.gameObject.SetActive(false);
    }

    // Like button in information window clicked
    public void LikeButtonPress()
    {
        // Prevent multiple clicks of like button while server is contacted
        SetLikeButtonInactive();

        // Check current like status to set action
        string action = liked ? "remove" : "add";
        string json = "{\"action\": \"" + action + "\"}";

        // Add or remove like from image
        StartCoroutine(dataManager.RequestHandler($"{dataManager.apiUrl}/videos/{selectedVideo.videoid}/likes", "PUT", json, (response, errorMessage) =>
        {
            if (response.Equals("failure"))
                notificationLogic.DisplayNotification("Like Process Failed", errorMessage, "error");
            else
            {
                // Set sprite filled or only outline and update local like display
                likeButtonIcon.sprite = liked ? likeOutline : likeFilled;
                selectedVideo.likes += liked ? -1 : 1;
                dataManager.UpdateLikedVideos(selectedVideo.videoid, liked);
                liked = !liked;
                UpdateLikesCount(selectedVideo.likes);
            }
            SetLikeButtonActive();
        }));
    }

    // Update local like display
    private void UpdateLikesCount(int likes)
    {
        activeSelection.transform.GetChild(2).GetChild(0).GetComponent<TMP_Text>().text = likes.ToString();
    }

    private void SetLikeButtonActive()
    {
        likeButton.GetComponent<Button>().enabled = true;
    }

    private void SetLikeButtonInactive()
    {
        likeButton.GetComponent<Button>().enabled = false;
    }

    // Link in information window clicked opens that link
    public void OpenLink()
    {
        Application.OpenURL(informationVideoLink.text);
    }

    private void DisableSelectionInformation()
    {
        DeleteButtonPressReset();
        selectionInformation.SetActive(false);
    }

    // When delete button pressed for the first time, change color
    private void DeleteButtonFirstPress()
    {
        audioManager.PlayOneShot(deleteFirst);
        deleteButtonIcon.color = new Color32(220, 75, 75, 255);
        deletePressedOnce = true;
    }

    // Reset delete button
    private void DeleteButtonPressReset()
    {
        deleteButtonIcon.color = Color.white;
        deletePressedOnce = false;
    }

    // Confirm button in content selection screen clicked
    public void ConfirmContentSelection()
    {
        // If selection was made, load that video into the VideoPlayer
        if (activeSelection != null && activeImage != null)
        {
            StartCoroutine(dataManager.DisableLastActiveVideoPlayer(returnValue =>
            {
                if (returnValue == "finished")
                {
                    if (!activeContentParent.activeSelf)
                        activeContentParent.SetActive(true);

                    PlaneClickHandler pch = activePlane.GetComponent<PlaneClickHandler>();
                    pch.NewVideoPlayer(selectedVideo);
                    CloseSelectContentScreen();
                }
            }));
        }
    }

    // Close content selection screen, reset active image and destroy created list items
    public void CloseSelectContentScreen()
    {
        activeVideoPlayer = null;
        activeImage = null;
        activeSelection = null;
        selectedVideo = null;

        while (listContentPrefab.transform.childCount > 0)
        {
            DestroyImmediate(listContentPrefab.transform.GetChild(0).gameObject);
        }

        selectionInformationButton.SetActive(false);
        DisableSelectionInformation();
        selectContentScreen.SetActive(false);
    }
}
