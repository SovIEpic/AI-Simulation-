using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance { get; set; }

    public Abilities abilities; //referencing Abilities script (for hiding Ability UI bar after deselecting character) 

    public List<GameObject> allUnitsList = new List<GameObject>();
    public List<GameObject> unitsSelected = new List<GameObject>();

    public LayerMask clickable;
    public LayerMask ground;
    public GameObject groundMarker;

    private Camera cam;


    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        cam = Camera.main;
    }


    //ray cast on a character to select it
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            //check if clicking on a clickable object, if not just unselect everything
            if (Physics.Raycast(ray, out hit))
            {
                if (((1 << hit.collider.gameObject.layer) & clickable) != 0)
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        MultiSelect(hit.collider.gameObject);
                    }
                    else
                    {
                        SelectByClicking(hit.collider.gameObject);
                    }
                }
                else if (((1 << hit.collider.gameObject.layer) & ground) != 0)
                {
                    DeselectAll(); // Deselect when clicking on the ground
                }
                else
                {
                    if (!Input.GetKey(KeyCode.LeftShift))
                    {
                        DeselectAll();
                    }
                }
            }
            else
            {
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    DeselectAll();
                }
            }

        }

        if (Input.GetMouseButtonDown(1) && unitsSelected.Count>0)
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            //check if clicking on ground
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, ground))
            {
                groundMarker.transform.position = hit.point;
                groundMarker.SetActive(false);
                groundMarker.SetActive(true);
                abilities.UpdateAbilityUI();
            }

        }
    }




    public void DeselectAll()
    {
        foreach(var unit in unitsSelected) 
        {
            EnableUnitMovement(unit, false); //disable all selected unit's movement after deselected
            TriggerSelectionIndicator(unit, false);//hide select indicator
        }
        groundMarker.SetActive(false);
        unitsSelected.Clear(); //clear all selected units.
        if (abilities != null)
        {
            abilities.UpdateAbilityUI();
        }
    }

    private  void SelectByClicking(GameObject gameObject)
    {
        DeselectAll(); //before click selecting a object, deselect the previous ones.

        unitsSelected.Add(gameObject);
        TriggerSelectionIndicator(gameObject,true);
        EnableUnitMovement(gameObject, true);

    }

    private void EnableUnitMovement(GameObject unit, bool moveable)
    {
        var movement = unit.GetComponent<unitMovement>();
        if (movement != null)
        {
            movement.enabled = moveable;
        }
    }

    private void MultiSelect(GameObject unit)
    {
        if(unitsSelected.Contains(unit)== false)
        {
            unitsSelected.Add(unit);
            TriggerSelectionIndicator(unit, true); //make selecting indicator visible
            EnableUnitMovement(unit, true);
        }
        else
        {
            EnableUnitMovement(unit,false);
            TriggerSelectionIndicator(unit, false); // make selecting indicator invisible
            unitsSelected.Remove(unit);
        }
    }

    private void TriggerSelectionIndicator(GameObject unit, bool isVisible)
    {
        //set the first child of the object visible (selected indicator)
        if (unit != null && unit.transform.childCount > 0)
        {
            unit.transform.GetChild(0).gameObject.SetActive(isVisible);
        }
    }
    internal void DragSelect(GameObject unit)
    {
        if (!unitsSelected.Contains(unit))
        {
            unitsSelected.Add(unit);
            TriggerSelectionIndicator(unit, true);
            EnableUnitMovement(unit, true);
        }
    }

}
