using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Breadcrumb : MonoBehaviour
{
    [Header("Time Delay Options")]
    [SerializeField] private bool timeDelay;
    [SerializeField, Tooltip("How long a delay before removing the breadcrumb")] private float removeDelay = 10f;
    [SerializeField, Tooltip("How long a delay there should be before the agent receives a penalty for colliding")]
    private float penaltyDelay = 0.5f;

    public bool newBreadcrumb = true;

    // Start is called before the first frame update
    void Start()
    {
        if (timeDelay)
        {
            StartCoroutine(RemoveBreadcrumb());
        }

        StartCoroutine(IsNew());
    }

    private IEnumerator RemoveBreadcrumb()
    {
        yield return new WaitForSeconds(removeDelay);
        Destroy(gameObject);
    }

    private IEnumerator IsNew()
    {
        yield return new WaitForSeconds(penaltyDelay);
        newBreadcrumb = false;
    }
}
