using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
public class TapToPlaceObject : MonoBehaviour
{
    // Public Attributes Obtained as Param to Script
    public GameObject objectToPlace;
    public GameObject placementIndicator;
    public Slider rotationSlider;
    public Slider scaleSlider;
    public Text modifierText;
    public Button prevButton;
    public Button nextButton;
    public Button placementButton;
    public Button manipulationButton;
    public Button moveObjectButton;
    
    // Private Attributes used for State Manipulation
    private bool isPlacementMode = true;
    private GameObject selectedObject;
    private GameObject objectToBeMoved;
    private int selectedObjectIndex = 1;
    private ARRaycastManager m_RaycastManager;
    private Pose m_PlacementPose;
    private bool m_PlacementPoseIsValid = false;
    public List<GameObject> placedObjects = new List<GameObject>();
    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();
    
    // Unity LifeCycle Methods
    
    // Start is called on the frame when a script is enabled just before any of the Update methods are called the first time.
    void Start()
    {
        bindUIElementsWithEventHandlers();
        setManipulationControlVisibility(false);
    }
    
    // Awake is used to initialize variables or states before the application starts.
    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
    }
    
    // Update is executed for each frame
    void Update()
    {
        if (!isPlacementMode && !objectToBeMoved)
        {
            HandleSelectorVisibility();
            if (!selectedObject)
            {
                SelectObject();
            }
        }
        else
        {
            TryUpdatePlacementPose();
            UpdatePlacementIndicator();
            if (!TryGetTouchPosition(out Vector2 touchPosition))
                return;
            if (m_PlacementPoseIsValid)
            {
                PlaceObject();
            }
        }
    }
    
    // Handle Visibility of UI elements based on current mode(placement or manipulation)
    void setManipulationControlVisibility(bool v)
    {
        scaleSlider.gameObject.SetActive(v);
        rotationSlider.gameObject.SetActive(v);
        nextButton.gameObject.SetActive(v);
        prevButton.gameObject.SetActive(v);
        manipulationButton.gameObject.SetActive(!v);
        placementButton.gameObject.SetActive(v);
        modifierText.gameObject.SetActive(v);
        moveObjectButton.gameObject.SetActive(v);
        placementIndicator.SetActive(!v);
    }

    // bind interactive UI elements to event listeners
    void bindUIElementsWithEventHandlers()
    {
        placementButton.onClick.AddListener(setPlacementMode);
        manipulationButton.onClick.AddListener(setManipulationMode);
        
        prevButton.onClick.AddListener(onPrevClick);
        nextButton.onClick.AddListener(onNextClick);
        
        rotationSlider.onValueChanged.AddListener(setRotationValue);
        scaleSlider.onValueChanged.AddListener(setScaleValue);
        moveObjectButton.onClick.AddListener(onMoveClick);
    }

    // Handler for moving a placed object
    void onMoveClick()
    {
        setManipulationControlVisibility(false);
        
        // Show Label for Moving
        modifierText.text = "Moving Plant - " +selectedObjectIndex;
        modifierText.gameObject.SetActive(true);
        
        // Set Object To Be Moved
        objectToBeMoved = selectedObject;
        placementIndicator.SetActive(true);
    }

    
    // Handler for selecting next object
    void onNextClick()
    {
        if (placedObjects.Count > selectedObjectIndex)
        {
            selectedObjectIndex = selectedObjectIndex + 1;
        }
        SelectObject();
    }
    
    // Handler for selecting previous object
    void onPrevClick()
    {
        if (selectedObjectIndex>1)
        {
            selectedObjectIndex = selectedObjectIndex - 1;
        }
        SelectObject();
    }
    
    // Handler for scale slider
    void setScaleValue(float v)
    {
        if (selectedObject)
        {
            Vector3 scaleChange = new Vector3(v, v, v);

            selectedObject.transform.localScale = scaleChange;
        }
        else
        {
            Debug.Log("No Selected Object To Rotate");
        }
    }
    
    // Handler for rotation slider
    void setRotationValue(float v)
    {
        if (selectedObject)
        {

            selectedObject.transform.localRotation = Quaternion.Euler(0.0f, v, 0.0f);
        }
        else
        {
            Debug.Log("No Selected Object To Rotate");
        }
    }

    // Handler for click event on Manipulation Mode Button
    void setManipulationMode()
    {
        isPlacementMode = false;
        setManipulationControlVisibility(true);
    }
    
    // Handler for click event on Placement Mode Button
    void setPlacementMode()
    {
        isPlacementMode = true;
        setManipulationControlVisibility(false);
    }
    
    // Detect if user clicked on screen
    bool TryGetTouchPosition(out Vector2 touchPosition)
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            touchPosition = Input.GetTouch(0).position;
            return true;
        }
        
        touchPosition = default;
        return false;
    }

    // Select an object to manipulate in manipulation mode
    void SelectObject()
    {
        if (placedObjects.Count > 0)
        {
            selectedObject = GameObject.Find("plant-"+selectedObjectIndex);
            moveObjectButton.GetComponentInChildren<Text>().text = "Move Plant-" + selectedObjectIndex;
            modifierText.text = "Modifying Plant - " +selectedObjectIndex;
        }
    }

    // Hide and Show, previous or next buttons based on object count
    void HandleSelectorVisibility()
    {
        if (selectedObjectIndex == 1 && placedObjects.Count == 1 )
        {
            prevButton.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(false);
        } 
        else if (selectedObjectIndex == 1 && placedObjects.Count > 1)
        {
            prevButton.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(true);   
        }
        else if (selectedObjectIndex == placedObjects.Count && placedObjects.Count > 1)
        {
            prevButton.gameObject.SetActive(true);
            nextButton.gameObject.SetActive(false);   
        }
        else if (selectedObjectIndex > 1 && selectedObjectIndex < placedObjects.Count)
        {
            prevButton.gameObject.SetActive(true);
            nextButton.gameObject.SetActive(true);   
        }
    }
    
    // Place or Move Object in AR world
    private void PlaceObject()
    {
        if (objectToBeMoved)
        {
            objectToBeMoved.transform.position = m_PlacementPose.position;
            setManipulationControlVisibility(true);
            objectToBeMoved = null;
        }
        else
        {
            GameObject objectInstance = Instantiate(objectToPlace, m_PlacementPose.position, m_PlacementPose.rotation) as GameObject;
            placedObjects.Add(objectInstance);
            objectInstance.name = "plant-" + placedObjects.Count;
        
            Debug.Log("Planted "+objectInstance.name);
        }
    }
    
    // Move Placement Indicator on Screen
    private void UpdatePlacementIndicator()
    {
        if (m_PlacementPoseIsValid)
        {
            placementIndicator.SetActive(true);
            placementIndicator.transform.SetPositionAndRotation(m_PlacementPose.position, m_PlacementPose.rotation);
        }
        else
        {
            placementIndicator.SetActive(false);
        }
    }
    
    // Update Position of Placement Indicator
    private void UpdatePlacementPose(Vector3 hitPoint){
        m_PlacementPose.position = hitPoint;
        var cameraForward = Camera.main.transform.forward;
        var cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
        m_PlacementPose.rotation = Quaternion.LookRotation(cameraBearing);
    }
    
    // Try to update placement position if Plane detected
    private void TryUpdatePlacementPose()
    {
        var screenCenter = Camera.main.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        m_RaycastManager.Raycast(screenCenter, s_Hits, TrackableType.PlaneWithinPolygon);
        m_PlacementPoseIsValid = s_Hits.Count > 0;
        if (m_PlacementPoseIsValid)
        {
            UpdatePlacementPose(s_Hits[0].pose.position);
        }
    }

}