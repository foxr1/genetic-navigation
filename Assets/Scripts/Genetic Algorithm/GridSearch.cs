using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSearch : MonoBehaviour
{
    private Manager manager;
    private CarController carController;
    public bool doGridSearch = false;
    [SerializeField] public int timeInMinutes = 25;
    [SerializeField] private int[][] multipleLayers = new int[][] { new int[3] { 14, 10, 5 }, new int[4] { 14, 10, 10, 5 }, new int[5] { 14, 10, 10, 10, 5 } };
    [SerializeField] private float[] mutationChances = new float[4] { 0.0001f, 0.001f, 0.01f, 0.1f };
    [SerializeField] private float[] mutationStrengths = new float[3] { 0.25f, 0.5f, 0.75f };

    // Start is called before the first frame update
    void Start()
    {
        carController = GetComponent<CarController>();
        manager = GetComponent<Manager>();
        if (doGridSearch)
        {
            StartCoroutine(RunSearch());
        }
    }

    IEnumerator RunSearch()
    {
        foreach (int[] layers in multipleLayers)
        {
            foreach (float chance in mutationChances)
            {
                foreach (float strength in mutationStrengths)
                {
                    manager.layers = layers;
                    manager.MutationChance = chance;
                    manager.MutationStrength = strength;
                    manager.RunAlgorithm();
                    yield return new WaitForSeconds(timeInMinutes * 60 * Time.timeScale);
                }
            }
        }    
    }
}
