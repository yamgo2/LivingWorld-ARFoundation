using System;
using System.Collections.Generic;

[Serializable]
public class ReferenceImageData
{
    public List<ReferenceImage> referenceImages;
}

[Serializable]
public class ReferenceImage
{
    public string id;

    public List<ImageVideo> imageVideos = new List<ImageVideo>();
}

[Serializable]
public class DatabaseReferenceImageIdList
{
    public List<DatabaseReferenceImageId> databaseReferenceImageIds;
}

[Serializable]
public class DatabaseReferenceImageId
{
    public string imageid;
}

[Serializable]
public class DatabaseReferenceImage
{
    public string imageid;

    public string library;

    public string image;
}

[Serializable]
public class ImageVideoList
{
    public List<ImageVideo> imageVideos = new List<ImageVideo>();
}

[Serializable]
public class ImageVideo
{
    public string videoid;

    public string imageid = null;

    public string creator;

    public string title;

    public string link;

    public int likes;

    public float width = 16;

    public float height = 9;
}