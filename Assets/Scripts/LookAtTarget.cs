using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class LookAtTarget : MonoBehaviour {

    [Tooltip("This is the object that the script's game object will look at by default")]
    public GameObject defaultTarget; // the default target that the camera should look at

    [Tooltip("This is the object that the script's game object is currently look at based on the player clicking on a gameObject")]
    public GameObject currentTarget; // the target that the camera should look at

    public GameObject myCanvasGameObject;

    public GameObject theSunGameObject;

    public GameObject theEarthGameObject;

    public GameObject theMoonGameObject;

    public GameObject theMarsGameObject;

    public GameObject theMercuryGameObject;

    public GameObject theVenusGameObject;
    //
    public GameObject theJupiterGameObject;

    public GameObject theSaturnGameObject;

    public GameObject theTitanGameObject;

    public GameObject theUranusGameObject;

    public GameObject theNeptuneGameObject;

    public Camera mainCamera;      // Главная камера (первоначальная)
    public GameObject earthCamera;    // Детальная камера Земли (которая появляется при фокусировке на объект)
    public GameObject marsCamera;    // Детальная камера Марса

    public GameObject neptuneCamera;    // Детальная камера Марса 
    public GameObject uranusCamera;    // Детальная камера Урана
    public GameObject saturnCamera;    // Детальная камера Сатурна
    public GameObject jupiterCamera;    // Детальная камера Юпитера
    public GameObject venusCamera;    // Детальная камера Венеры
    public GameObject mercuryCamera;    // Детальная камера Меркурия
    public GameObject moonCamera;    // Детальная камера Луны
    public GameObject titanCamera;    // Детальная камера Титана



    // Start happens once at the beginning of playing. This is a great place to setup the behavior for this gameObject

    private void Awake()
    {
        Input.multiTouchEnabled = true;
    }

    void Start () {
		if (defaultTarget == null) 
		{
            defaultTarget = this.gameObject;
			Debug.Log ("defaultTarget target not specified. Defaulting to parent GameObject");
		}

        if (currentTarget == null)
        {
            currentTarget = this.gameObject;
            Debug.Log("currentTarget target not specified. Defaulting to parent GameObject");
        }
    }
	
    void MakeAllDescriptionsInvisible()
    {
        theEarthGameObject.SetActive(false);
        theMoonGameObject.SetActive(false);
        theMarsGameObject.SetActive(false);
        theMercuryGameObject.SetActive(false);
        theVenusGameObject.SetActive(false);
        theJupiterGameObject.SetActive(false);
        theSaturnGameObject.SetActive(false);
        if (theTitanGameObject) theTitanGameObject.SetActive(false);
        theUranusGameObject.SetActive(false);
        theNeptuneGameObject.SetActive(false);
        theSunGameObject.SetActive(false);
    }

    void MakeDescriptionVisible(GameObject planet)
    {
        planet.SetActive(true);
    }


    void Update()
    {
        // if primary mouse button is pressed
        if (Input.GetMouseButtonDown(0))
        {
            // determine the ray from the camera to the mousePosition
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // cast a ray to see if it hits any gameObjects
            RaycastHit hit;

            // if there is a hit
            if (Physics.Raycast(ray, out hit))
            {
                currentTarget = hit.collider.gameObject;
                Debug.Log("currentTarget.name is " + currentTarget.name);

                bool useDetailCamera = currentTarget.name != "Sun";
                FocusPlanet(currentTarget.name, useDetailCamera, showDescription: true);

                Debug.Log("defaultTarget changed to "+currentTarget.name);
            }
        } else if (Input.GetMouseButtonDown(1)) // if the second mouse button is pressed
        {
            currentTarget = defaultTarget;
            Debug.Log("defaultTarget changed to " + currentTarget.name);
        }

        // if a currentTarget is set, then look at it
        
        if (currentTarget!=null)
        {
            // transform here refers to the attached gameobject this script is on.
            // the LookAt function makes a transform point it's Z axis towards another point in space
            // In this case it is pointing towards the target.transform
            transform.LookAt(currentTarget.transform);
        } else // reset the look at back to the default
        {
            currentTarget = defaultTarget;
            Debug.Log("defaultTarget changed to " + currentTarget.name);
        }
        
        // Here
    }

    public void FocusPlanet(string planetName, bool useDetailCamera, bool showDescription = true)
    {
        GameObject planetGo = GameObject.Find(planetName);
        if (planetGo == null)
        {
            Debug.LogWarning($"FocusPlanet: '{planetName}' not found in scene.");
            return;
        }

        currentTarget = planetGo;
        MakeAllDescriptionsInvisible();
        TurnOffAllDetailCameras();

        if (planetName == "Sun" || !useDetailCamera)
        {
            TurnOnMainCamera();
        }
        else
        {
            TurnOffMainCamera();
            TurnOnDetailCameraForPlanet(planetName);
        }

        if (showDescription)
        {
            ShowDescriptionForPlanet(planetName);
        }
    }

    public void TurnOffAllDetailCameras()
    {
        if (earthCamera) earthCamera.SetActive(false);
        if (moonCamera) moonCamera.SetActive(false);
        if (marsCamera) marsCamera.SetActive(false);
        if (mercuryCamera) mercuryCamera.SetActive(false);
        if (venusCamera) venusCamera.SetActive(false);
        if (jupiterCamera) jupiterCamera.SetActive(false);
        if (saturnCamera) saturnCamera.SetActive(false);
        if (titanCamera) titanCamera.SetActive(false);
        if (uranusCamera) uranusCamera.SetActive(false);
        if (neptuneCamera) neptuneCamera.SetActive(false);
    }

    private void ShowDescriptionForPlanet(string planetName)
    {
        if (planetName == "Sun" && theSunGameObject) MakeDescriptionVisible(theSunGameObject);
        else if (planetName == "Earth" && theEarthGameObject) MakeDescriptionVisible(theEarthGameObject);
        else if (planetName == "Moon" && theMoonGameObject) MakeDescriptionVisible(theMoonGameObject);
        else if (planetName == "Mars" && theMarsGameObject) MakeDescriptionVisible(theMarsGameObject);
        else if (planetName == "Mercury" && theMercuryGameObject) MakeDescriptionVisible(theMercuryGameObject);
        else if (planetName == "Venus" && theVenusGameObject) MakeDescriptionVisible(theVenusGameObject);
        else if (planetName == "Jupiter" && theJupiterGameObject) MakeDescriptionVisible(theJupiterGameObject);
        else if (planetName == "Saturn" && theSaturnGameObject) MakeDescriptionVisible(theSaturnGameObject);
        else if (planetName == "Titan" && theTitanGameObject) MakeDescriptionVisible(theTitanGameObject);
        else if (planetName == "Uranus" && theUranusGameObject) MakeDescriptionVisible(theUranusGameObject);
        else if (planetName == "Neptune" && theNeptuneGameObject) MakeDescriptionVisible(theNeptuneGameObject);
    }

    private void TurnOnDetailCameraForPlanet(string planetName)
    {
        if (planetName == "Earth") TurnOnEarthCamera();
        else if (planetName == "Moon") TurnOnMoonCamera();
        else if (planetName == "Mars") TurnOnMarsCamera();
        else if (planetName == "Mercury") TurnOnMercuryCamera();
        else if (planetName == "Venus") TurnOnVenusCamera();
        else if (planetName == "Jupiter") TurnOnJupiterCamera();
        else if (planetName == "Saturn") TurnOnSaturnCamera();
        else if (planetName == "Titan") TurnOnTitanCamera();
        else if (planetName == "Uranus") TurnOnUranusCamera();
        else if (planetName == "Neptune") TurnOnNeptuneCamera();
    }

    public GameObject GetActiveDetailCamera()
    {
        if (earthCamera != null && earthCamera.activeSelf) return earthCamera;
        if (moonCamera != null && moonCamera.activeSelf) return moonCamera;
        if (marsCamera != null && marsCamera.activeSelf) return marsCamera;
        if (mercuryCamera != null && mercuryCamera.activeSelf) return mercuryCamera;
        if (venusCamera != null && venusCamera.activeSelf) return venusCamera;
        if (jupiterCamera != null && jupiterCamera.activeSelf) return jupiterCamera;
        if (saturnCamera != null && saturnCamera.activeSelf) return saturnCamera;
        if (titanCamera != null && titanCamera.activeSelf) return titanCamera;
        if (uranusCamera != null && uranusCamera.activeSelf) return uranusCamera;
        if (neptuneCamera != null && neptuneCamera.activeSelf) return neptuneCamera;
        return null;
    }

    public void TurnOffMarsCamera()
    {
        marsCamera.SetActive(false);
    }

    public void TurnOnMarsCamera()
    {
        marsCamera.SetActive(true);
    }

    public void TurnOffMainCamera()
    {
        mainCamera.enabled = false;               
    }

    public void TurnOnMainCamera()
    {
        mainCamera.enabled = true;
    }

    public void TurnOffEarthCamera()
    {
        earthCamera.SetActive(false);
    }

    public void TurnOnEarthCamera()
    {
        earthCamera.SetActive(true);
    }

    public void TurnOnNeptuneCamera()
    {
        neptuneCamera.SetActive(true);
    }
    public void TurnOffNeptuneCamera()
    {
        neptuneCamera.SetActive(false);
    }
    public void TurnOnUranusCamera()
    {
        uranusCamera.SetActive(true);
    }
    public void TurnOffUranusCamera()
    {
        uranusCamera.SetActive(false);
    }
    public void TurnOnSaturnCamera()
    {
        saturnCamera.SetActive(true);
    }
    public void TurnOffSaturnCamera()
    {
        saturnCamera.SetActive(false);
    }
    public void TurnOnJupiterCamera()
    {
        jupiterCamera.SetActive(true);
    }

    public void TurnOffJupiterCamera()
    {
        jupiterCamera.SetActive(false);
    }

    public void TurnOnVenusCamera()
    {
        venusCamera.SetActive(true);
    }

    public void TurnOffVenusCamera()
    {
        venusCamera.SetActive(false);
    }

    public void TurnOnMercuryCamera()
    {
        mercuryCamera.SetActive(true);
    }

    public void TurnOffMercuryCamera()
    {
        mercuryCamera.SetActive(false);
    }

    public void TurnOnMoonCamera()
    {
        moonCamera.SetActive(true);
    }

    public void TurnOffMoonCamera()
    {
        moonCamera.SetActive(false);
    }

    public void TurnOnTitanCamera()
    {
        if (titanCamera) titanCamera.SetActive(true);
    }

    public void TurnOffTitanCamera()
    {
        if (titanCamera) titanCamera.SetActive(false);
    }
}
