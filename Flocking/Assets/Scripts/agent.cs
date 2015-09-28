using UnityEngine;
using System.Collections;

public class agent : MonoBehaviour {

    protected readonly float MAXX = 45.5f;
    protected readonly float MAXY = 19.5f;
    public float maxplayerSpeed = 1.0f;
    public float maxplayerRot = 40.0f;
    public float maxlinaccel = 0.2f;
    public float maxangaccel = 10.0f;
    [ReadOnly]
    public float currentplayerspeed = 0;
    [ReadOnly]
    //+rot = clockwise -rot = counterclockwise
    public float currentplayerrot = 0;
    [ReadOnly]
    public float linaccel = 0;
    [ReadOnly]
    //+ang = clockwise, -ang = counterclockwise
    public float angaccel = 0;

    public float cap(float val, float cap)
    {
        if (val > cap)
        {
            return cap;
        }
        else if (val < -1 * cap)
        {
            return -1 * cap;
        }
        return val;
    }

    #region actual functions to manipulate the agent depending on speed
    public void applyrotation()
    {
        currentplayerrot += angaccel * Time.deltaTime;
        //cap rotation speed
        currentplayerrot = cap(currentplayerrot, maxplayerRot);
        //change angle
        transform.Rotate(0, 0, currentplayerrot * Time.deltaTime * -1);
    }

    public void applylinspeed()
    {
        currentplayerspeed += linaccel * Time.deltaTime;
        //cap line speed
        currentplayerspeed = cap(currentplayerspeed, maxplayerSpeed);
        transform.Translate(Vector3.up * currentplayerspeed * Time.deltaTime);
        //make sure you don't go out of bounds
        if (Mathf.Abs(transform.position.x) > MAXX)
        {
            transform.position = new Vector3(
                transform.position.x > 0 ? MAXX : -1 * MAXX,
                transform.position.y,
                transform.position.z);
        }

        if (Mathf.Abs(transform.position.y) > MAXY)
        {
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y > 0 ? MAXY : -1 * MAXY,
                transform.position.z);
        }
    }
    #endregion
}
