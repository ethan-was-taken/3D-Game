using UnityEngine;
using System.Collections;

public class Clone : MonoBehaviour {

    public Transform clone;

	// Use this for initialization
	void OnTriggerEnter (Collider other)
    {
        if (other.gameObject.CompareTag("Cloner"))
        {
            other.gameObject.SetActive(false);
            Instantiate(clone, new Vector3(0, 0, 0), Quaternion.identity);
        }
    }
}
