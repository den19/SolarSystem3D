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

                //
                Debug.Log("currentTarget.name is " + currentTarget.name);
                if (currentTarget.name == "Sun")
                {
                    MakeAllDescriptionsInvisible();
                    
                    MakeDescriptionVisible(theSunGameObject);
                }
                if (currentTarget.name == "Earth")
                {
                    MakeAllDescriptionsInvisible();

                    TurnOffMainCamera();

                    TurnOnEarthCamera();

                    MakeDescriptionVisible(theEarthGameObject);
                }
                if (currentTarget.name == "Moon")
                {
                    MakeAllDescriptionsInvisible();
                    MakeDescriptionVisible(theMoonGameObject);

                    TurnOffMainCamera();
                    TurnOnMoonCamera();

                }
                if (currentTarget.name == "Mars")
                {
                    MakeAllDescriptionsInvisible();

                    TurnOffMainCamera();
                    TurnOnMarsCamera();

                    MakeDescriptionVisible(theMarsGameObject);
                }

                if (currentTarget.name == "Mercury")
                {
                    MakeAllDescriptionsInvisible();
                    MakeDescriptionVisible(theMercuryGameObject);

                    TurnOffMainCamera();
                    TurnOnMercuryCamera();
                }

                if (currentTarget.name == "Venus")
                {
                    MakeAllDescriptionsInvisible();
                    MakeDescriptionVisible(theVenusGameObject);

                    TurnOffMainCamera();
                    TurnOnVenusCamera();
                }

                if (currentTarget.name == "Jupiter")
                {
                    MakeAllDescriptionsInvisible();
                    MakeDescriptionVisible(theJupiterGameObject);

                    TurnOffMainCamera();
                    TurnOnJupiterCamera();
                }

                if (currentTarget.name == "Saturn")
                {
                    MakeAllDescriptionsInvisible();
                    MakeDescriptionVisible(theSaturnGameObject);

                    TurnOffMainCamera();
                    TurnOnSaturnCamera();
                }

                if (currentTarget.name == "Uranus")
                {
                    MakeAllDescriptionsInvisible();
                    MakeDescriptionVisible(theUranusGameObject);

                    TurnOffMainCamera();
                    TurnOnUranusCamera();
                }

                if (currentTarget.name == "Neptune")
                {
                    MakeAllDescriptionsInvisible();
                    MakeDescriptionVisible(theNeptuneGameObject);
                    
                    TurnOffMainCamera();
                    TurnOnNeptuneCamera();
                }

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
}
