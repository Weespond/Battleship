using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Linq;
public class ScoreManager : MonoBehaviour
{
    public enum SortingType
    {
        SortByWinrate,
        SortByAccuracy
    }
    [SerializeField] private GameObject scoreList = null;
    [SerializeField] private GameObject scoreEntryPrefab = null;
    [SerializeField] private Animator loadingRingAnimator = null;
    
    public SortingType currentSortingType = SortingType.SortByWinrate;

    public const string privateCode = "Bkbb-mZf30CUQHzbrVt-5ghz58on7kQ0SF6kLdpGa99g";
    public const string publicCode = "603126ed8f40bb39ec1ed838";
    public const string webURL = "http://dreamlo.com/lb/";

    List<string[]> entryList = new List<string[]>();

    public void ResetLoadingRing()
    {        
        loadingRingAnimator.SetBool("Loading", false);        
        loadingRingAnimator.gameObject.GetComponent<Image>().color = new Color(255f, 255f, 255f, 0f);
    }
    public void SortByWinrate()
    {
        currentSortingType = SortingType.SortByWinrate;
        LoadEntriesFromList(currentSortingType);
    }
    public void SortByAccuracy()
    {
        currentSortingType = SortingType.SortByAccuracy;
        LoadEntriesFromList(currentSortingType);
    }
    void OnEnable() 
    {
        loadingRingAnimator.SetBool("Loading", false);
        GetLeaderboard(); 
    }
    public void GetLeaderboard() => StartCoroutine(GetScores());
    IEnumerator GetScores()
    {
        bool doneHere = false;
        loadingRingAnimator.SetBool("Loading", true);
        while (!doneHere)
        {            
            var get = new UnityWebRequest(webURL + publicCode + "/pipe/");
            get.downloadHandler = new DownloadHandlerBuffer();

            yield return get.SendWebRequest();

            if (string.IsNullOrEmpty(get.error))
            {
                doneHere = true;

                FormEntryList(get.downloadHandler.text);
                print("get success! " + get.downloadHandler.text);

                loadingRingAnimator.SetBool("Loading", false);
            }
            else
            {
                yield return new WaitForSeconds(1f);
                print("get error: " + get.error);
            }
        }
        LoadEntriesFromList(currentSortingType);
    }

    void FormEntryList(string textStream)
    {
        entryList.Clear();

        string[] textStreamEntries = textStream.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string textEntry in textStreamEntries)
        {
            string[] entryInfo = textEntry.Split(new char[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries);

            string name = entryInfo[0];
            double winRate = double.Parse(entryInfo[1]) / double.Parse(entryInfo[2]);
            double accuracy = 0;
            try
            {
                accuracy = double.Parse(entryInfo[3]) / double.Parse(entryInfo[4]);
            }
            catch (Exception ex) { accuracy = 0; }

            string[] entry = new string[3];
            entry[0] = name;
            entry[1] = winRate.ToString("0.000000");
            entry[2] = accuracy.ToString("0.000000");

            entryList.Add(entry);
        }
    }

    public void LoadEntriesFromList(SortingType sortingType)
    {
        if (sortingType == SortingType.SortByWinrate)
        {
            entryList = entryList.OrderByDescending(x => double.Parse(x[1])).ToList();
        }
        else 
        {
            entryList = entryList.OrderByDescending(x => double.Parse(x[2])).ToList();
        }        

        foreach (Transform child in scoreList.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (string[] entry in entryList)
        {
            GameObject entryObject = Instantiate(scoreEntryPrefab, scoreList.transform);

            entryObject.transform.Find("Name").GetComponent<Text>().text = entry[0];
            entryObject.transform.Find("Winrate").GetComponent<Text>().text = (double.Parse(entry[1]) * 100).ToString("0.000") + "%";
            entryObject.transform.Find("Accuracy").GetComponent<Text>().text = (double.Parse(entry[2]) * 100).ToString("0.000") + "%";
        }
    }

    //        To send:
    //              UnityWebRequest www = new UnityWebRequest(webURL + privateCode + "/add/" + UnityWebRequest.EscapeURL(username) + "/" + score);
    //              yield return www.SendWebRequest();


    //        To receive:
    //              UnityWebRequest www = new UnityWebRequest(webURL + publicCode + "/pipe/");
    //              www.downloadHandler = new DownloadHandlerBuffer();
    //              yield return www.SendWebRequest();
    //              print(www.downloadHandler.text);

}
