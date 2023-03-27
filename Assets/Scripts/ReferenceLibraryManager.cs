using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ReferenceLibraryManager : MonoBehaviour
{
    public DataManager dataManager;

    public NotificationLogic notificationLogic;

    // Add texture to MutableRuntimeReferenceImageLibrary
    public bool AddToReferenceLibrary(Texture2D texture, string name)
    {
        var manager = GetComponent<ARTrackedImageManager>();
        if (manager == null)
        {
            notificationLogic.DisplayNotification("Image Manager Error", $"No {nameof(ARTrackedImageManager)} available.", "error");
            return false;
        }

        // Only return success if reference library is mutable
        if (manager.referenceLibrary is MutableRuntimeReferenceImageLibrary mutableLibrary)
        {
            try
            {
                mutableLibrary.ScheduleAddImageWithValidationJob(texture, name, 0.1f);
                return true;
            }
            catch (InvalidOperationException e)
            {
                notificationLogic.DisplayNotification("ScheduleAddImageJob Exception", e.Message, "error");
                return false;
            }
        }
        else
        {
            notificationLogic.DisplayNotification("Library Error", "The reference image library is not mutable.", "error");
            return false;
        }
    }
}
