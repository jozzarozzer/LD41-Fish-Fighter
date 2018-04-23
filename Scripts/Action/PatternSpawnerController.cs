using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatternSpawnerController : MonoBehaviour
{
    public IntVariable enemyCount;


    public void SpawnPattern(GameObject chosenPattern)
    {
        StartCoroutine(SpawnPatternIE(chosenPattern));
    }
    public IEnumerator SpawnPatternIE(GameObject pattern)
    {
        GameObject patternInstance = Instantiate(pattern, transform);

        yield return new WaitForSeconds(1);

        if (enemyCount.value == 0)
        {
            Destroy(patternInstance);
        }
    }
}
