using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using System.Collections;

public class Manager : MonoBehaviour 
{

    public float timeframe;
    public int populationSize;//creates population size
    public GameObject prefab;//holds bot prefab
    public float spawnRate = 0.5f;

    public int[] layers = new int[3] { 5, 3, 2 };//initializing network to the right size

    [Range(0.0001f, 1f)] public float MutationChance = 0.01f;

    [Range(0f, 1f)] public float MutationStrength = 0.5f;

    [Range(0.1f, 10f)] public float Gamespeed = 1f;

    //public List<Bot> Bots;
    public List<NeuralNetwork> networks;
    private List<CarController> cars;

    void Start()// Start is called before the first frame update
    {
        if (populationSize % 2 != 0)
            populationSize = 50;//if population size is not even, sets it to fifty

        InitNetworks();
        InvokeRepeating("CreateBots", 0.1f, timeframe);//repeating function
    }

    public void InitNetworks()
    {
        networks = new List<NeuralNetwork>();
        for (int i = 0; i < populationSize; i++)
        {
            NeuralNetwork net = new NeuralNetwork(layers);
            //net.Load("Assets/Pre-trained.txt");//on start load the network save
            networks.Add(net);
        }
    }

    public void CreateBots()
    {
        Time.timeScale = Gamespeed;//sets gamespeed, which will increase to speed up training
        if (cars != null && cars.Count == populationSize)
        {
            if (cars[0].randomiseGoalPosition)
            {
                cars[0].RandomiseTargetPos();
            }
            
            for (int i = 0; i < cars.Count; i++)
            {
                GameObject.Destroy(cars[i].gameObject);//if there are Prefabs in the scene this will get rid of them
            }

            SortNetworks();//this sorts networks and mutates them
        }

        //cars = new List<CarController>();
        //for (int i = 0; i < populationSize; i++)
        //{
        //    CarController car = Instantiate(prefab, new Vector3(-20f, 2.5f, 0), new Quaternion(0, 0, 0, 0), transform.parent).GetComponent<CarController>();//create botes
        //    car.network = networks[i];//deploys network to each learner
        //    cars.Add(car);
        //}
        StartCoroutine(DelayedCreation(spawnRate));
    }

    private IEnumerator DelayedCreation(float delay)
    {
        cars = new List<CarController>();
        for (int i = 0; i < populationSize; i++)
        {
            CarController car = Instantiate(prefab, new Vector3(-20f, 2.5f, 0), new Quaternion(0, 0.7f, 0, 0.7f), transform.parent).GetComponent<CarController>();//create botes
            car.network = networks[i];//deploys network to each learner
            cars.Add(car);
            yield return new WaitForSeconds(delay);
        }
    }

    public void SortNetworks()
    {
        //for (int i = 0; i < populationSize; i++)
        //{
        //    cars[i].UpdateFitness();//gets bots to set their corrosponding networks fitness
        //}
        networks.Sort();
        networks[populationSize - 1].Save("Assets/Save.txt");//saves networks weights and biases to file, to preserve network performance
        for (int i = 0; i < populationSize / 2; i++)
        {
            networks[i] = networks[i + populationSize / 2].copy(new NeuralNetwork(layers));
            networks[i].Mutate((int)(1 / MutationChance), MutationStrength);
        }
    }
}