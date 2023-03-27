using System;
using System.Collections.Generic;

[Serializable]
public class SessionData
{
    public List<ReferenceLibrary> availableLibraries;

    public List<string> likedVideos;

    public List<string> deletedImages;

    public string activeLibrary;

    public string activeUser;

    public string lastActiveUser;

    public bool stretchVideos = true;

    public bool holoMode = false;

    public bool blackBackground = false;

    public bool localMode = true;

    public bool casheImages = true;
}

[Serializable]
public class ReferenceLibraryList
{
    public List<ReferenceLibrary> referenceLibraries;
}

[Serializable]
public class ReferenceLibrary
{
    public string name;
    public string creator;
    public string description;
    public int views = 0;
    public int imagecount = 0;
}