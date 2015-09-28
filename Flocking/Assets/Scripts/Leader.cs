using UnityEngine;
using System.Collections;

public class ReadOnlyAttribute : PropertyAttribute { }

public class Leader : agent
{
    readonly float BOUNDBOX = 6.0f;
    readonly float SEARCHDIST = 6.0f;
    readonly float SEPERATIONMINDIST = 1.0f;
    Vector3 targetpos;
    Quaternion targetrot;
    public GameObject target = null;
    GameObject targetinstance = null;
    SpriteRenderer myrenderer = null;
    public GameObject prefab = null;
    private GameObject[] agents = null;
    private agent[] agentscripts = null;
    public int flocksize = 10;
    public float sval = 0.6f, aval = 0.1f, cval = 0.3f;

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

    bool rotationdirection()
    {
        //true for clockwise, false for counter
        float diff = targetrot.eulerAngles.z - transform.rotation.eulerAngles.z;
        diff = (diff > 0) ? diff : diff + 360;
        return diff > 180;
    }

    float getdistto(float dx, float dy)
    {
        return Mathf.Sqrt(dy * dy + dx * dx);
    }

    // Use this for initialization
    void Start()
    {
        targetrot = Quaternion.Euler(0, 0, 0);
        targetpos = new Vector3(0, 0, 0);
        myrenderer = gameObject.GetComponent<SpriteRenderer>();
        //init other objects
        if (prefab != null)
        {
            agents = new GameObject[flocksize + 1];
            agentscripts = new agent[flocksize + 1];
            for (int i = 0; i < flocksize; ++i)
            {
                Vector3 position = new Vector3(
                    (Random.value - 0.5f) * BOUNDBOX + transform.position.x,
                    (Random.value - 0.5f) * BOUNDBOX + transform.position.y,
                    (Random.value - 0.5f) * BOUNDBOX + transform.position.z);
                GameObject boid = (GameObject)Instantiate(prefab, position, transform.rotation);
                agent boidscript = boid.GetComponent<agent>();
                agents[i] = boid;
                agentscripts[i] = boidscript;
            }
            agents[flocksize] = this.gameObject;
            agentscripts[flocksize] = this;
        }
    }

    // Update is called once per frame
    void Update()
    {
        settarget();
        angaccel = getangaccel(transform.rotation, targetrot, currentplayerrot);
        linaccel = getlinaccel(transform.position, targetpos, currentplayerspeed);
        applyrotation();
        applylinspeed();
        dootheragentbehaviour();
    }

