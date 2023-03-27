using UnityEngine.XR.ARSubsystems;
using UnityEngine.Video;
using TMPro;

namespace UnityEngine.XR.ARFoundation
{
    // This component listens for images detected by the XRImageTrackingSubsystem
    [RequireComponent(typeof(ARTrackedImageManager))]
    public class TrackedImageInfoManager : MonoBehaviour
    {
        // The prefab has a world space UI canvas, which requires a camera to function properly
        [SerializeField]
        Camera m_WorldSpaceCanvasCamera;
        public Camera worldSpaceCanvasCamera
        {
            get { return m_WorldSpaceCanvasCamera; }
            set { m_WorldSpaceCanvasCamera = value; }
        }

        // If an image is detected but no source texture can be found, this texture is used instead
        [SerializeField]
        Texture2D m_DefaultTexture;
        public Texture2D defaultTexture
        {
            get { return m_DefaultTexture; }
            set { m_DefaultTexture = value; }
        }

        ARTrackedImageManager m_TrackedImageManager;

        public DataManager dataManager;

        public NotificationLogic ntfLogic;

        public Material holoMaterial;
        public Material solidMaterial;

        void Awake()
        {
            m_TrackedImageManager = GetComponent<ARTrackedImageManager>();
        }

        void OnEnable()
        {
            m_TrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        }

        void OnDisable()
        {
            m_TrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
        }

        // Called when image targets are lost or found
        void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
        {
            // Iterate all newly tracked images
            foreach (var trackedImage in eventArgs.added)
            {
                var trackedImageGo = trackedImage.transform.gameObject;

                // Don't show virtual content if image was deleted in current session
                if (dataManager.SessionData.deletedImages.Contains(trackedImage.referenceImage.name))
                {
                    if (trackedImageGo.activeSelf)
                        trackedImageGo.SetActive(false);
                }
                else
                {
                    if (!trackedImageGo.activeSelf)
                        trackedImageGo.SetActive(true);
                }

                // ContentParent and plane get name (id) of their respective image
                var planeGo = trackedImage.transform.GetChild(0).GetChild(0).GetChild(0);
                trackedImage.transform.GetChild(0).gameObject.name = trackedImage.referenceImage.name + "content";
                planeGo.name = trackedImage.referenceImage.name;

                // Set the texture of the plane to that of the reference image
                var material = planeGo.GetComponentInChildren<MeshRenderer>().material;
                material.mainTexture = (trackedImage.referenceImage.texture == null) ? defaultTexture : trackedImage.referenceImage.texture;

                UpdateInfo(trackedImage);
            }
            foreach (var trackedImage in eventArgs.updated)
            {
                UpdateInfo(trackedImage);
            }
        }

        void UpdateInfo(ARTrackedImage trackedImage)
        {
            var contentParent = trackedImage.transform.GetChild(0).gameObject;

            // Disabling the object with the last active plane would destroy the active video player, therefore a differentiation is made
            if (dataManager.lastActivePlane != null && contentParent.name == dataManager.lastActivePlane.name + "content")
                UpdateActiveImage(contentParent, trackedImage);
            else
                UpdateInactiveImage(contentParent, trackedImage);
        }

