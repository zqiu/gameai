using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ReadOnlyAttribute : PropertyAttribute { }

public class agentbehaviour : MonoBehaviour {
    readonly float MAXX = 11.25f;
    readonly float MAXY = 4.75f;
    public float maxplayerSpeed = 1.0f;
    public float maxplayerRot = 40.0f;
    public float maxlinaccel = 0.2f;
    public float maxangaccel = 10.0f;
    Vector3 targetpos;
    Quaternion targetrot;
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
    public GameObject target = null;
    GameObject targetinstance = null;
    public GameObject speedupanim = null;
    GameObject speedupinstance = null;
    public enum Algorithm { arrive, evade, wander, pathfollow, followmouse };
    public Algorithm algotype;
    SpriteRenderer myrenderer = null;
    
    //for evading only
    public GameObject evading = null;
    //for pathfinding only
    List<Vector2> path = null;
    //for hunterwanderer only
    float timer = 20.0f;

	// Use this for initialization
	void Start () {
        transform.rotation = Quaternion.Euler(0, 0, 
            algotype == Algorithm.pathfollow ? 90 : Random.value * 360);
        transform.localScale = new Vector3(2, 2, 1);
        targetrot = Quaternion.Euler(0, 0, 0);
        targetpos = new Vector3(0, 0, 0);
        myrenderer = gameObject.GetComponent<SpriteRenderer>();
        setcoloronalgo();
        if (algotype == Algorithm.pathfollow)
        {
            initializepath();
        }
	}
	
	// Update is called once per frame
	void Update () {
        settarget();
        setangaccel();
        setlinaccel();
        applyrotation();
        applylinspeed();
	}

    #region ai to set lin and ang acceleration
    //make sure targetrot is set already
    void setangaccel()
    {
        float angle = Quaternion.Angle(transform.rotation, targetrot) * (rotationdirection() ? 1:-1);
        //Debug.Log(angle);
        if ((Mathf.Abs(angle) < (currentplayerrot * currentplayerrot / (2 * maxangaccel)))
            || Mathf.Abs(angle) < 1)
        {
            //decelerate...so pretty much slow down speed
            angaccel = maxangaccel * (currentplayerrot > 0 ? -1 : 1);
        }else{
            //accelerate...accelerate in the direction that's smallest distance
            angaccel = maxangaccel * (angle > 0 ? 1 : -1);
        }
    }

    //make sure targetpos is set already
    void setlinaccel()
    {
        float dx = transform.position.x - targetpos.x;
        float dy = transform.position.y - targetpos.y;
        float dist = getdistto(dx, dy);
        //Debug.Log(dist);
        if (dist < (currentplayerspeed * currentplayerspeed/ (2 * maxlinaccel))
            || dist < 0.1){
            //decelerate
            linaccel = -1 * maxlinaccel;
        }
        else
        {
            //accelerate
            linaccel = maxlinaccel;
        }
    }
    #endregion

