using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class bounceCounter : MonoBehaviour
{
    public Text score;
    public int counter = 0;
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
            counter++;
            score.color = coolBlue;
            score.text = "Score: " + counter;
        }
        if (collisionInfo.gameObject.name == "enemy") {
            counter--;
            score.color = warningRed;
            StartCoroutine(flashColor());
            score.text = "Score: " + counter;
        }
    }
}
