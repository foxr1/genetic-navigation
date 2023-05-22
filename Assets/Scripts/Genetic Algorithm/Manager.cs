using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using System.Collections;
using System.Drawing;
using System.Linq;

public class Manager : MonoBehaviour 
{
    private GridSearch gridSearch;
    [SerializeField] private SetText setText;

    [Header("Environment Settings")]
    [SerializeField][Range(0.1f, 20f)] public float GameSpeed = 1f;
    [SerializeField] public GameObject prefab;// Holds agent prefab
    private int randomSeedNo = 0; // Seed for grid
    [SerializeField] private int randomiseGridNumber = 10; // Randomise grid every x times
    private int randomGridCounter = 0;
    public int numberOfGoals = 0; // Number of goals reached in one generation
    public int numberOfCollisions = 0; // Number of collisions in one generation
    public int numberOfCellsVisited = 0; // Number of cells visited
    [SerializeField] public int populationSize;//creates population size
    [SerializeField] public float timeframe; // How long each generation lasts in seconds
    [SerializeField] public float spawnRate = 0.5f; // How quickly agents spawn on new generation
    [SerializeField] private GameObject cells;

    [Header("Network Settings")]
    [SerializeField] public int[] layers = new int[3] { 5, 3, 2 };//initializing network to the right size
    [SerializeField][Range(0.0001f, 1f)] public float MutationChance = 0.01f;
    [SerializeField][Range(0f, 1f)] public float MutationStrength = 0.5f;
    [SerializeField][Range(0f, 1f)] private float PercentageOfMutatedAgents = 0.5f;
    [SerializeField] private bool UseRandomAgents = false;
    [SerializeField][Range(0f, 1f)] private float PercentageOfRandomAgents = 0.5f;
    [SerializeField] private bool hyperMutation = false;
    [SerializeField] private int CheckMutation = 10;
    [SerializeField] private int CheckMutationCounter = 0;
    [SerializeField] private bool eliteBased = false;

    private int currentGeneration = 0;
    private float previousAverageFitness = 0f;
    private float previousMutationChance = 0f;
    private float previousMutationStrength = 0f;
    private List<float> lastMaxFitness = new List<float>();
    public float timeSinceStart = 0f;

    public List<NeuralNetwork> networks;
    private List<CarController> cars;

    void Start()// Start is called before the first frame update
    {
        gridSearch = GetComponent<GridSearch>();
        if (!gridSearch.doGridSearch)
        {
            RunAlgorithm();
        }
    }

    private void FixedUpdate()
    {
        timeSinceStart += Time.deltaTime / Time.timeScale;

        setText.SetInfoText(
            currentGeneration.ToString(),
            (lastMaxFitness.Count > 0) ? lastMaxFitness[lastMaxFitness.Count - 1].ToString() : "0",
            timeSinceStart);
    }

    public void RunAlgorithm()
    {
        if (populationSize % 2 != 0)
            populationSize = 50;//if population size is not even, sets it to fifty

        InitNetworks();
        InvokeRepeating("CreateAgents", 0.1f, timeframe);//repeating function
    }

    public void InitNetworks()
    {
        numberOfGoals = 0;
        networks = new List<NeuralNetwork>();
        for (int i = 0; i < populationSize; i++)
        {
            layers[0] = CarController.LAYERS;
            NeuralNetwork net = new NeuralNetwork(layers);
            //net.Load("Assets/Save/Save-MC-0.01MS0.5-Layers4.txt");//on start load the network save
            networks.Add(net);
        }
    }

    public void CreateAgents()
    {
        Time.timeScale = GameSpeed;//sets gamespeed, which will increase to speed up training
        currentGeneration++;
        if (cars != null)
        {
            cars[0].RemoveAllBreadcrumbs();
            NumberOfCellsVisited(randomGridCounter >= randomiseGridNumber);

            if (cars[0].isRandomGrid && randomGridCounter >= randomiseGridNumber)
            {
                if (cars[0].randomiseGoalPosition)
                {
                    cars[0].RandomiseTargetPos();
                }
                if (randomSeedNo > 99)
                {
                    randomSeedNo = 0;
                }
                else
                {
                    randomSeedNo++;
                }
                cars[0].BuildRandomGrid(randomSeedNo);
                randomGridCounter = 0;
            }

            for (int i = 0; i < cars.Count; i++)
            {
                GameObject.Destroy(cars[i].gameObject);//if there are Prefabs in the scene this will get rid of them
            }

            SortNetworks();//this sorts networks and mutates them
        }

        randomGridCounter++;
        StartCoroutine(DelayedCreation(spawnRate / Time.timeScale)); // Spawning lots at one location led to agents being shoved off the map
    }

    private IEnumerator DelayedCreation(float delay)
    {
        cars = new List<CarController>();
        for (int i = 0; i < populationSize; i++)
        {
            CarController car = Instantiate(prefab, new Vector3(-20f, 2.5f, 0), Quaternion.Euler(0, 90f, 0), transform.parent).GetComponent<CarController>();
            car.network = networks[i]; // Deploys network to each learner
            cars.Add(car);
            yield return new WaitForSeconds(delay);
        }
    }

