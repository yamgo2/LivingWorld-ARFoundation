using TMPro;
using UnityEngine;

public class TextFieldActiveHandler : MonoBehaviour
{
    public GameObject textfieldFilledActive;

    public GameObject placeholder;

    public GameObject placeholderActive;

    private TMP_InputField inputField;

    // Set input field state on deselection based on empty or existing input
    public void InputFieldDeselect()
    {
        inputField = gameObject.GetComponent<TMP_InputField>();
        if (inputField.text != "")
        {
            textfieldFilledActive.SetActive(true);
            placeholder.SetActive(false);
            placeholderActive.SetActive(true);
        }
        else
        {
            textfieldFilledActive.SetActive(false);
            placeholderActive.SetActive(false);
            placeholder.SetActive(true);
        }
    }
}