        private void UpdateActiveImage(GameObject contentParent, ARTrackedImage trackedImage)
        {
            // Store frequently accessed objects as member variables
            var planeParentGo = contentParent.transform.GetChild(0).gameObject;
            var canvasSelectionButton = contentParent.transform.GetChild(1).gameObject;
            var canvasPlayIcon = contentParent.transform.GetChild(2).gameObject;
            var planeParentBgGo = contentParent.transform.GetChild(3).gameObject;
            var canvasVideoControl = contentParent.transform.GetChild(4).gameObject;
            var canvasVideoName = contentParent.transform.GetChild(5).gameObject;
            var canvasAddButton = contentParent.transform.GetChild(6).gameObject;

            var planeGo = planeParentGo.transform.GetChild(0).gameObject;
            var clickCanvas = planeParentGo.transform.GetChild(1).gameObject;
            var holoParent = planeParentGo.transform.GetChild(2).gameObject;

            MeshRenderer meshRenderer = planeGo.GetComponent<MeshRenderer>();
            VideoPlayer videoPlayer = planeGo.GetComponent<VideoPlayer>();
            PlaneClickHandler pch = planeGo.GetComponent<PlaneClickHandler>();

            // Set tracked image dimensions
            planeParentGo.transform.localScale = new Vector3(trackedImage.size.x, 1f, trackedImage.size.y);

            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                // Enable gameObjects when they were previously disabled (only checking one component for performance reasons)
                if (!meshRenderer.enabled)
                {
                    meshRenderer.enabled = true;
                    canvasSelectionButton.SetActive(true);
                    canvasAddButton.SetActive(true);
                    canvasPlayIcon.SetActive(true);
                    clickCanvas.SetActive(true);
                }

                if (videoPlayer.enabled)
                {
                    // Resume video when tracked image was previously lost and is found again
                    if (pch.wasPlaying)
                    {
                        if (!videoPlayer.isPlaying)
                        {
                            videoPlayer.Play();
                            pch.DisablePlayIcon();
                        }
                    }

                    if (!canvasVideoControl.activeSelf)
                    {
                        canvasVideoControl.SetActive(true);
                        canvasVideoName.SetActive(true);
                    }

                    // Position video control element at bottom and video name at top of image target
                    canvasVideoControl.transform.localPosition = new Vector3(canvasVideoControl.transform.localPosition.x, canvasVideoControl.transform.localPosition.y, -0.005f - trackedImage.size.y / 2);
                    canvasVideoName.transform.localPosition = new Vector3(canvasVideoName.transform.localPosition.x, canvasVideoName.transform.localPosition.y, 0.01f + trackedImage.size.y / 2);

                    // Set name to active video name if necessary
                    var videoName = canvasVideoName.transform.GetChild(0).GetComponent<TMP_Text>();

                    if (videoName.text != pch.videoName)
                        videoName.text = pch.videoName;

                    // Check if setting to stretch videos is enabled and set size of tracked image accordingly
                    if (!dataManager.SessionData.stretchVideos)
                    {
                        // Calculate tracked image and video aspect ratios, then compare them to set size
                        float imageAspectRatio = trackedImage.size.x / trackedImage.size.y;
                        float videoAspectRatio = pch.videoAspectRatioWidth / pch.videoAspectRatioHeight;

                        if (imageAspectRatio < videoAspectRatio)
                        {
                            planeParentGo.transform.localScale = new Vector3(trackedImage.size.x, 1f, trackedImage.size.x / videoAspectRatio);
                        }
                        else if (imageAspectRatio > videoAspectRatio)
                        {
                            planeParentGo.transform.localScale = new Vector3(trackedImage.size.y * videoAspectRatio, 1f, trackedImage.size.y);
                        }

                        // Disable background if setting for holo mode is enabled and stretch videos disabled
                        if (!planeParentBgGo.activeSelf && !dataManager.SessionData.holoMode)
                            planeParentBgGo.SetActive(true);
                        else if (planeParentBgGo.activeSelf && dataManager.SessionData.holoMode)
                            planeParentBgGo.SetActive(false);

                        planeParentBgGo.transform.localScale = new Vector3(trackedImage.size.x, 1f, trackedImage.size.y);
                    }
                    else if (planeParentBgGo.activeSelf)
                        planeParentBgGo.SetActive(false);
                }
                else
                {
                    // Don't show control elements if no video is playing
                    if (canvasVideoControl.activeSelf)
                        canvasVideoControl.SetActive(false);
                    if (canvasVideoName.activeSelf)
                        canvasVideoName.SetActive(false);
                }

                PositionButtons(canvasSelectionButton, canvasAddButton, trackedImage);
                CheckHoloMode(holoParent, planeGo, canvasPlayIcon, clickCanvas, meshRenderer, trackedImage);
            }
            else
            {
                // Pause video when tracking is lost
                if (videoPlayer.isPlaying)
                {
                    videoPlayer.Pause();
                    pch.EnablePlayIcon();
                }
                // Disable gameObjects if they were previously enabled
                if (meshRenderer.enabled)
                {
                    meshRenderer.enabled = false;
                    canvasSelectionButton.SetActive(false);
                    canvasAddButton.SetActive(false);
                    clickCanvas.SetActive(false);
                    canvasVideoName.SetActive(false);
                    canvasPlayIcon.SetActive(false);
                    canvasVideoControl.SetActive(false);
                    planeParentBgGo.SetActive(false);
                    holoParent.SetActive(false);
                }
            }
        }