    public void NumberOfCellsVisited(bool reset)
    {
        foreach (Cell cell in cells.GetComponentsInChildren<Cell>())
        {
            if (cell.hasVisited) 
            {
                numberOfCellsVisited++;
                if (reset)
                {
                    cell.hasVisited = false;
                    cell.timesVisited = 0;
                }
            }
        }
    }

    public void SortNetworks() 
    {   
        networks.Sort();

        if (lastMaxFitness.Count >= CheckMutation)
        {
            lastMaxFitness.RemoveAt(0);
        }
        lastMaxFitness.Add(networks[populationSize - 1].fitness); // Get best fitness from generation
        
        CheckMutationCounter++;
        if (CheckMutationCounter >= CheckMutation && hyperMutation)
        {
            float avgFitness = lastMaxFitness.Average();
            if (previousAverageFitness != 0f)
            {
                if (avgFitness < previousAverageFitness)
                {
                    // Mutation Chance
                    if (previousMutationChance < MutationChance)
                    {
                        previousMutationChance = MutationChance;
                        if (MutationChance <= 0.2f)
                        {
                            MutationChance -= 0.0025f;
                        } 
                        else
                        {
                            MutationChance -= 0.05f;
                        }
                    }
                    else if (previousMutationChance > MutationChance)
                    {
                        previousMutationChance = MutationChance;
                        if (MutationChance >= 0.8f)
                        {
                            MutationChance += 0.0025f;
                        }
                        else
                        {
                            MutationChance += 0.05f;
                        }
                    }

                    // Mutation Strength
                    if (previousMutationStrength < MutationStrength)
                    {
                        previousMutationStrength = MutationStrength;
                        if (MutationStrength <= 0.2f)
                        {
                            MutationStrength -= 0.005f;
                        }
                        else
                        {
                            MutationStrength -= 0.05f;
                        }
                    }
                    else if (previousMutationStrength > MutationStrength)
                    {
                        previousMutationStrength = MutationStrength;
                        if (MutationStrength >= 0.8f)
                        {
                            MutationStrength += 0.005f;
                        }
                        else
                        {
                            MutationStrength += 0.05f;
                        }
                    }
                }
                else if (avgFitness > previousAverageFitness)
                {
                    // Mutation Chance
                    if (previousMutationChance < MutationChance)
                    {
                        previousMutationChance = MutationChance;
                        if (MutationChance >= 0.8f)
                        {
                            MutationChance += 0.0025f;
                        }
                        else
                        {
                            MutationChance += 0.05f;
                        }
                    }
                    else if (previousMutationChance > MutationChance)
                    {
                        previousMutationChance = MutationChance;
                        if (MutationChance <= 0.2f)
                        {
                            MutationChance -= 0.0025f;
                        }
                        else
                        {
                            MutationChance -= 0.05f;
                        }
                    }

                    // Mutation Strength
                    if (previousMutationStrength < MutationStrength)
                    {
                        previousMutationStrength = MutationStrength;
                        if (MutationStrength >= 0.8f)
                        {
                            MutationStrength += 0.005f;
                        }
                        else
                        {
                            MutationStrength += 0.05f;
                        }
                    }
                    else if (previousMutationStrength > MutationStrength)
                    {
                        previousMutationStrength = MutationStrength;
                        if (MutationStrength <= 0.2f)
                        {
                            MutationStrength -= 0.005f;
                        }
                        else
                        {
                            MutationStrength -= 0.05f;
                        }
                    }
                }
            }
            else
            {
                previousMutationChance = MutationChance;
                previousMutationStrength = MutationStrength;
                MutationChance += 0.05f;
                MutationStrength += 0.1f;
            }
            previousAverageFitness = avgFitness;
            CheckMutationCounter = 0;
        }

        networks[populationSize - 1].Save($"Assets/Save/Save-MC-{MutationChance}MS{MutationStrength}-Layers{layers.Length}.txt", numberOfGoals); // Saves networks weights and biases to file, to preserve network performance

        int size = Mathf.RoundToInt(populationSize * PercentageOfMutatedAgents);

        if (eliteBased)
        {
            for (int i = 0; i < size; i++)
            {
                networks[i] = networks[populationSize - 1];
                networks[i].Mutate((int)(1 / MutationChance), MutationStrength);
            }
        } 
        else
        {
            for (int i = 0; i < size; i++)
            {
                networks[i] = networks[i + size].copy(new NeuralNetwork(layers));
                networks[i].Mutate((int)(1 / MutationChance), MutationStrength);
            }
        }

        // Replace random individuals with new random network
        if (UseRandomAgents)
        {
            int numOfRandomAgents = Mathf.RoundToInt(populationSize * PercentageOfRandomAgents);
            for (int i = 0; i < numOfRandomAgents; i++)
            {
                int randomValue = Random.Range(0, populationSize - 2); // exclude best agent from random
                networks[randomValue] = new NeuralNetwork(layers);
            }
        }

        numberOfCollisions = 0;
    }
}