    #region functions to set the target destination
    void settarget()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousepos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            targetpos = mousepos;
            maketarget();
        }
        targetrot = updaterotationfromposition(transform.position, targetpos);
    }

    //angle between target and current heading
    Quaternion updaterotationfromposition(Vector3 from, Vector3 to)
    {
        float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
        angle = (angle + 270);
        return Quaternion.Euler(0, 0, angle);
    }
    #endregion

    #region ai to set lin and ang acceleration
    float getangaccel(Quaternion from, Quaternion to, float currentrot)
    {
        float angle = Quaternion.Angle(from, to) * (rotationdirection() ? 1 : -1);
        //Debug.Log(angle);
        if ((Mathf.Abs(angle) < (currentrot * currentrot / (2 * maxangaccel)))
            || Mathf.Abs(angle) < 1)
        {
            //decelerate...so pretty much slow down speed
            return maxangaccel * (currentrot > 0 ? -1 : 1);
        }
        else
        {
            //accelerate...accelerate in the direction that's smallest distance
            return maxangaccel * (angle > 0 ? 1 : -1);
        }
    }
    
    float getlinaccel(Vector3 from, Vector3 to, float currentspeed)
    {
        float dx = from.x - to.x;
        float dy = from.y - to.y;
        float dist = getdistto(dx, dy);
        //Debug.Log(dist);
        if (dist < (currentspeed * currentspeed / (2 * maxlinaccel))
            || dist < 0.1){
            //decelerate
            return -1 * maxlinaccel;
        }
        else
        {
            //accelerate
            return maxlinaccel;
        }
    }
    #endregion

    #region behaviour specific to other agents
    void dootheragentbehaviour()
    {
        float s_lin = 0f, s_ang = 0f, a_lin = 0f, a_ang = 0f, c_lin = 0f, c_ang = 0f;
        for (int i = 0; i < agents.Length - 1; ++i) //minus one cause we don't wanna update the leader's position here
        {
            getadverageseperation(i, ref s_lin, ref s_ang);
            getadveragealign(i, ref a_lin, ref a_ang);
            getadveragecohesion(i, ref c_lin, ref c_ang);
            float totlin = sval * s_lin + aval * a_lin + cval * c_lin;
            float totang = sval * s_ang + aval * a_ang + cval * c_ang;
            //Debug.Log(i.ToString() + " " + totlin.ToString() + " " + totang.ToString() + " " + c_lin);
            agentscripts[i].linaccel = cap(totlin, maxlinaccel);
            agentscripts[i].angaccel = cap(totang, maxangaccel);
        }
        for (int i = 0; i < agents.Length - 1; ++i) //update the positions of the agents
        {
            agentscripts[i].applylinspeed();
            agentscripts[i].applyrotation();
        }
    }

    void getadverageseperation(int index, ref float targetlinaccel, ref float targetangaccel)
    {
        targetlinaccel = 0;
        targetangaccel = 0;
        if (agents == null) { return; }
        float agentx = agents[index].transform.position.x;
        float agenty = agents[index].transform.position.y;
        int numagents = 0;
        float totalangaccell = 0f;
        float totallinaccell = 0f;
        for (int i = 0; i < agents.Length; ++i)
        {
            float targetx = agents[i].transform.position.x;
            float targety = agents[i].transform.position.y;
            float dist = getdistto(targetx - agentx, targety - agenty);
            if (i == index || dist > SEARCHDIST)
            {
                continue;
            }
            numagents++;
            Vector3 _targetpos = new Vector3(agentx, agenty, 0);
            _targetpos.x -= Mathf.Max(SEPERATIONMINDIST / (targetx - agentx), SEARCHDIST);
            _targetpos.y -= Mathf.Max(SEPERATIONMINDIST / (targety - agenty), SEARCHDIST);
            Quaternion _targetrot = updaterotationfromposition(agents[index].transform.position, _targetpos);
            totalangaccell += getangaccel(agents[index].transform.rotation, _targetrot, agentscripts[index].currentplayerrot);
            totallinaccell += getlinaccel(agents[index].transform.position, _targetpos, agentscripts[index].currentplayerspeed);
        }
        if (numagents > 0)
        {
            targetangaccel = totalangaccell / numagents;
            targetlinaccel = totallinaccell / numagents;
        }
    }

    void getadveragealign(int index, ref float targetlinaccel, ref float targetangaccel)
    {
        targetlinaccel = 0;
        targetangaccel = 0;
        if (agents == null) { return; }
        float agentx = agents[index].transform.position.x;
        float agenty = agents[index].transform.position.y;
        int numagents = 0;
        float totspeed = 0f;
        Quaternion totalangaccell = Quaternion.Euler(0, 0, 0);
        for (int i = 0; i < agents.Length; ++i)
        {
            float targetx = agents[i].transform.position.x;
            float targety = agents[i].transform.position.y;
            float dist = getdistto(targetx - agentx, targety - agenty);
            if (i == index || dist > SEARCHDIST)
            {
                continue;
            }
            numagents++;
            totspeed += agentscripts[i].currentplayerspeed;
            totalangaccell.z += agents[i].transform.rotation.z;
        }
        //get average angle
        if (numagents > 0)
        {
            totalangaccell.z /= numagents;
            targetangaccel = getangaccel(agents[index].transform.rotation, totalangaccell, agentscripts[index].currentplayerrot);
            targetlinaccel = (totspeed / numagents - agentscripts[index].currentplayerspeed) * maxlinaccel / maxplayerSpeed;
        }
    }

    void getadveragecohesion(int index, ref float targetlinaccel, ref float targetangaccel)
    {
        targetlinaccel = 0;
        targetangaccel = 0;
        if (agents == null) { return; }
        float agentx = agents[index].transform.position.x;
        float agenty = agents[index].transform.position.y;
        int numagents = 0;
        Vector3 sumdistance = new Vector3(0, 0, 0);
        for (int i = 0; i < agents.Length; ++i)
        {
            float targetx = agents[i].transform.position.x;
            float targety = agents[i].transform.position.y;
            float dist = getdistto(targetx - agentx, targety - agenty);
            //Debug.Log(i.ToString() + " " + targetx.ToString() + " " + targety.ToString());
            if (i == index || dist > SEARCHDIST)
            {
                continue;
            }
            numagents++;
            sumdistance.x += agents[i].transform.position.x;
            sumdistance.y += agents[i].transform.position.y;
        }
        //get avg location
        if (numagents > 0)
        {
            sumdistance.x /= numagents;
            //Debug.Log(sumdistance);
            sumdistance.y /= numagents;
            Quaternion _targetrot = updaterotationfromposition(agents[index].transform.position, sumdistance);
            targetangaccel = getangaccel(agents[index].transform.rotation, _targetrot, agentscripts[index].currentplayerrot);
            targetlinaccel = getlinaccel(agents[index].transform.position, sumdistance, agentscripts[index].currentplayerspeed);
        }
    }
    #endregion
}
