using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Diamond : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<AudioSource>().Play();
            GameManager_stickhero.instance.UpdateDiamonds();
            Destroy(gameObject);
        }
    }
}
