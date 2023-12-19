using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Mirror;

public class WindowScript : MonoBehaviour, IDragHandler
{
    //public static Vector2Int defaultWindowSize = new Vector2Int(1024, 768);
    private Vector2 _deltaValue = Vector2.zero;
    private bool _maximized;
    //public Vector2Int borderSize;
    [SerializeField] private GameObject soundPanel = null;
    

    public void Start()
    {
        //if (!Application.isEditor)
        //{
        //    BorderlessWindow.MoveWindowPos(Vector2Int.zero, defaultWindowSize.x, defaultWindowSize.y);
        //    BorderlessWindow.SetFramelessWindow();
        //    BorderlessWindow.MoveWindowPos(Vector2Int.zero, Screen.width - borderSize.x, Screen.height - borderSize.y);
        //}
    }

    public void OnCloseBtnClick()
    {
        if (SceneManager.GetActiveScene().name == "Game_PVP")
        {
            NetworkManagerBS Room = NetworkManager.singleton as NetworkManagerBS;
            Room.IngameDisconnect();
        }
        EventSystem.current.SetSelectedGameObject(null);
        Application.Quit();
    }
    public void OnMinimizeBtnClick()
    {
        EventSystem.current.SetSelectedGameObject(null);
        BorderlessWindow.MinimizeWindow();
    }
    public void OnMaximizeBtnClick()
    {
        EventSystem.current.SetSelectedGameObject(null);
        if (_maximized)
            BorderlessWindow.RestoreWindow();
        else
            BorderlessWindow.MaximizeWindow();
        _maximized = !_maximized;
    }
    public void OnDrag(PointerEventData data)
    {
        if (BorderlessWindow.framed)
            return;
        _deltaValue += data.delta;
        if (data.dragging)
        {
            BorderlessWindow.MoveWindowPos(_deltaValue, Screen.width, Screen.height);
        }
    }

    public void OnSoundBtnClick()
    {
        if (soundPanel != null)
        {
            soundPanel.SetActive(!soundPanel.activeSelf);
        }
    }
}