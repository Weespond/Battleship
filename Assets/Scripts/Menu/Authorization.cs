using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Authorization : MonoBehaviour
{
    public static string nickname;
    public static bool isAuthorized;

    [SerializeField] private GameObject errorPanel = null;
    [SerializeField] private Text textField = null;

    bool IsAllLettersOrDigits(string s)
    {
        foreach (char c in s)
        {
            if (!char.IsLetterOrDigit(c))
                return false;
        }
        return true;
    }

    public void Authorize()
    {
        string name = textField.text;

        if ((string.IsNullOrEmpty(name)) || (name.Length < 5))
        {
            errorPanel.SetActive(true);
            errorPanel.transform.Find("MinLength").gameObject.SetActive(true);
            errorPanel.transform.Find("WrongSymbals").gameObject.SetActive(false);
            return;
        }
        else if (!IsAllLettersOrDigits(name))
        {
            errorPanel.SetActive(true);
            errorPanel.transform.Find("MinLength").gameObject.SetActive(false);
            errorPanel.transform.Find("WrongSymbals").gameObject.SetActive(true);
            return;
        }
        errorPanel.SetActive(false);

        nickname = name;
        isAuthorized = true;

        gameObject.GetComponent<MainMenu>().ReloadMenu();
    }
}