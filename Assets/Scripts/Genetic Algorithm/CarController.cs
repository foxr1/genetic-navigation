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
    [SerializeField] private bool isRandomGrid = true;

    private Vector3 startPosition, startRotation;
    public NeuralNetwork network = null;

    [Range(-1f,1f)]
    public float a,t;

    public float timeSinceStart = 0f; 

    [Header("Fitness")]
    public float overallFitness;
    public float distanceMultipler = -0.15f;
    public float targetMultiplier = 1f;
    public float collisionMultiplier = -0.1f;
    public float sensorMultiplier = 0.1f;

    [Header("Network Options")]
    public static int LAYERS = 18;
    public int NEURONS = 10;

    [Header("Environment")]
    private GameObject target;
    [SerializeField] private GameObject agentGhost;
    private Rigidbody rbd;
    private GridWithParams grid;

    private Vector3 lastPosition;
    private float totalDistanceTravelled;
    private float avgSpeed;
    private float numOfCollisions = 0;
    private float breadcrumbCollisions = 0;

    //private List<float> sensors = new List<float>() { 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0 };
    private float[] sensors = new float[18];

    private Manager geneticManager;
    [SerializeField] private SetText setText;

    private int randomSeedNo = 0;
    private bool reachedGoal = false;
    private bool collided = false;

    private void Awake() 
    {
        rbd = GetComponent<Rigidbody>();
        startPosition = transform.position;
        startRotation = transform.eulerAngles;
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
    public void ResetWithNetwork(NeuralNetwork net)
    {
        network = net;
        Reset();
    }
    public void Reset() 
    {
        timeSinceStart = 0f;
        totalDistanceTravelled = 0f;
        avgSpeed = 0f;
        lastPosition = startPosition;
        overallFitness = 0f;
        numOfCollisions = 0f;
        breadcrumbCollisions = 0f;
        transform.position = startPosition;
        transform.eulerAngles = startRotation;

        if (isRandomGrid)
        {
            BuildRandomGrid();
        }
        if (randomiseGoalPosition)
        {
            RandomiseTargetPos();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
        {
            reachedGoal = true;
            collided = true;
            Death(true);
            RandomiseTargetPos();
            rbd.velocity = Vector3.zero;
        }
        else if (other.CompareTag("Wall"))
        {
            collided = true;
            //Instantiate(agentGhost, transform.position, transform.rotation);
            numOfCollisions += 1;
            //Death(false);
            rbd.velocity = Vector3.zero; 
        }
    }

    public void RandomiseTargetPos()
    {
        int gridHeight = grid.parameters.height;
        int gridWidth = grid.parameters.width;
        Vector3 gridMargin = grid.parameters.marginBetweenShapes;

        // Get random building in environment
        int randomX = Random.Range(-Mathf.FloorToInt(gridHeight / 2), gridHeight - Mathf.FloorToInt(gridHeight / 2));
        int randomZ = Random.Range(-Mathf.FloorToInt(gridHeight / 2), gridWidth - Mathf.FloorToInt(gridHeight / 2));

        // Position target in centre of a building so that it is always reachable despite roadblocks
        target.transform.localPosition = new Vector3(randomX * gridMargin.x * 2 - gridMargin.x, target.transform.localScale.y / 2, randomZ * gridMargin.z * 2 - gridMargin.z);
    }

    public void BuildRandomGrid()
    {
        if (randomSeedNo > 99)
        {
            randomSeedNo = 0;
        }
        else
        {
            randomSeedNo++;
        }
        grid.parameters.randomSeed = randomSeedNo;

        grid.BuildGrid();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Breadcrumb"))
        {
            // If breadcrumb has been active for more than 1 second, then penalise for colliding with it
            if (other.GetComponent<Breadcrumb>().newBreadcrumb == false)
            {
                breadcrumbCollisions++;
            }
        }
        //if (other.CompareTag("Charging"))
        //{
        //    if (batteryLevel < 20)
        //    {
        //        batteryLevel = 100;
        //        SetReward(0.5f);
        //        StartCoroutine(ChangeFloorMaterial(winMaterial));
        //    }
        //}
    }
    private void FixedUpdate() 
    {
        if (!collided && network != null)
        {
            InputSensors();
            lastPosition = transform.position;

            //(a, t) = network.RunNetwork(sensors);

            float[] ouput = network.FeedForward(sensors);
            MoveCar(ouput);

            timeSinceStart += Time.deltaTime;

            //CalculateFitness();
            UpdateFitness();
            //setText.SetInfoText(
            //    geneticManager.currentGeneration.ToString(),
            //    geneticManager.currentGenome.ToString(),
            //    overallFitness.ToString(),
            //    geneticManager.timeSinceStart);

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
        yield return new WaitForSeconds(0.5f * Time.timeScale);
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
    private void Death(bool reachedTarget)
    {
        RemoveAllBreadcrumbs();
        if (reachedTarget)
        {
            float fitness = (float)(20 + 100 / totalDistanceTravelled);
            //GameObject.FindObjectOfType<Manager>().Death(fitness, network);
        }
        else
        {
            //GameObject.FindObjectOfType<Manager>().Death(overallFitness, network);
        }
        
    }
    private void CalculateFitness() 
    {
        // add boolean for whether has reached target or + max double (inf)
        totalDistanceTravelled += Vector3.Distance(transform.position, lastPosition);
        avgSpeed = totalDistanceTravelled / timeSinceStart;
        float distanceToTarget = Vector3.Distance(transform.localPosition, target.transform.localPosition);

        //overallFitness = (totalDistanceTravelled * distanceMultipler) + (avgSpeed * avgSpeedMultiplier) + (((aSensor + bSensor + cSensor) / 3) * sensorMultiplier);
         
        float distanceToTargetNormalised = (float)((distanceToTarget - 0.1) / (100 - 0.1) * 100);
        distanceToTargetNormalised = (float)(Math.Pow(distanceToTargetNormalised / 400, -1) * 75);

        //overallFitness = (float)(1 / Math.Abs(
        //    (totalDistanceTravelled * distanceMultipler) +
        //    (distanceToTarget * targetMultiplier) +
        //    //((1 / (1 + numOfCollisions + breadcrumbCollisions)) * collisionMultiplier) +
        //    (sensors.Average() * sensorMultiplier)
        //    ));

        //overallFitness = (float)(
        //    (totalDistanceTravelled * distanceMultipler) +
        //    (distanceToTargetNormalised * targetMultiplier) +
        //    ((1 / (1 + numOfCollisions + breadcrumbCollisions)) * collisionMultiplier) +
        //    (sensors.Average() * sensorMultiplier)
        //    ); 

        //overallFitness = (float)(
        //    (1 / (1 + distanceToTarget)) * targetMultiplier +
        //    (1 / (1 + totalDistanceTravelled)) * distanceMultipler +
        //    (1 / (1 + numOfCollisions + breadcrumbCollisions)) * collisionMultiplier +
        //    (sensors.Average() * sensorMultiplier));

        overallFitness = (float)(10 / Math.Pow(1 / (1 + distanceToTarget), 2));

        //if (timeSinceStart > 20 && overallFitness < 90) {
        //    Death(false);
        //}

        if (timeSinceStart > 2000)
        {
            Death(false); 
        }
        
        //if (overallFitness >= 1000) {
        //    Death();
        //}
    }
    private void InputSensors() 
    {
        // same raycasts as RL (4 each direction up to 75 deg)
        List<Vector3> raycasts = new List<Vector3>() 
        {
            (Quaternion.Euler(0, -75f, 0) * transform.forward),
            (Quaternion.Euler(0, -56.25f, 0) * transform.forward),
            (Quaternion.Euler(0, -37.5f, 0) * transform.forward),
            (Quaternion.Euler(0, -18.75f, 0) * transform.forward),
            (transform.forward),
            (Quaternion.Euler(0, 75f, 0) * transform.forward),
            (Quaternion.Euler(0, 56.25f, 0) * transform.forward),
            (Quaternion.Euler(0, 37.5f, 0) * transform.forward),
            (Quaternion.Euler(0, 18.75f, 0) * transform.forward)
        };

        // draw raycasts and add hit to sensors
        for (int i = 0; i < 9; i++) {
            Ray r = new Ray(transform.position, raycasts[i]);
            RaycastHit hit;
            if (Physics.Raycast(r, out hit, 50 , LayerMask.GetMask("Wall", "Goal")))
            {
                sensors[i] = hit.distance;
                Debug.DrawLine(r.origin, hit.point, Color.red);
            }
        }

        // velocity
        sensors[9] = rbd.velocity.x;
        sensors[10] = rbd.velocity.z;

        // distance to target
        sensors[11] = Vector3.Distance(transform.localPosition, target.transform.localPosition); 

        // direction to target
        sensors[12] = (target.transform.position - transform.position).normalized.x; 
        sensors[13] = (target.transform.position - transform.position).normalized.y;
        sensors[14] = (target.transform.position - transform.position).normalized.z;

        // forward transform
        sensors[15] = transform.forward.x;
        sensors[16] = transform.forward.y;
        sensors[17] = transform.forward.z;
    }

    public void MoveCar(float[] act) 
    {
        //var dirToGo = transform.forward * act[0];
        //var rotateDir = transform.up * act[1];

        //transform.Rotate(rotateDir, Time.deltaTime * 200f);
        //rbd.AddForce(dirToGo * 2f, ForceMode.VelocityChange);

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
            case 3:
                rotateDir = transform.up * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
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
            network.fitness = (float)(20 + 100 / Math.Pow(totalDistanceTravelled, 2));
        } else
        {
            network.fitness = (float)(100 / (1 + Math.Pow(distanceToTarget, 2))) - numOfCollisions;
        }
        Debug.Log(network.fitness);
    }
}
