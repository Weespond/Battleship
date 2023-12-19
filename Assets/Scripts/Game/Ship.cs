using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class Ship : MonoBehaviour
{
    #region Private Properties

    float zPosition;
    bool isDragging;

    Vector3 defaultPosition;
    Quaternion defaultRotation;

    Vector3 curPosition;
    Quaternion curRotation;

    ShipsGrid shipsGrid;
    Collider gridCollider;

    Camera playerCamera;     

    BoxCollider ownCollider;
    List<Transform> decksList;

    GameObject deckPrefab;    

    Material whenStatic;
    Material whenDragging;
    Material whenIntersects;

    #endregion

    [SerializeField] public int deckAmount;

    public Vector3 CurPosition { get { return curPosition; } set { curPosition = value; } }
    public Quaternion CurRotation { get { return curRotation; } set { curRotation = value; } }

    void Awake()
    {         
        shipsGrid = transform.parent.GetComponent<ShipsGrid>();
        gridCollider = shipsGrid.gameObject.GetComponent<Collider>();
        playerCamera = GameObject.Find("MainCamera").GetComponent<Camera>();


        ownCollider = GetComponent<BoxCollider>();
        ownCollider.size = new Vector3(deckAmount + 2, 2, 3);
        ownCollider.center = new Vector3(((float)deckAmount - 1) / 2, 0, 0);              

        deckPrefab = Resources.Load("Deck") as GameObject;
        
        decksList = new List<Transform>();
        for (int i = 0; i < deckAmount; i++)
        {
            GameObject deck = Instantiate(deckPrefab, transform, false);
            deck.transform.localPosition = new Vector3((float)i, 0f, 0f);
            decksList.Add(deck.transform);
        }        

        whenStatic = Resources.Load("Material/Static", typeof(Material)) as Material;
        whenDragging = Resources.Load("Material/Transparent", typeof(Material)) as Material;
        whenIntersects = Resources.Load("Material/Red", typeof(Material)) as Material;        
    }

    void Start()
    {
        defaultPosition = transform.position;
        defaultRotation = transform.rotation;
        curPosition = defaultPosition;
        curRotation = defaultRotation;
        zPosition = playerCamera.WorldToScreenPoint(transform.position).z;        
    }
    public bool ShipIsInsideGrid()
    {
        for (int i = 0; i < deckAmount; i++)
        {
            if (!gridCollider.bounds.Contains(decksList[i].transform.position))
            {
                return false;
            }
        }
        return true;
    }

    public bool DeckOverlappsAnyOtherShip(Transform deck)
    {
        BoxCollider deckCollider = deck.GetComponent<BoxCollider>();
        for (int i = 0; i < 10; i++)
        {
            if (deck.parent.gameObject.GetInstanceID() != shipsGrid.ShipsColliders()[i].gameObject.GetInstanceID())
                if (deckCollider.bounds.Intersects(shipsGrid.ShipsColliders()[i].bounds))
                    return true;
        }
        return false;
    }

    void Update()
    {
        if (isDragging)
        {
            shipsGrid.IsDragging = true;

            Vector3 position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, zPosition);           

            transform.position = playerCamera.ScreenToWorldPoint(position);

            bool shipIsInsideGrid = ShipIsInsideGrid();

            if (shipIsInsideGrid)
            {
                transform.localPosition = new Vector3(
                    Mathf.Round(transform.localPosition.x - .5f) + .5f,
                    transform.localPosition.y,
                    Mathf.Round(transform.localPosition.z + .5f) - .5f);                
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform deck = transform.GetChild(i);
                MeshRenderer deckRenderer = deck.GetComponent<MeshRenderer>();

                deckRenderer.material = whenDragging;
                if (shipIsInsideGrid && DeckOverlappsAnyOtherShip(deck))
                {
                    deckRenderer.material = whenIntersects;
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                transform.rotation *= Quaternion.Euler(0, 90, 0);
                FindObjectOfType<AudioManager>().Play("TURNSHIP");
            }
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                FindObjectOfType<AudioManager>().Play("DELETESHIP");
                DeleteShip();                
            }
        }        
    }

    void OnMouseDown()
    {
        if (!isDragging)
        {
            
            BeginDrag();
            transform.localScale = new Vector3(1f, 0.4f, 1f);            
        }
    }

    void OnMouseUp()
    {
        if (isDragging)
        {
            FindObjectOfType<AudioManager>().Play("PLACESHIP");
            EndDrag();            
        }
    }

    public void BeginDrag()
    {
        isDragging = true;        
    }

    public void EndDrag()
    {
        isDragging = false;
        shipsGrid.IsDragging = false;
        
        bool overlapsAnyOtherShip = false;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform deck = transform.GetChild(i);
            deck.transform.GetComponent<MeshRenderer>().material = whenStatic;
            overlapsAnyOtherShip |= DeckOverlappsAnyOtherShip(deck);
        }

        if (!ShipIsInsideGrid() || overlapsAnyOtherShip) 
        {
            if (curPosition != defaultPosition)
            {
                transform.localPosition = curPosition;
                transform.localRotation = curRotation;                
            }
            else
            {
                DeleteShip();
            }
        }
        else
        {
            curPosition = transform.localPosition;
            curRotation = transform.rotation;
        }

        shipsGrid.SwitchReadyState();
    }
       
    public void DeleteShip()
    {
        isDragging = false;
        shipsGrid.IsDragging = false;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform deck = transform.GetChild(i);
            deck.transform.GetComponent<MeshRenderer>().material = whenStatic;
        }

        transform.position = defaultPosition;
        transform.rotation = defaultRotation;
        curPosition = transform.position;
        curRotation = transform.rotation;
        transform.localScale = new Vector3(0.5f, 0.2f, 0.5f);

        shipsGrid.SwitchReadyState();
    }
    public void DeleteAllShips()
    {        
        for (int i = 0; i < 10; i++)
        {
            shipsGrid.gameObject.transform.GetChild(i).GetComponent<Ship>().DeleteShip();
        }
    }

}
