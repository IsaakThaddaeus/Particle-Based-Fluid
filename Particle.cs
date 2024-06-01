using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle : MonoBehaviour
{
    public Texture2D texture;

    float maxSpeed = 15;
    float start = 100;
    public void setPosition(Vector2 position)
    {
        transform.position = new Vector3(position.x, position.y, 0);
    }

    public void setColor(Vector2 velocity)
    {

        float x = start + velocity.magnitude * ((512-start) / maxSpeed);
        Color c = texture.GetPixel((int)x, texture.height / 2);
        gameObject.GetComponent<SpriteRenderer>().color = c;
    }
}
