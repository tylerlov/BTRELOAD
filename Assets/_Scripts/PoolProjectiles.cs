using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolProjectiles : MonoBehaviour
{
    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update() { }

    public void recycleAllProjectiles()
    {
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            if (gameObject.transform.GetChild(i).gameObject.activeSelf == true)
            {
                gameObject
                    .transform.GetChild(i)
                    .gameObject.GetComponent<ProjectileStateBased>()
                    .Death();
            }
        }
    }
}
