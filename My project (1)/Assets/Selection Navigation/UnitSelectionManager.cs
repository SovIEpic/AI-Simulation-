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

            if (Physics.Raycast(ray, out hit))
            {
                if (((1 << hit.collider.gameObject.layer) & clickable) != 0) // shift click for multi select
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
                    DeselectAll(); // clicked on ground, deselect
                }
                else
                {
                    if (!Input.GetKey(KeyCode.LeftShift))
                    {
                        DeselectAll(); // clicked somewhere else, deselect unless shift
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
        // right click stuff(not used in demo version)
        if (Input.GetMouseButtonDown(1) && unitsSelected.Count > 0)
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, ground))
            {
                groundMarker.transform.position = hit.point;
                groundMarker.SetActive(false);
                groundMarker.SetActive(true);
            }
        }
    }



    //deselect all the unit that were selected
    public void DeselectAll()
    {
        foreach (var unit in unitsSelected)
        {
            EnableUnitMovement(unit, false);
            TriggerSelectionIndicator(unit, false);

            // hide ability UI for all deselected units
            var abilities = unit.GetComponent<Abilities>();
            if (abilities != null)
            {
                abilities.SetAbilityUIActive(false);
            }
        }

        groundMarker.SetActive(false);
        unitsSelected.Clear();
        UpdateStatsPanelVisibility();
    }

    // select one unit by clicking
    private void SelectByClicking(GameObject gameObject)
    {
        DeselectAll(); // clear previous selection

        unitsSelected.Add(gameObject);
        TriggerSelectionIndicator(gameObject, true); // show selection circle
        EnableUnitMovement(gameObject, true); // allow movement

        var abilities = gameObject.GetComponent<Abilities>();
        if (abilities != null)
        {
            abilities.SetAbilityUIActive(true); // show ability ui
        }

        UpdateStatsPanelVisibility(); // show stats panel
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