        private void UpdateInactiveImage(GameObject contentParent, ARTrackedImage trackedImage)
        {
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                if (!contentParent.activeSelf)
                    contentParent.SetActive(true);

                var planeParentGo = contentParent.transform.GetChild(0).gameObject;
                var canvasSelectionButton = contentParent.transform.GetChild(1).gameObject;
                var canvasPlayIcon = contentParent.transform.GetChild(2).gameObject;
                var planeParentBgGo = contentParent.transform.GetChild(3).gameObject;
                var canvasAddButton = contentParent.transform.GetChild(6).gameObject;

                var planeGo = planeParentGo.transform.GetChild(0).gameObject;
                var clickCanvas = planeParentGo.transform.GetChild(1).gameObject;
                var holoParent = planeParentGo.transform.GetChild(2).gameObject;

                // Disable background, as it's only needed for active planes
                if (planeParentBgGo.activeSelf)
                    planeParentBgGo.SetActive(false);

                // Enabled control elements in case they were previously disabled for an untracked active image
                if (!canvasSelectionButton.activeSelf || !canvasAddButton.activeSelf || !clickCanvas.activeSelf || !canvasPlayIcon.activeSelf)
                {
                    canvasSelectionButton.SetActive(true);
                    canvasAddButton.SetActive(true);
                    canvasPlayIcon.SetActive(true);
                    clickCanvas.SetActive(true);
                }

                planeParentGo.transform.localScale = new Vector3(trackedImage.size.x, 1f, trackedImage.size.y);

                PositionButtons(canvasSelectionButton, canvasAddButton, trackedImage);
                CheckHoloMode(holoParent, planeGo, canvasPlayIcon, clickCanvas, planeGo.GetComponent<MeshRenderer>(), trackedImage);
            }
            // Disable enitre object if not active or tracked
            else if (contentParent.activeSelf)
                contentParent.SetActive(false);
        }

        // Place the buttons at the left and right border of the image
        private void PositionButtons(GameObject canvasSelectionButton, GameObject canvasAddButton, ARTrackedImage trackedImage)
        {
            canvasSelectionButton.transform.localPosition = new Vector3(-0.01f - trackedImage.size.x / 2, canvasSelectionButton.transform.localPosition.y, canvasSelectionButton.transform.localPosition.z);
            canvasAddButton.transform.localPosition = new Vector3(0.01f + trackedImage.size.x / 2, canvasAddButton.transform.localPosition.y, canvasAddButton.transform.localPosition.z);
        }

        private void CheckHoloMode(GameObject holoParent, GameObject planeGo, GameObject canvasPlayIcon, GameObject clickCanvas, MeshRenderer meshRenderer, ARTrackedImage trackedImage)
        {
            // Enable or disable gameObjects for holo mode and make necessary settings
            if (dataManager.SessionData.holoMode && (!holoParent.activeSelf || planeGo.transform.localPosition.y != 0.045f))
            {
                holoParent.SetActive(true);
                planeGo.transform.localPosition = new Vector3(0, 0.045f, 0);
                canvasPlayIcon.transform.localPosition = new Vector3(0, 0.045f, 0);
                clickCanvas.transform.localPosition = new Vector3(0, 0.05f, 0);
                meshRenderer.sharedMaterial = holoMaterial;
                meshRenderer.material.mainTexture = (trackedImage.referenceImage.texture == null) ? defaultTexture : trackedImage.referenceImage.texture;
            }
            else if (!dataManager.SessionData.holoMode && (holoParent.activeSelf || planeGo.transform.localPosition.y == 0.045f))
            {
                holoParent.SetActive(false);
                planeGo.transform.localPosition = new Vector3(0, 0, 0);
                canvasPlayIcon.transform.localPosition = new Vector3(0, 0, 0);
                clickCanvas.transform.localPosition = new Vector3(0, 0.005f, 0);
                meshRenderer.sharedMaterial = solidMaterial;
                meshRenderer.material.mainTexture = (trackedImage.referenceImage.texture == null) ? defaultTexture : trackedImage.referenceImage.texture;
            }
        }
    }
}