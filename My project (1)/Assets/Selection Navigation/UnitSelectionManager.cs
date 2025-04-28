using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance { get; set; }

    public Abilities abilities; //referencing Abilities script (for hiding Ability UI bar after deselecting character) 
    public GameObject statsPanel; //creating statsPanel

    public List<GameObject> allUnitsList = new List<GameObject>();
    public List<GameObject> unitsSelected = new List<GameObject>();

    public LayerMask clickable;
    public LayerMask ground;
    public GameObject groundMarker;

    private Camera cam;//referecing main camera
    private void Awake()
    {
        if(Instance != null && Instance != this) // if there was already an instance destroy this one
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
        cam = Camera.main; //grab main camera
        UpdateStatsPanelVisibility();
    }


    //ray cast on a character when player click on it to select it
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            //check if we clicked on a clickable object,decide to select or multi-select
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

        if (Input.GetMouseButtonDown(1) && unitsSelected.Count>0) // right click to move unit
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



    //deselect all the unit that were selected
    public void DeselectAll()
    {
        foreach(var unit in unitsSelected) 
        {
            EnableUnitMovement(unit, false); //disable all selected unit's movement after deselected
            TriggerSelectionIndicator(unit, false);//hide select indicator
        }
        groundMarker.SetActive(false);
        unitsSelected.Clear(); //clear all selected units in the list
        if (abilities != null)
        {
            abilities.UpdateAbilityUI();
        }
        UpdateStatsPanelVisibility();
    }

    private  void SelectByClicking(GameObject gameObject)
    {
        DeselectAll(); //before click selecting a object, deselect the previous ones.

        unitsSelected.Add(gameObject);
        TriggerSelectionIndicator(gameObject,true);
        EnableUnitMovement(gameObject, true); //enable movement for selected units

        UpdateStatsPanelVisibility();
    }

    private void EnableUnitMovement(GameObject unit, bool moveable)
    {
        var movement = unit.GetComponent<unitMovement>();
        if (movement != null)
        {
            movement.enabled = moveable;
        }
    }

    // function to add or remove units from selection when shift is held
    private void MultiSelect(GameObject unit)
    {
        if(unitsSelected.Contains(unit)== false)
        {
            unitsSelected.Add(unit);
            TriggerSelectionIndicator(unit, true); //make selecting indicator visible
            EnableUnitMovement(unit, true);
        }
        else // if the unit is already selected, deselect it
        {
            EnableUnitMovement(unit,false);
            TriggerSelectionIndicator(unit, false); // make selecting indicator invisible
            unitsSelected.Remove(unit);
        }
        UpdateStatsPanelVisibility();

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
            unitsSelected.Add(unit); // if the unit isn't already selected, add it
            TriggerSelectionIndicator(unit, true);
            EnableUnitMovement(unit, true);
            UpdateStatsPanelVisibility();
        }

    }

    // function to update the stats panel visibility based on whether any units are already selected
    private void UpdateStatsPanelVisibility()
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(unitsSelected.Count > 0);
        }
    }

}
