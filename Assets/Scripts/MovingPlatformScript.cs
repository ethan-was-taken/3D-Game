using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Got help from: https://www.youtube.com/watch?v=z20wHJSXk98
public class MovingPlatformScript : RayCastController {

    public LayerMask passengerMask;
    public Vector3 moveSpeed;

    public Transform currPoint;
    public Transform[] points;
    private int pointSelection;

    public bool isVerticalPlat;
    private Vector3 velocity;

    //public GameObject player;

    public override void Start() {
        base.Start();
        pointSelection = 1;
        currPoint = points[pointSelection];
        velocity = moveSpeed * Time.deltaTime;
    }

    void Update() {

        velocity = moveSpeed * Time.deltaTime;

        transform.Translate(velocity);

        if (isAtPoint(velocity)) {
            //Debug.Log("at point");
            if (++pointSelection >= points.Length)
                pointSelection = 0;
            currPoint = points[pointSelection];
            moveSpeed *= -1;
        }
    }
  
    // More code, but easier on the eyes
    private bool isAtPoint(Vector3 velocity) {

        float dirX = Mathf.Sign(velocity.x);
        float dirY = Mathf.Sign(velocity.y);

        Vector3 currPos = transform.position;
        Vector3 pointPos = currPoint.position;

        //if we're moving up
        if (isVerticalPlat) {
            if (dirY >= 0 && currPos.y >= pointPos.y)
                return true;
            //if we're moving down
            else if (dirY < 0 && currPos.y <= pointPos.y)
                return true;
        } else {
            if (dirX >= 0 && currPos.x >= pointPos.x)
                return true;
            else if (dirX < 0 && currPos.x <= pointPos.x)
                return true;
        }

        return false;
    }
   
}
