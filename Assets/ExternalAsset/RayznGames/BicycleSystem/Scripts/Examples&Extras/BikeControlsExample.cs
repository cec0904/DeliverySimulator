using rayzngames;
using UnityEngine;

namespace rayzngames
{        
    public class BikeControlsExample : MonoBehaviour
    {
        BicycleVehicle bicycle;
        public bool controllingBike;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            bicycle = GetComponent<BicycleVehicle>();
        }
        // Update is called once per frame
        void Update()
        {
            bicycle.verticalInput = Input.GetAxis("Vertical");
            bicycle.horizontalInput = Input.GetAxis("Horizontal");
            BrakingInput();

            //Extending functionality 
            bicycle.InControl(controllingBike);

            if (controllingBike)
            {
                // Keep the mounted bike stable even when the ground contact
                // briefly flickers while crossing seams or uneven surfaces.
                bicycle.ConstrainRotation(true);
            }
            else
            {
                bicycle.ConstrainRotation(false);
            }

            /*
            //Detach controls
            if (bicycle.OnGround() == false) { controllingBike = false; }

            //Landing Controls (Land Pressing E)
            if (Input.GetKey(KeyCode.E)) { controllingBike = true; }
            bicycle.InControl(controllingBike);   
            */
        }
        void BrakingInput()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                bicycle.braking = true;
            }
            if (Input.GetKeyUp(KeyCode.Space))
            {
                bicycle.braking = false;
            }

        }
    }
}
