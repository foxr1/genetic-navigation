using MathNet.Numerics.Random;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NeuralNetwork))]
public class CarController : MonoBehaviour
{
    [Header("Agent Controls")]
    [SerializeField, Range(0, 100)] public int agentNo = 0;
    [SerializeField] private GameObject breadcrumbPrefab;
    [SerializeField] private bool leaveBreadcrumbs = true;
    [SerializeField] public bool randomiseGoalPosition = true;
    [SerializeField] public bool isRandomGrid = true;

    private Vector3 startPosition, startRotation;
    public NeuralNetwork network = null;

    public float timeSinceStart = 0f; 

    [Header("Fitness")]
    public float overallFitness;

    [Header("Network Options")]
    [SerializeField] public static int LAYERS = 11;

    [Header("Environment")]
    private GameObject target;
    private Rigidbody rbd;
    private GridWithParams grid;

    private Vector3 lastPosition;
    private float totalDistanceTravelled = 0;
    private float avgSpeed;
    private float numOfCollisions = 0;

    private float[] sensors = new float[LAYERS];

    private Manager geneticManager;
    private bool reachedGoal = false;
    private bool collided = false;

    private void Awake() 
    {
        rbd = GetComponent<Rigidbody>();
        startPosition = transform.position;
        startRotation = transform.eulerAngles;
        lastPosition = startPosition;
        totalDistanceTravelled = 0f;
        numOfCollisions = 0f;
        reachedGoal = false;
        collided = false;

        foreach (Transform tr in transform.parent)
        {
            if (tr.tag == "Goal")
            {
                target = tr.gameObject;
            }
            if (tr.tag == "Grid")
            {
                grid = tr.GetComponent<GridWithParams>();
            }
            if (tr.tag == "Manager")
            {
                geneticManager = tr.GetComponent<Manager>();
            }
        }
        //network = GetComponent<NeuralNetwork>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
        {
            reachedGoal = true;
            //RandomiseTargetPos();
            geneticManager.numberOfGoals += 1;
            Stop();

        }
        if (other.CompareTag("Wall"))
        {
            numOfCollisions += 1;
            Stop();
        }
        //if (other.CompareTag("Breadcrumb"))
        //{
        //    // If breadcrumb has been active for more than 1 second, then penalise for colliding with it
        //    if (other.GetComponent<Breadcrumb>().newBreadcrumb == false)
        //    {
        //        numOfCollisions += 1;
        //        geneticManager.numberOfCollisions++;
        //    }
        //}
    }

    private void Stop()
    {
        rbd.velocity = Vector3.zero;
        collided = true;
        UpdateFitness();
    }

    public void RandomiseTargetPos()
    {
        int gridHeight = grid.parameters.height;
        int gridWidth = grid.parameters.width;
        Vector3 gridMargin = grid.parameters.marginBetweenShapes;

        // Get random building in environment
        int randomX = Random.Range(-Mathf.FloorToInt(gridHeight / 2) + 1, gridHeight - Mathf.FloorToInt(gridHeight / 2));
        int randomZ = Random.Range(-Mathf.FloorToInt(gridHeight / 2), gridWidth - Mathf.FloorToInt(gridHeight / 2));

        // Position target in centre of a building so that it is always reachable despite roadblocks
        target.transform.localPosition = new Vector3(randomX * gridMargin.x * 2 - gridMargin.x, target.transform.localScale.y / 2, randomZ * gridMargin.z * 2 - gridMargin.z);
    }

