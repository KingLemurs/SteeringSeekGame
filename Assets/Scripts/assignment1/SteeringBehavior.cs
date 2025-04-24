using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class SteeringBehavior : MonoBehaviour
{
    public Vector3 target;
    public KinematicBehavior kinematic;
    public List<Vector3> path;
    public float minSpeed = 5;
    // you can use this label to show debug information,
    // like the distance to the (next) target
    public TextMeshProUGUI label;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        kinematic = GetComponent<KinematicBehavior>();
        target = transform.position;
        path = null;
        EventBus.OnSetMap += SetMap;

    }

    // Update is called once per frame
    void Update()
    {
        // Assignment 1: If a single target was set, move to that target
        //                If a path was set, follow that path ("tightly")
        if (target == transform.position && path == null) return;
        
        if (path != null && path.Count > 0)
        {
            target = path[0];
        }
        
        Vector3 dist = target - transform.position;
        // label.text = $"dist: {dist.magnitude}";

        if (dist.magnitude < 1.5f)
        {
            // reached target
            kinematic.SetDesiredSpeed(0);
            kinematic.SetDesiredRotationalVelocity(0);
            print("waht");

            if (path != null && path.Count > 0)
            {
                path.RemoveAt(0);
                print("hi");
            }

            return;
        }

        float targetAngle = Vector3.SignedAngle(transform.forward, dist.normalized, Vector3.up);

        if (Mathf.Abs(targetAngle - transform.eulerAngles.z) < 5f)
        {
            targetAngle *= 0.01f;
        }

        kinematic.SetDesiredRotationalVelocity( targetAngle * Time.deltaTime * 1000);
        kinematic.SetDesiredSpeed(Mathf.Max(dist.sqrMagnitude * Time.deltaTime * 5, minSpeed) );
        // kinematic.SetDesiredSpeed(dist.sqrMagnitude * Time.deltaTime * 20);


        // you can use kinematic.SetDesiredSpeed(...) and kinematic.SetDesiredRotationalVelocity(...)
        //    to "request" acceleration/decceleration to a target speed/rotational velocity
    }

    public void SetTarget(Vector3 target)
    {
        this.target = target;
        EventBus.ShowTarget(target);
    }

    public void SetPath(List<Vector3> path)
    {
        this.path = path;
    }

    public void SetMap(List<Wall> outline)
    {
        this.path = null;
        this.target = transform.position;
    }
}
