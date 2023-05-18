using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CheckPosition : MonoBehaviour
{
    [SerializeField] public GridWithParams grid;

    void Start()
    {
        grid = transform.parent.GetChild(0).GetComponent<GridWithParams>();
        SetGoalPosition();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
        {
            SetGoalPosition();
        }
    }

    private void SetGoalPosition()
    {
        if (grid != null)
        {
            int gridHeight = grid.parameters.height;
            int gridWidth = grid.parameters.width;
            Vector3 gridMargin = grid.parameters.marginBetweenShapes;

            // Get random building in environment
            int randomX = Random.Range(-Mathf.FloorToInt(gridHeight / 2), gridHeight - Mathf.FloorToInt(gridHeight / 2));
            int randomZ = Random.Range(-Mathf.FloorToInt(gridHeight / 2), gridWidth - Mathf.FloorToInt(gridHeight / 2));

            // Position target in centre of a building so that it is always reachable despite roadblocks
            transform.localPosition = new Vector3(randomX * gridMargin.x * 2 - gridMargin.x, transform.localScale.y / 2, randomZ * gridMargin.z * 2 - gridMargin.z);
        }
        
    }
}
