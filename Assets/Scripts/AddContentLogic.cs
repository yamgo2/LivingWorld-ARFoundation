using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class AddContentLogic : MonoBehaviour
{
    [Serializable]
    public class ImageData
    {
        // The source texture for the image (must be marked as readable)
        [SerializeField]
        Texture2D m_Texture;
        public Texture2D texture
        {
            get => m_Texture;
            set => m_Texture = value;
        }

        // The name for this image
        [SerializeField]
        string m_Name;
        public string name
        {
            get => m_Name;
            set => m_Name = value;
        }
    }

    public GameObject baseScreen;

    public GameObject addContentScreen;

    public GameObject loadingScreen;
    public TMP_Text loadingMessage;

    public DataManager dataManager;

    public ReferenceLibraryManager referenceLibraryManager;

    public NotificationLogic notificationLogic;

    public Animator cancelButtonAnimator;
    public Animator confirmButtonAnimator;

    private ImageData imageToAddData = new ImageData();

    private ReferenceImage activeImage;

    private bool isNewImage;

    public TMP_InputField videoLinkInputField;

    public TMP_InputField videoNameInputField;

    public TMP_InputField videoWidthInputField;

    public TMP_InputField videoHeightInputField;

    public TextFieldActiveHandler videoNameActiveHandler;

    public TextFieldActiveHandler videoLinkActiveHandler;

    public TextFieldActiveHandler videoWidthActiveHandler;

    public TextFieldActiveHandler videoHeightActiveHandler;

    private Regex videoLinkRegex = new Regex(@"(youtu\.be/|youtube\.com/(watch\?v=|embed/|shorts/))(\w+)");
    private Regex videoFormatRegex = new Regex(@"\.(mp4|mov|webm)$");

    private const int MaxImageSize = 500;
    private const int JpegQuality = 85;

    // Called when photo button is clicked
    public void CaptureImage()
    {
        StartCoroutine(CaptureScreen());
    }

    public IEnumerator CaptureScreen()
    {
        // Wait till the last possible moment before screen rendering to hide the UI
        yield return null;
        baseScreen.SetActive(false);

        // Wait for screen rendering to complete
        yield return new WaitForEndOfFrame();

        // Take screenshot
        var textureCapt = ScreenCapture.CaptureScreenshotAsTexture();

        baseScreen.SetActive(true);

        CropImageJob(textureCapt);
    }

    // Uses NativeGallery plugin to load an image from the devices gallery
    public void LoadImageFromGallery()
    {
        NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                // Create Texture from selected image
                Texture2D loadedImage = NativeGallery.LoadImageAtPath(path);
                if (loadedImage == null)
                {
                    notificationLogic.DisplayNotification("Loading Exception", "Loaded image from " + path + " is null.", "error");
                    baseScreen.SetActive(true);
                    return;
                }
                else
                    CropImageJob(loadedImage);
            }
        });
    }

    // Uses CropImage plugin to crop images
    void CropImageJob(Texture2D imageToCrop)
    {
        ImageCropper.Instance.Show(imageToCrop, (bool result, Texture originalImage, Texture2D croppedImage) =>
        {
            // If screenshot was cropped successfully
            if (result)
            {
                imageToAddData.name = Guid.NewGuid().ToString();
                imageToAddData.texture = croppedImage;
                isNewImage = true;
                ShowAddContentScreen();
            }
            else
                baseScreen.SetActive(true);
        },
        settings: new ImageCropper.Settings()
        {
            ovalSelection = false,
            autoZoomEnabled = true,
            imageBackground = Color.clear,
            markTextureNonReadable = false,
            visibleButtons = ImageCropper.Button.Rotate90Degrees
        });
    }

    // Called when add content button of image target is clicked
    public void AddContentToImage(GameObject clickedImage)
    {
        isNewImage = false;
        activeImage = dataManager.GetImageData(clickedImage.name);
        ShowAddContentScreen();
    }

    // Display screen to add content to image target
    private void ShowAddContentScreen()
    {
        addContentScreen.SetActive(true);
        videoWidthInputField.text = "16";
        videoHeightInputField.text = "9";
        videoWidthActiveHandler.InputFieldDeselect();
        videoHeightActiveHandler.InputFieldDeselect();
        // Fix stuck button animation
        cancelButtonAnimator.Play("Normal", 0, 0f);
        confirmButtonAnimator.Play("Normal", 0, 0f);
    }

    // Called when confirm button is clicked in screen to add content
    public void ConfirmAddContent()
    {
        string inputLink = videoLinkInputField.text;
        string inputTitle = videoNameInputField.text;
        string videoWidth = videoWidthInputField.text;
        string videoHeight = videoHeightInputField.text;

        // Perform input validation
        if (string.IsNullOrEmpty(inputTitle))
        {
            notificationLogic.DisplayNotification("Invalid Input", "Title can't be empty.", "warning");
            return;
        }
        if (string.IsNullOrEmpty(inputLink))
        {
            notificationLogic.DisplayNotification("Invalid Input", "Link can't be empty.", "warning");
            return;
        }
        if (string.IsNullOrEmpty(videoWidth) || string.IsNullOrEmpty(videoHeight))
        {
            notificationLogic.DisplayNotification("Invalid Input", "Please enter the aspect ratio of the video.", "warning");
            return;
        }
        if (!IsVideoLinkValid(inputLink))
        {
            notificationLogic.DisplayNotification("Invalid Input", "Link has to be a valid video link.", "warning");
            return;
        }

        ImageVideo videoToAdd = new ImageVideo
        {
            title = inputTitle,
            link = inputLink,
            videoid = Guid.NewGuid().ToString(),
            likes = 0,
            creator = dataManager.SessionData.activeUser,
            height = int.Parse(videoHeight),
            width = int.Parse(videoWidth)
        };

        // Check if video is added to newly created or existing image
        if (isNewImage)
        {
            if (IsImageTooBig(imageToAddData.texture))
                imageToAddData.texture = RezizeImageKeepingAspect(imageToAddData.texture, MaxImageSize);

            SaveImage(imageToAddData.name, imageToAddData.texture);

            ReferenceImage imageToAdd = new ReferenceImage { id = imageToAddData.name };

            // Check if image is only added locally or also remotely
            if (dataManager.SessionData.localMode)
            {
                dataManager.AddImageLocal(imageToAdd);
                // Fixes a crash with disabling active video players on some devices
                StartCoroutine(dataManager.DisableLastActiveVideoPlayer(returnValue =>
                {
                    if (returnValue == "finished")
                    {
                        referenceLibraryManager.AddToReferenceLibrary(imageToAddData.texture, imageToAddData.name);
                        AddVideoToImage(imageToAdd, videoToAdd);
                    }
                }));
            }
            else
            {
                loadingScreen.SetActive(true);
                loadingMessage.text = "Saving image";

                // Base64 encode image and save resulting string into the DatabaseReferenceImage object that will be posted to the server
                DatabaseReferenceImage dbImage = new DatabaseReferenceImage
                {
                    imageid = imageToAdd.id,
                    library = dataManager.SessionData.activeLibrary,
                    image = Convert.ToBase64String(imageToAddData.texture.EncodeToJPG(JpegQuality))
                };

                string json = JsonUtility.ToJson(dbImage);

                StartCoroutine(dataManager.RequestHandler($"{dataManager.apiUrl}/images", "POST", json, (response, errorMessage) =>
                {
                    // Check if image was created successfully in database
                    if (response.Equals("failure"))
                    {
                        notificationLogic.DisplayNotification("Image Creation Failed", errorMessage, "error");
                        loadingScreen.SetActive(false);
                    }
                    else
                    {
                        dataManager.AddImageLocal(imageToAdd);
                        StartCoroutine(dataManager.DisableLastActiveVideoPlayer(returnValue =>
                        {
                            if (returnValue == "finished")
                            {
                                referenceLibraryManager.AddToReferenceLibrary(imageToAddData.texture, imageToAddData.name);
                                AddVideoToImage(imageToAdd, videoToAdd);
                            }
                        }));
                    }
                }));
            }
        }
        else
            AddVideoToImage(activeImage, videoToAdd);
    }

    // Support a few common and tested video formats and Youtube links
    private bool IsVideoLinkValid(string link)
    {
        return videoLinkRegex.IsMatch(link) || videoFormatRegex.IsMatch(link);
    }

    // Check image size
    private bool IsImageTooBig(Texture2D texture)
    {
        return texture.width > MaxImageSize || texture.height > MaxImageSize;
    }

    // Save image locally if cashe is enabled or in local mode
    private void SaveImage(string imageName, Texture2D texture)
    {
        if (dataManager.SessionData.casheImages || dataManager.SessionData.localMode)
        {
            try
            {
                File.WriteAllBytes(dataManager.GetImageFilePath(imageName), texture.EncodeToJPG(JpegQuality));
            }
            catch (Exception e)
            {
                notificationLogic.DisplayNotification("File Writing Exception", e.Message, "error");
            }
        }
    }

    private void AddVideoToImage(ReferenceImage image, ImageVideo videoToAdd)
    {
        // Add video details to local json  if local mode, or save video details to remote database if not
        if (dataManager.SessionData.localMode)
        {
            dataManager.AddVideoToImageLocal(image.id, videoToAdd);
            CloseAddContentScreen();
        }
        else
        {
            loadingMessage.text = "Adding video";
            loadingScreen.SetActive(true);

            videoToAdd.imageid = image.id;
            string json = JsonUtility.ToJson(videoToAdd);

            StartCoroutine(dataManager.RequestHandler($"{dataManager.apiUrl}/videos", "POST", json, (response, errorMessage) =>
            {
                // Check if video was created successfully in database
                if (response.Equals("failure"))
                    notificationLogic.DisplayNotification("Video Creation Failed", errorMessage, "error");
                else
                    // Only close add content screen when success, otherwise keep it open to retry or cancel
                    CloseAddContentScreen();

                loadingScreen.SetActive(false);
            }));
        }
    }

    // Resizes image to fit within a maximum size limit while keeping its aspect ratio intact
    private Texture2D RezizeImageKeepingAspect(Texture2D image, int maxSize)
    {
        float iHeight = image.height;
        float iWidth = image.width;

        // Differentiate between cases of higher than wide or wider than high images
        if (image.width > image.height)
        {
            float aspectRatio = iHeight / iWidth;
            int height = Mathf.RoundToInt(maxSize * aspectRatio);
            image = ResizeTexture(image, maxSize, height);
        }
        else
        {
            float aspectRatio = iWidth / iHeight;
            int width = Mathf.RoundToInt(maxSize * aspectRatio);
            image = ResizeTexture(image, width, maxSize);
        }
        return image;
    }

    // Resizes a given texture to the given width and height using a temporary render texture
    private Texture2D ResizeTexture(Texture2D sourceTexture, int targetWidth, int targetHeight)
    {
        // FilterMode.Point to avoid blurring when resizing
        sourceTexture.filterMode = FilterMode.Point;
        RenderTexture render = RenderTexture.GetTemporary(targetWidth, targetHeight);
        render.filterMode = FilterMode.Point;
        RenderTexture.active = render;
        Graphics.Blit(sourceTexture, render);
        Texture2D newTexture = new Texture2D(targetWidth, targetHeight);
        // Read the pixels from the temporary render texture into the new Texture2D object and apply the changes
        newTexture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        newTexture.Apply();
        RenderTexture.active = null;
        // Release the temporary render texture from memory
        RenderTexture.ReleaseTemporary(render);
        return newTexture;
    }

    // Close screen to add content and reset input fields
    public void CloseAddContentScreen()
    {
        videoLinkInputField.text = "";
        videoNameInputField.text = "";
        videoNameActiveHandler.InputFieldDeselect();
        videoLinkActiveHandler.InputFieldDeselect();
        activeImage = null;
        addContentScreen.SetActive(false);
    }
}