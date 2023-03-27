using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

public class DataManager : MonoBehaviour
{
    // Only required in DataManager, therefore declared here
    [Serializable]
    public class Response
    {
        public string message;
    }

    private ReferenceImageData data;
    private SessionData sessionData;
    private string file = "ReferenceImageData.json";
    private string sessionFile = "SessionData.json";

    public NotificationLogic notificationLogic;

    public string apiUrl;

    public ReferenceImageData Data => data;

    public SessionData SessionData => sessionData;

    public static DataManager Instance { get; private set; }

    public GameObject lastActivePlane;

    private Regex videoFormatRegex = new Regex(@"\.(mp4|mov|webm)$");

    // Creates necessary folders for the application to store data locally, including images and reference libraries
    public void CreateFolders()
    {
        if (!Directory.Exists(Application.persistentDataPath + "/Libraries/Local/"))
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/Libraries/Local/");
        }
        if (!Directory.Exists(Application.persistentDataPath + "/Libraries/Local" + "/images/"))
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/Libraries/Local" + "/images/");
        }

        foreach (ReferenceLibrary library in sessionData.availableLibraries)
        {
            if (!Directory.Exists(Application.persistentDataPath + "/Libraries/" + library.name + "/"))
            {
                Directory.CreateDirectory(Application.persistentDataPath + "/Libraries/" + library.name + "/");
            }
            if (!Directory.Exists(Application.persistentDataPath + "/Libraries/" + library.name + "/images/"))
            {
                Directory.CreateDirectory(Application.persistentDataPath + "/Libraries/" + library.name + "/images/");
            }
        }
    }

    // Deletes and recreates folder for a specific reference library
    public void ResetLibraryFolder(string libraryName)
    {
        if (Directory.Exists(Application.persistentDataPath + "/Libraries/" + libraryName + "/"))
        {
            Directory.Delete(Application.persistentDataPath + "/Libraries/" + libraryName + "/", true);
        }
        Directory.CreateDirectory(Application.persistentDataPath + "/Libraries/" + libraryName + "/");
        Directory.CreateDirectory(Application.persistentDataPath + "/Libraries/" + libraryName + "/images/");
        LoadJsonImageData();
    }

    // Sends a HTTP request to the specified URI
    // Can be given JSON data to send
    public IEnumerator RequestHandler(string uri, string httpMethod, string json = "", System.Action<string, string> callback = null)
    {
        // Create a new UnityWebRequest with the specified URI and HTTP method
        using (UnityWebRequest req = new UnityWebRequest(uri, httpMethod))
        {
            // If HTTP method is POST or PUT, add the JSON data to the request body and set the "Content-Type" header
            if (httpMethod == "POST" || httpMethod == "PUT")
            {
                byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(jsonToSend);
                req.SetRequestHeader("Content-Type", "application/json");
            }

            // Set the download handler to buffer the response data, and set a connection timeout of 45 seconds
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = 45;

            // Send the request and wait until it completes
            yield return req.SendWebRequest();

            string errorMessage = null;
            // If the HTTP request did not complete successfully
            if (req.result != UnityWebRequest.Result.Success)
            {
                // Deserialize the response body into a Response object
                Response error = JsonUtility.FromJson<Response>(req.downloadHandler.text);

                // If the error message is empty, the server is unreachable or the connection timed out
                if (string.IsNullOrEmpty(error?.message))
                    notificationLogic.DisplayNotification("Server Unreachable", "Please check your internet connection or try again later.", "error");
                else
                    errorMessage = error.message;

                // Call the callback function with a "failure" status and the error message (if any)
                callback("failure", errorMessage);
            }
            // If the request succeeds, pass the response body to the callback function with a null error message
            else
                callback(req.downloadHandler.text, errorMessage);
        }
    }

    // Various methods to handle local data

    public void SetActiveUser(string user)
    {
        sessionData.activeUser = user;
        SaveJsonSessionData();
    }

    public void SetLastActiveUser(string user)
    {
        sessionData.lastActiveUser = user;
        SaveJsonSessionData();
    }

    public void SetActiveLibrary(string library)
    {
        sessionData.activeLibrary = library;
        // Set file to that of active library
        file = "/Libraries/" + sessionData.activeLibrary + "/ReferenceImageData.json";
        SaveJsonSessionData();
    }

    public void AddLibrary(ReferenceLibrary library)
    {
        sessionData.availableLibraries.Add(library);
        SaveJsonSessionData();
    }

    public void UpdateLikedVideos(string id, bool isLiked)
    {
        if (isLiked)
            sessionData.likedVideos.Remove(id);
        else
            sessionData.likedVideos.Add(id);

        SaveJsonSessionData();
    }

    public void AddDeletedImage(string id)
    {
        sessionData.deletedImages.Add(id);
        SaveJsonSessionData();
    }

    public void ClearDeletedImages()
    {
        sessionData.deletedImages.Clear();
        SaveJsonSessionData();
    }

    public void AddImageLocal(ReferenceImage imageToAdd)
    {
        data.referenceImages.Add(imageToAdd);
        SaveJsonImageData();
    }

    public void RemoveImageLocal(string imageToRemove)
    {
        ReferenceImage image = GetImageData(imageToRemove);
        data.referenceImages.Remove(image);
        SaveJsonImageData();
    }

    public void AddVideoToImageLocal(string imageID, ImageVideo videoToAdd)
    {
        ReferenceImage image = GetImageData(imageID);
        image.imageVideos.Add(videoToAdd);
        SaveJsonImageData();
    }

    public void RemoveVideoFromImageLocal(string imageID, ImageVideo videoToRemove)
    {
        ReferenceImage image = GetImageData(imageID);
        image.imageVideos.Remove(videoToRemove);
        SaveJsonImageData();
    }

    public ReferenceImage GetImageData(string imageID)
    {
        ReferenceImage imageToReturn = data.referenceImages.Find(x => x.id == imageID);
        return imageToReturn;
    }

    public void LoadJsonImageData()
    {
        data = new ReferenceImageData();
        string json = ReadFromFile(file);
        if (!string.IsNullOrEmpty(json))
            data = JsonUtility.FromJson<ReferenceImageData>(json);
        else
        {
            SaveJsonImageData();
            LoadJsonImageData();
        }
    }

    public void SaveJsonImageData()
    {
        string json = JsonUtility.ToJson(data);
        WriteToFile(file, json);
    }

    public void LoadJsonSessionData()
    {
        sessionData = new SessionData();
        string json = ReadFromFile(sessionFile);
        if (!string.IsNullOrEmpty(json))
            sessionData = JsonUtility.FromJson<SessionData>(json);
        else
        {
            SaveJsonSessionData();
            LoadJsonSessionData();
        }
    }

    public void SaveJsonSessionData()
    {
        string json = JsonUtility.ToJson(sessionData);
        WriteToFile(sessionFile, json);
    }

    // Writes a JSON string to a file
    private void WriteToFile(string fileName, string json)
    {
        string path = GetFilePath(fileName);
        try
        {
            // Write to file
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            // Display an error notification if the write operation fails
            notificationLogic.DisplayNotification("File Writing Exception", $"Failed to write to file {path}: {e.Message}", "error");
        }
    }

    // Reads a JSON string from a file
    private string ReadFromFile(string fileName)
    {
        string path = GetFilePath(fileName);
        try
        {
            // Check if the file exists
            if (File.Exists(path))
                // Read the contents of the file
                return File.ReadAllText(path);
            else
                return string.Empty;
        }
        catch (Exception e)
        {
            // Display an error notification if the read operation fails
            notificationLogic.DisplayNotification("File Reading Exception", $"Failed to read from file {path}: {e.Message}", "error");
            return string.Empty;
        }
    }

    // Return playable video link from provided url
    public string GetCutVideoLink(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;
        else if (videoFormatRegex.IsMatch(url))
            return url;
        else
        {
            // Normalize Youtube URL such that the video ID appears in the query string
            url = url
                .Trim()
                .Replace("youtu.be/", "youtube.com/watch?v=")
                .Replace("youtube.com/embed/", "youtube.com/watch?v=")
                .Replace("youtube.com/shorts/", "youtube.com/watch?v=")
                .Replace("?feature=share", "")
                .Replace("/watch#", "/watch?");

            if (url.Contains("/v/"))
            {
                var absolutePath = new Uri(url).AbsolutePath;
                absolutePath = absolutePath.Replace("/v/", "/watch?v=");
                url = $"https://youtube.com{absolutePath}";
            }

            // URL should now contain a query string of the format v={video-id} to get googlevideo source
            var queryString = new Uri(url).Query;
            var query = HttpUtility.ParseQueryString(queryString);
            return "https://" + "y.com.sb/latest_version?id=" + query.Get("v") + "&itag=22";
        }
    }

    // Get path of session or image data
    private string GetFilePath(string fileName)
    {
        return Application.persistentDataPath + "/" + fileName;
    }

    // Get path of a specific image with ID
    public string GetImageFilePath(string imageName)
    {
        return Application.persistentDataPath + "/Libraries/" + sessionData.activeLibrary + "/images/" + imageName + ".jpg";
    }

    // Potentially fixes a crash with disabling active video players on some devices
    // Put into DataManager, as most other classes already have access to it
    public IEnumerator DisableLastActiveVideoPlayer(Action<string> callback = null)
    {
        if (lastActivePlane != null)
        {
            VideoPlayer vp = lastActivePlane.GetComponent<VideoPlayer>();
            if (vp != null)
            {
                try
                {
                    vp.Pause();
                    lastActivePlane.GetComponent<MeshRenderer>().enabled = false;
                    vp.enabled = false;
                }
                catch (Exception e)
                {
                    notificationLogic.DisplayNotification("Disabling Last Video Failed", e.Message, "error");
                }

                yield return null;

                try
                {
                    lastActivePlane.GetComponent<PlaneClickHandler>().EnablePlayIcon();
                    lastActivePlane.GetComponent<MeshRenderer>().enabled = true;
                    lastActivePlane = null;
                }
                catch (Exception e)
                {
                    notificationLogic.DisplayNotification("Disabling Last Video Failed", e.Message, "error");
                }
            }
            else
            {
                notificationLogic.DisplayNotification("Last Videoplayer Null", "This shouldn't happen.", "error");
            }
        }
        callback("finished");
    }
}