    public void BuildRandomGrid(int randomSeedNo)
    {
        foreach (Transform tr in transform.parent) 
        {
            if (tr.tag == "Grid")
            {
                grid = tr.GetComponent<GridWithParams>();
                
                grid.parameters.randomSeed = randomSeedNo;

                grid.BuildGrid();
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!collided)
        {
            if (other.CompareTag("Breadcrumb"))
            {
                // If breadcrumb has been active for more than 1 second, then penalise for colliding with it
                if (other.GetComponent<Breadcrumb>().newBreadcrumb == false)
                {
                    numOfCollisions += 1;
                    geneticManager.numberOfCollisions++;
                }
            }
        }
    }

    private void FixedUpdate() 
    {
        if (!collided && network != null)
        {
            InputSensors();
            lastPosition = transform.position;

            float[] ouput = network.FeedForward(sensors);
            MoveCar(ouput); 

            UpdateFitness();

            if (leaveBreadcrumbs)
            {
                if (placingBreadcrumb == false)
                {
                    StartCoroutine(PlaceBreadcrumb());
                }
            }
        }
    }

    private bool placingBreadcrumb = false;
    IEnumerator PlaceBreadcrumb()
    {
        placingBreadcrumb = true;
        GameObject breadcrumb = Instantiate(breadcrumbPrefab);
        breadcrumb.transform.parent = transform.parent;
        breadcrumb.name = $"Breadcrumb{agentNo}";
        breadcrumb.transform.localPosition = transform.localPosition;
        yield return new WaitForSeconds(0.5f);
        placingBreadcrumb = false;
    }

    public void RemoveAllBreadcrumbs()
    {
        foreach (Transform child in transform.parent)
        {
            if (child.tag == "Breadcrumb" && child.name == $"Breadcrumb{agentNo}")
            {
                Destroy(child.gameObject); 
            }
        }
    }

    private void InputSensors() 
    {
        // Same raycasts as RL (4 each direction up to 75 deg)
        //List<Vector3> raycasts = new List<Vector3>()
        //{
        //    (Quaternion.Euler(0, -75f, 0) * transform.forward),
        //    (Quaternion.Euler(0, -56.25f, 0) * transform.forward),
        //    (Quaternion.Euler(0, -37.5f, 0) * transform.forward),
        //    (Quaternion.Euler(0, -18.75f, 0) * transform.forward),
        //    (transform.forward),
        //    (Quaternion.Euler(0, 75f, 0) * transform.forward),
        //    (Quaternion.Euler(0, 56.25f, 0) * transform.forward),
        //    (Quaternion.Euler(0, 37.5f, 0) * transform.forward),
        //    (Quaternion.Euler(0, 18.75f, 0) * transform.forward)
        //};

        // NSWE directions
        List<Vector3> raycasts = new List<Vector3>()
        {
            (Quaternion.Euler(0, -90f, 0) * transform.forward),
            (transform.forward),
            (Quaternion.Euler(0, 90f, 0) * transform.forward),
            (Quaternion.Euler(0, -180f, 0) * transform.forward),
        };

        // draw raycasts and add hit to sensors
        for (int i = 0; i < raycasts.Count; i++) {
            Ray r = new Ray(transform.position, raycasts[i]);
            RaycastHit hit;
            if (Physics.Raycast(r, out hit, 100, LayerMask.GetMask("Wall", "Goal")))
            {
                sensors[i] = (50 - hit.distance) / 50;
                Debug.DrawLine(r.origin, hit.point, Color.red);
            } 
            else
            {
                sensors[i] = 0;
            }
        }

        // velocity
        sensors[raycasts.Count] = rbd.velocity.x;
        sensors[raycasts.Count + 1] = rbd.velocity.z;

        // distance to target
        sensors[raycasts.Count + 2] = Vector3.Distance(transform.localPosition, target.transform.localPosition);

        // direction to target
        sensors[raycasts.Count + 3] = (target.transform.position - transform.position).normalized.x;
        sensors[raycasts.Count + 4] = (target.transform.position - transform.position).normalized.y;
        sensors[raycasts.Count + 5] = (target.transform.position - transform.position).normalized.z;

        //sensors[raycasts.Count + 6] = transform.position.x;
        //sensors[raycasts.Count + 7] = transform.position.y;
        //sensors[raycasts.Count + 8] = transform.position.z;

        sensors[raycasts.Count + 6] = numOfCollisions;

        // forward transform
        //sensors[raycasts.Count + 7] = transform.forward.x;
        //sensors[raycasts.Count + 8] = transform.forward.y;
        //sensors[raycasts.Count + 9] = transform.forward.z;

        //sensors[raycasts.Count + 10] = transform.right.x;
        //sensors[raycasts.Count + 11] = transform.right.y;
        //sensors[raycasts.Count + 12] = transform.right.z;
    }

    public void MoveCar(float[] act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        switch (act.ToList().IndexOf(act.Max()))
        {
            case 1:
                dirToGo = transform.forward * 1f;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                break;
            //case 3:
            //    rotateDir = transform.up * 1f;
            //    break;
            //case 4:
            //    rotateDir = transform.up * -1f;
            //    break;
            case 3:
                dirToGo = transform.right * 1f;
                break;
            case 4:
                dirToGo = transform.right * -1f;
                break;
        }
        transform.Rotate(rotateDir, Time.deltaTime * 200f);
        rbd.AddForce(dirToGo * 2f, ForceMode.VelocityChange);
    }

    public void UpdateFitness()
    {
        totalDistanceTravelled += Vector3.Distance(transform.position, lastPosition);
        float distanceToTarget = Vector3.Distance(transform.localPosition, target.transform.localPosition);
        if (reachedGoal)
        {
            network.fitness = (float)(20 + (100 / Math.Pow(totalDistanceTravelled, 2)));
        } 
        else
        {
            //network.fitness = (float)(10 / Math.Pow(distanceToTarget, 2)) - numOfCollisions;
            network.fitness = (float)(10 / Math.Pow(distanceToTarget + geneticManager.numberOfCollisions, 2));

            //float collisionPenalty = 0.1f;
            //if (collided)
            //{
            //    collisionPenalty = 5f;
            //}

            //float noDistancePenalty = 0.1f;
            //if (totalDistanceTravelled < 5)
            //{
            //    noDistancePenalty = 10f;
            //}

            //network.fitness = (float)(10 / (Math.Pow(distanceToTarget, 2) + collisionPenalty + noDistancePenalty));
        }
        //Debug.Log(network.fitness);
    }
}
