using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LoginLogic : MonoBehaviour
{
    // Only required in LoginLogic, therefore declared here
    [Serializable]
    public class User
    {
        public string name;
        public string password;
    }

    public GameObject loginScreen;
    public GameObject libraryScreen;
    public GameObject createLibraryScreen;
    public GameObject registerScreen;
    public GameObject loadingScreen;

    public TMP_Text loadingMessage;

    public DataManager dataManager;

    public TMP_InputField inputFieldUsername;
    public TMP_InputField inputFieldPassword;
    public TMP_InputField inputFieldLibraryName;
    public TMP_InputField inputFieldLibraryDescription;
    public TMP_InputField inputFieldRegisterUsername;
    public TMP_InputField inputFieldRegisterPassword;

    public Animator cancelButtonAnimator;
    public Animator confirmButtonAnimator;

    public GameObject libraryStatsGo;
    public TMP_Text libraryDescriptionText;
    public TMP_Text viewsText;
    public TMP_Text imageCountText;

    public GameObject casheLocalToggleGo;
    public Toggle casheLocalToggle;

    public TextFieldActiveHandler usernameActiveHandler;

    public TMP_Dropdown libDropdown;

    public ReferenceLibraryManager referenceLibraryManager;

    public NotificationLogic notificationLogic;

    public AudioSource audioManager;

    public AudioClip selectionChange;
    public AudioClip startSound;

    private List<string> libraryNameList = new List<string>();

    private const int JpegQuality = 85;

    // Start is called before the first frame update
    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // Load or create local session data and reset deleted images cashe
        dataManager.LoadJsonSessionData();
        dataManager.ClearDeletedImages();

        // If image cashing was disabled reset current library folder in case of any remnants (should normally be empty)
        if (!dataManager.SessionData.casheImages && dataManager.SessionData.activeLibrary != "Local")
            dataManager.ResetLibraryFolder(dataManager.SessionData.activeLibrary);


        // Load last active user into input field for faster login
        string lastActiveUser = dataManager.SessionData.lastActiveUser;
        if (lastActiveUser != "")
        {
            inputFieldUsername.text = dataManager.SessionData.lastActiveUser;
            usernameActiveHandler.InputFieldDeselect();
        }

        // Check server availability and set local mode if not available
        StartCoroutine(dataManager.RequestHandler($"{dataManager.apiUrl}/", "GET", "", (response, errorMessage) =>
        {
            // Fix for first sound not being affected by AudioMixer volume
            audioManager.PlayOneShot(startSound);

            if (response.Equals("failure"))
            {
                dataManager.SessionData.localMode = true;
                dataManager.SetActiveUser("local");
                loginScreen.SetActive(false);
                SetLibraries();
            }
            else
            {
                dataManager.SessionData.localMode = false;
                loadingScreen.SetActive(false);
                notificationLogic.DisplayNotification("Connected To Server", "Connection established.", "information");
            }
        }));
    }

    // Go to register button clicked
    public void GoToRegister()
    {
        inputFieldRegisterUsername.text = "";
        inputFieldRegisterPassword.text = "";
        inputFieldRegisterUsername.GetComponent<TextFieldActiveHandler>().InputFieldDeselect();
        inputFieldRegisterPassword.GetComponent<TextFieldActiveHandler>().InputFieldDeselect();
        registerScreen.SetActive(true);
    }

    // Back to login button clicked
    public void BackToLogin()
    {
        registerScreen.SetActive(false);
    }

    // Register button clicked
    public void Register()
    {
        string username = inputFieldRegisterUsername.text;
        string password = inputFieldRegisterPassword.text;

        // Perform input validation
        if (string.IsNullOrEmpty(username))
        {
            notificationLogic.DisplayNotification("Invalid Input", "Username can't be empty.", "warning");
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            notificationLogic.DisplayNotification("Invalid Input", "Password can't be empty.", "warning");
            return;
        }
        if (password.Length < 5)
        {
            notificationLogic.DisplayNotification("Invalid Input", "Password must be at least 5 characters.", "warning");
            return;
        }

        loadingScreen.SetActive(true);
        loadingMessage.text = "Creating new user";

        User newUser = new User
        {
            name = username,
            password = password
        };

        string json = JsonUtility.ToJson(newUser);

        // Send user to be saved in the database and only continue if successful
        StartCoroutine(dataManager.RequestHandler($"{dataManager.apiUrl}/users", "POST", json, (response, errorMessage) =>
        {
            // Check if user was created successfully in database
            if (response.Equals("failure"))
                notificationLogic.DisplayNotification("Registration Failed", errorMessage, "error");
            else
            {
                registerScreen.SetActive(false);
                notificationLogic.DisplayNotification("Registration Successful", "User '" + newUser.name + "' saved to database.", "information");
            }
            loadingScreen.SetActive(false);
        }));
    }

    // Triggers when login button is clicked on login screen
    public void LoginButtonClicked()
    {
        string username = inputFieldUsername.text;
        string password = inputFieldPassword.text;

        // Perform input validation
        if (string.IsNullOrEmpty(username))
        {
            notificationLogic.DisplayNotification("Invalid Input", "Username can't be empty.", "warning");
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            notificationLogic.DisplayNotification("Invalid Input", "Password can't be empty.", "warning");
            return;
        }
        if (password.Length < 5)
        {
            notificationLogic.DisplayNotification("Invalid Input", "Password too short.", "warning");
            return;
        }

        loadingScreen.SetActive(true);
        loadingMessage.text = "Checking login data";

        User user = new User
        {
            name = username,
            password = password
        };

        string json = JsonUtility.ToJson(user);

        // Check login data
        StartCoroutine(dataManager.RequestHandler($"{dataManager.apiUrl}/users/login", "POST", json, (response, errorMessage) =>
        {
            if (response.Equals("failure"))
            {
                notificationLogic.DisplayNotification("Login Failed", errorMessage, "error");
                loadingScreen.SetActive(false);
            }
            else
            {
                // Set active user and continue to library selection
                dataManager.SetActiveUser(inputFieldUsername.text);
                dataManager.SetLastActiveUser(inputFieldUsername.text);
                casheLocalToggle.isOn = dataManager.SessionData.casheImages;
                GetLibraries();
            }
        }));
    }

    // Get list of available libraries from server
    private void GetLibraries()
    {
        loadingScreen.SetActive(true);
        loadingMessage.text = "Requesting libraries";

        // Reset library data of last active session
        dataManager.SessionData.availableLibraries.Clear();
        libraryNameList.Clear();

        // Try to get all available libraries from remote REST API
        StartCoroutine(dataManager.RequestHandler($"{dataManager.apiUrl}/libraries", "GET", "", (response, errorMessage) =>
        {
            if (response.Equals("failure"))
            {
                notificationLogic.DisplayNotification("Fetch Libraries Failed", errorMessage, "error");
                loadingScreen.SetActive(true);
            }
            else
            {
                ReferenceLibraryList libs = JsonUtility.FromJson<ReferenceLibraryList>("{\"referenceLibraries\":" + response + "}");
                foreach (var lib in libs.referenceLibraries)
                {
                    dataManager.SessionData.availableLibraries.Add(lib);
                    libraryNameList.Add(lib.name);
                }
                SetLibraries();
            }
        }));
    }

    // Set libraries locally to be displayed in the dropdown
    private void SetLibraries()
    {
        dataManager.CreateFolders();

        List<string> libraryDropdownList = new List<string>
            {
                // Always have local option at top in dropdown menu
                "Local"
            };
        // Add option to create new library if connected to server
        if (!dataManager.SessionData.localMode)
            libraryDropdownList.Add("- Create New -");
        libDropdown.AddOptions(libraryDropdownList);
        // Add all available library names to dropdown, then clear list
        libDropdown.AddOptions(libraryNameList);

        // Fix dropdown display error
        libraryDropdownList.Clear();
        libraryDropdownList.Add("empty fix");
        libDropdown.AddOptions(libraryDropdownList);

        // Select library from last session
        string itemToFind = dataManager.SessionData.activeLibrary;

        if (itemToFind != "")
            libDropdown.value = libDropdown.options.FindIndex(option => option.text == itemToFind);

        // In case initial dropdown selection is "Local"
        if (libDropdown.options[libDropdown.value].text == "Local")
        {
            casheLocalToggleGo.SetActive(false);
            libraryStatsGo.SetActive(false);
            libraryDescriptionText.text = "Using this library, your images and video won't be added to the server database and will only be locally available  on your device.";
        }

        loginScreen.SetActive(false);
        loadingScreen.SetActive(false);
    }

    // Triggers on every OnValueChange-event of library dropdown
    public void LibrarySelectionChanged()
    {
        audioManager.PlayOneShot(selectionChange);

        // Show or hide library input field and cashe toggle depending on dropdown selection
        switch (libDropdown.options[libDropdown.value].text)
        {
            case "- Create New -":
                casheLocalToggleGo.SetActive(false);
                libraryStatsGo.SetActive(false);
                libraryDescriptionText.text = "Allows you to create a new library on the next screen.";
                break;
            case "Local":
                casheLocalToggleGo.SetActive(false);
                libraryStatsGo.SetActive(false);
                libraryDescriptionText.text = "Using this library, your images and videos won't be added to the server database and are only available locally on your device.";
                break;
            default:
                casheLocalToggleGo.SetActive(true);
                libraryStatsGo.SetActive(true);
                ReferenceLibrary selectedLib = dataManager.SessionData.availableLibraries.Find(x => x.name == libDropdown.options[libDropdown.value].text);
                viewsText.text = selectedLib.views.ToString();
                imageCountText.text = selectedLib.imagecount.ToString();
                libraryDescriptionText.text = selectedLib.description;
                break;
        }
    }

    // Triggers when continue button is clicked on library screen
    public void ContinueButtonClicked()
    {
        if (libDropdown.options[libDropdown.value].text == "- Create New -")
            createLibraryScreen.SetActive(true);
        // If a library is selected
        else
        {
            dataManager.SetActiveLibrary(libDropdown.options[libDropdown.value].text);
            //If "Local" selected in dropdown, set local mode to true
            if (libDropdown.options[libDropdown.value].text == "Local")
            {
                dataManager.SessionData.localMode = true;
                dataManager.SetActiveUser("local");
                LoginSuccessful();
            }
            else
            {
                // Add a view to selected library, then proceed
                StartCoroutine(dataManager.RequestHandler($"{dataManager.apiUrl}/libraries/{dataManager.SessionData.activeLibrary}/views", "PUT", "", (response, errorMessage) =>
                {
                    if (response.Equals("failure"))
                        notificationLogic.DisplayNotification("Add View Failed", errorMessage, "error");
                    else
                        LoginSuccessful();
                }));
            }
        }
    }

    // Confirm button in library creation screen clicked
    public void ConfirmButtonClicked()
    {
        string libraryName = inputFieldLibraryName.text;
        string libraryDescription = inputFieldLibraryDescription.text;

        // Perform input validation
        if (string.IsNullOrEmpty(libraryName))
        {
            notificationLogic.DisplayNotification("Invalid Input", "Library name can't be empty.", "warning");
            return;
        }
        // Warning if library already exists (StringComparer argument so that comparison to available libraries is case insensitive)
        if (libraryNameList.Contains(libraryName, StringComparer.OrdinalIgnoreCase) || libraryName.ToLower() == "local" || libraryName == "- Create New -")
        {
            notificationLogic.DisplayNotification("Invalid Input", "Library already exists.", "warning");
            return;
        }
        if (string.IsNullOrEmpty(libraryDescription))
        {
            notificationLogic.DisplayNotification("Invalid Input", "Please enter a short description of the library.", "warning");
            return;
        }

        loadingScreen.SetActive(true);
        loadingMessage.text = "Creating library";

        ReferenceLibrary libraryToAdd = new ReferenceLibrary
        {
            name = libraryName,
            description = libraryDescription,
            creator = dataManager.SessionData.activeUser
        };

        string json = JsonUtility.ToJson(libraryToAdd);

        StartCoroutine(dataManager.RequestHandler($"{dataManager.apiUrl}/libraries", "POST", json, (response, errorMessage) =>
        {
            if (response.Equals("failure"))
            {
                notificationLogic.DisplayNotification("Library Creation Failed", errorMessage, "error");
                loadingScreen.SetActive(false);
            }
            else
            {
                // If library was created successfully in database, create local folder and continue
                notificationLogic.DisplayNotification("Library Creation Successful", "Library '" + libraryToAdd.name + "' saved to database.", "information");
                dataManager.AddLibrary(libraryToAdd);
                dataManager.CreateFolders();
                dataManager.SetActiveLibrary(libraryToAdd.name);
                libDropdown.ClearOptions();
                CancelButtonClicked();
                GetLibraries();
            }
        }));
    }

    // Resets library creation screen
    public void CancelButtonClicked()
    {
        inputFieldLibraryName.text = "";
        inputFieldLibraryDescription.text = "";
        inputFieldLibraryName.GetComponent<TextFieldActiveHandler>().InputFieldDeselect();
        inputFieldLibraryDescription.GetComponent<TextFieldActiveHandler>().InputFieldDeselect();
        // Fix stuck button animation
        cancelButtonAnimator.Play("Normal", 0, 0f);
        confirmButtonAnimator.Play("Normal", 0, 0f);
        createLibraryScreen.SetActive(false);
    }

    // Library selection was succesfull
    public void LoginSuccessful()
    {
        loadingMessage.text = "Fetching images";
        loadingScreen.SetActive(true);

        dataManager.LoadJsonImageData();

        // If in local mode, load all local images into mutable reference image library
        if (dataManager.SessionData.localMode)
            LoadImagesIntoMutableLibrary();
        else
        {
            // Check and set cashe toggle
            dataManager.SessionData.casheImages = casheLocalToggle.isOn;
            dataManager.SaveJsonSessionData();

            // Get all image IDs for the selected library from server
            StartCoroutine(dataManager.RequestHandler($"{dataManager.apiUrl}/libraries/{dataManager.SessionData.activeLibrary}/imageids", "GET", "", (response, errorMessage) =>
            {
                if (response.Equals("failure"))
                {
                    notificationLogic.DisplayNotification("Fetch Image IDs Failed", errorMessage, "error");
                    loadingScreen.SetActive(false);
                }
                else
                {
                    DatabaseReferenceImageIdList idList = JsonUtility.FromJson<DatabaseReferenceImageIdList>("{\"databaseReferenceImageIds\":" + response + "}");
                    // If images not cashed, delete local library and empty ReferenceImageData.json
                    if (!dataManager.SessionData.casheImages)
                        dataManager.ResetLibraryFolder(dataManager.SessionData.activeLibrary);
                    StartCoroutine(GetImagesFromIdList(idList));
                }
            }));
        }
    }

    // Starts all WebRequests for remote images to be pulled and waits for all to finish
    IEnumerator GetImagesFromIdList(DatabaseReferenceImageIdList idList)
    {
        var requests = new List<UnityWebRequestAsyncOperation>();

        // Check for all server image ids if image id already exists locally
        foreach (var imageId in idList.databaseReferenceImageIds)
        {
            // If image id doesn't exist, send a GET request for the image
            if (!dataManager.Data.referenceImages.Any(i => i.id == imageId.imageid))
            {
                // Start all requests, but don't wait for them for now
                UnityWebRequest req = UnityWebRequest.Get(dataManager.apiUrl + "/images/" + imageId.imageid);
                requests.Add(req.SendWebRequest());
            }
        }
        // Now wait for all requests parallel
        yield return new WaitUntil(() => AllRequestsDone(requests));

        // Now evaluate all results
        HandleAllRequestsWhenFinished(requests);

        List<ReferenceImage> temporaryList = new List<ReferenceImage>(dataManager.Data.referenceImages);

        // Delete images that still exist locally but not on server
        foreach (var image in temporaryList)
        {
            if (!idList.databaseReferenceImageIds.Any(i => i.imageid == image.id))
            {
                dataManager.RemoveImageLocal(image.id);
                File.Delete(dataManager.GetImageFilePath(image.id));
            }
        }
        LoadImagesIntoMutableLibrary();

        // Clean up resources
        foreach (var request in requests)
        {
            request.webRequest.Dispose();
        }
    }

    private bool AllRequestsDone(List<UnityWebRequestAsyncOperation> requests)
    {
        // Possible because of Linq: returns true if All requests are done
        return requests.All(r => r.isDone);
    }

    // Evaluates results of all previously finished requests
    private void HandleAllRequestsWhenFinished(List<UnityWebRequestAsyncOperation> requests)
    {
        foreach (var request in requests)
        {
            var req = request.webRequest;
            if (req.result != UnityWebRequest.Result.Success)
                notificationLogic.DisplayNotification("GET Error", req.error, "error");
            else
            {
                DatabaseReferenceImage dbImage = JsonUtility.FromJson<DatabaseReferenceImage>(req.downloadHandler.text);
                ReferenceImage imageToAdd = new ReferenceImage { id = dbImage.imageid };
                // Decode Base64 string and save image locally into .jpg 
                byte[] imageBytes = Convert.FromBase64String(dbImage.image);
                Texture2D loadTexture = new Texture2D(1, 1);
                loadTexture.LoadImage(imageBytes);
                File.WriteAllBytes(dataManager.GetImageFilePath(imageToAdd.id), loadTexture.EncodeToJPG(JpegQuality));
                // Add image id to local json for current library
                dataManager.AddImageLocal(imageToAdd);
            }
        }
    }

    private void LoadImagesIntoMutableLibrary()
    {
        // If no reference images exist skip the loading screen
        if (!dataManager.Data.referenceImages.Any())
        {
            libraryScreen.SetActive(false);
            loadingScreen.SetActive(false);
        }
        else
        {
            bool errorLogged = false;
            // Read all image files as bytes, create a texture from them and load them into the reference library
            foreach (ReferenceImage refImage in dataManager.Data.referenceImages)
            {
                try
                {
                    byte[] bytes;
                    bytes = File.ReadAllBytes(dataManager.GetImageFilePath(refImage.id));

                    // Delete local image after adding it to the mutable AR library if cashing is disabled
                    if (!dataManager.SessionData.casheImages && !dataManager.SessionData.localMode)
                        File.Delete(dataManager.GetImageFilePath(refImage.id));

                    Texture2D loadTexture = new Texture2D(1, 1);
                    loadTexture.LoadImage(bytes);
                    bool success = referenceLibraryManager.AddToReferenceLibrary(loadTexture, refImage.id);
                    if (!success)
                        errorLogged = true;
                }
                catch (Exception e)
                {
                    notificationLogic.DisplayNotification("File Reading Exception", e.Message, "error");
                    errorLogged = true;
                }
            }
            loadingScreen.SetActive(false);
            // Only continue if no errors were logged
            if (!errorLogged)
                libraryScreen.SetActive(false);
        }
    }
}
