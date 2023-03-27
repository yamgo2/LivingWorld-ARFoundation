using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class HighlightManager : MonoBehaviour
{
    Color highlighted = new Color32(25, 35, 45, 255);
    Color notHighlighted = new Color32(25, 35, 45, 0);

    // Set background color of active and inactive list items
    public void ListItemToggleActive(GameObject item)
    {
        GameObject[] listViewItems = GameObject.FindGameObjectsWithTag("ListViewItem");

        foreach (GameObject lvItem in listViewItems)
        {
            lvItem.GetComponent<Image>().color = notHighlighted;
        }
        item.GetComponent<Image>().color = highlighted;
    }
}
