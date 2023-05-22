using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public bool hasVisited;
    public int timesVisited = 0; // How many recurrent times this cell 

    [SerializeField] private Material visitedMat;
    [SerializeField] private Material unvisitedMat;

    // Update is called once per frame
    void Update()
    {
        if (hasVisited)
        {
            GetComponent<Renderer>().material = visitedMat;
        }
        else
        {
            GetComponent<Renderer>().material = unvisitedMat;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agent"))
        {
            hasVisited = true;
            timesVisited++;
        }
    }
}
