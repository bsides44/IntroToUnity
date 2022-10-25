using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class bounceCounter : MonoBehaviour
{
    public Text score;
    public int counter = 0;
    public GameObject newBall;
    public PhysicMaterial highBounce;
    public AudioSource goodSound;
    public AudioSource badSound;
    private Color warningRed = new Color(1f, 0.1f, 0f, 1f);
    private Color coolBlue = new Color(0f, 0.65f, 1f, 1f);

    IEnumerator flashColor()
    {
        yield return new WaitForSeconds(0.5f);
        score.color = coolBlue;
    }

    void OnCollisionEnter(Collision collisionInfo)
    {
        if (collisionInfo.gameObject.name == "floor" || collisionInfo.gameObject.name == "wall") {
            Instantiate(newBall, new Vector3(0,7,0), transform.rotation);
            newBall.GetComponent<Collider>().material = highBounce;
            counter++;
            score.color = coolBlue;
            score.text = "Score: " + counter;
            goodSound.Play();
        }
        if (collisionInfo.gameObject.name == "enemy" || collisionInfo.gameObject.name == "newBall(Clone)") {
            collisionInfo.gameObject.SetActive(false);
            counter--;
            score.color = warningRed;
            StartCoroutine(flashColor());
            score.text = "Score: " + counter;
            badSound.Play();
        }
    }
}
