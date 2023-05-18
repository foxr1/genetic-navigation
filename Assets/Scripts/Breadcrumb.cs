using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Breadcrumb : MonoBehaviour
{
    [Header("Time Delay Options")]
    [SerializeField] private bool timeDelay;
    [SerializeField] private float delay;

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
        yield return new WaitForSeconds(delay * Time.timeScale);
        Destroy(gameObject);
    }

    private IEnumerator IsNew()
    {
        yield return new WaitForSeconds(1 * Time.timeScale);
        newBreadcrumb = false;
    }
}