    #region functions to set the target destination
    void settarget()
    {
        float angle;
        Ray2D temp;
        switch (algotype)
        {
            case Algorithm.followmouse:
                if (Input.GetMouseButtonDown(0))
                {
                    Vector3 mousepos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    targetpos = mousepos;
                    maketarget();
                }
                break;
            case Algorithm.wander:
                Vector3 circle = Random.insideUnitCircle * 0.5f;
                angle = (transform.rotation.eulerAngles.z - 90 + 5) * Mathf.Deg2Rad;
                temp = new Ray2D(transform.position, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
                circle += (Vector3)temp.GetPoint(-2.0f);
                targetpos = circle;
                maketarget();
                break;
            case Algorithm.arrive:
                targetpos = new Vector3(3.5f, 4f, 0f);
                maketarget();
                break;
            case Algorithm.evade:
                if (evading != null)
                {
                    float dy = evading.transform.position.y - transform.position.y;
                    float dx = evading.transform.position.x - transform.position.x;
                    angle = Mathf.Atan2(dy, dx);
                    float dist = 1 / getdistto(dx, dy);
                    temp = new Ray2D(transform.position, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
                    targetpos = (Vector3)temp.GetPoint(-1 * dist);
                    maketarget();
                }
                break;
            case Algorithm.pathfollow:
                float mindist = float.MaxValue;
                int pos = 0;
                for (int i = 0; i < path.Count; ++i)
                {
                    float distp = getdistto(path[i].x - transform.position.x, path[i].y - transform.position.y);
                    if (distp < mindist)
                    {
                        pos = i;
                        mindist = distp;
                    }
                }
                targetpos = path[pos < 3 ? 0 : pos - 3];
                maketarget();
                break;
        }
        updaterotationfromposition();
        if (loadnextlevel())
        {
            doloadnextlevel();    
        }
    }

    //angle between target and current heading
    void updaterotationfromposition()
    {
        float angle = Mathf.Atan2(targetpos.y - transform.position.y, targetpos.x - transform.position.x) * Mathf.Rad2Deg;
        angle = (angle + 270);
        targetrot = Quaternion.Euler(0, 0, angle);
    }
    #endregion

    #region actual functions to manipulate the agent depending on speed
    void applyrotation()
    {
        currentplayerrot += angaccel * Time.deltaTime;
        //cap rotation speed
        if (currentplayerrot > maxplayerRot)
        {
            currentplayerrot = maxplayerRot;
        }
        else if (currentplayerrot < -1 * maxplayerRot)
        {
            currentplayerrot = -1 * maxplayerRot;
        }
        //change angle
        transform.Rotate(0, 0, currentplayerrot * Time.deltaTime * -1);
    }

    void applylinspeed()
    {
        currentplayerspeed += linaccel * Time.deltaTime;
        //cap line speed
        if (currentplayerspeed > maxplayerSpeed)
        {
            currentplayerspeed = maxplayerSpeed;
        }
        else if (currentplayerspeed < 0)
        {
            currentplayerspeed = 0;
        }
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
        makespeed(currentplayerspeed > 0);
    }
    #endregion

    #region Utilityfunctions
    bool rotationdirection()
    {
        //true for clockwise, false for counter
        float diff = targetrot.eulerAngles.z - transform.rotation.eulerAngles.z;
        diff = (diff > 0) ? diff : diff + 360;
        return diff > 180;
    }

    void setcoloronalgo()
    {
        switch (algotype)
        {
            case Algorithm.arrive:
                myrenderer.color = Color.green;
                break;
            case Algorithm.evade:
                myrenderer.color = Color.magenta;
                break;
            case Algorithm.wander:
                myrenderer.color = Color.yellow;
                break;
            case Algorithm.pathfollow:
                myrenderer.color = Color.red;
                break;
            case Algorithm.followmouse:
                myrenderer.color = Color.blue;
                break;
        }
    }

    void maketarget()
    {
        if (targetinstance != null)
        {
            Destroy(targetinstance);
        }
        if (target != null)
        {
            targetinstance = (GameObject)Instantiate(target, new Vector3(targetpos.x, targetpos.y, 0), Quaternion.identity);
            targetinstance.GetComponent<SpriteRenderer>().color = myrenderer.color;
        }
    }

    void makespeed(bool speedup)
    {
        if (speedupinstance != null && !speedup)
        {
            Destroy(speedupinstance);
        }
        if (speedupanim != null && speedup)
        {
            if (speedupinstance == null)
            {
                speedupinstance = (GameObject)Instantiate(speedupanim, new Vector3(transform.position.x, transform.position.y, 0), Quaternion.identity);
            }
            float angle = (transform.rotation.eulerAngles.z - 90) * Mathf.Deg2Rad;
            Ray2D temp = new Ray2D(transform.position, new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)));
            //Debug.Log(temp.ToString() + ":" + angle.ToString());
            speedupinstance.transform.position = temp.GetPoint(10 * currentplayerspeed * Time.deltaTime);
            speedupinstance.transform.rotation = transform.rotation;
            speedupinstance.GetComponent<SpriteRenderer>().color = myrenderer.color;
        }
    }

    void initializepath()
    {
        path = new List<Vector2>();
        float x = 3.5f;
        float y = 4.0f;
        path.Add(new Vector2(x, y));
        for (int i = 0; i < 10; ++i)
        {
            y -= 0.2f;
            path.Add(new Vector2(x, y));
        }
        for (int i = 0; i < 14; ++i)
        {
            x -= 0.5f;
            path.Add(new Vector2(x, y));
        }
        for (int i = 0; i < 10; ++i)
        {
            y -= 0.2f;
            path.Add(new Vector2(x, y));
        }
        for (int i = 0; i < 7; ++i)
        {
            x += 0.5f;
            path.Add(new Vector2(x, y));
        }
        if (target != null)
        {
            foreach (Vector2 iter in path)
            {
                Instantiate(target, new Vector3(iter.x, iter.y, 0), Quaternion.identity);
            }
        }
    }

    float getdistto(float dx, float dy)
    {
        return Mathf.Sqrt(dy * dy + dx * dx);
    }

    bool loadnextlevel()
    {
        //Debug.Log(Application.loadedLevelName);
        if (Application.loadedLevelName == "redfollow" || Application.loadedLevelName == "wolfapproach")
        {
            return getdistto(transform.position.x - 3.5f, transform.position.y - 4f) < 0.1;
        }
        if (Application.loadedLevelName == "hunterwander")
        {
            timer -= Time.deltaTime;
            return timer < 0;
        }
        return false;
    }

    void doloadnextlevel()
    {
        Application.LoadLevel(Application.loadedLevel + 1);
    }
    #endregion
}
