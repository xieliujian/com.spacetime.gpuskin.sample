using ST.GPUSkin;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinTest : MonoBehaviour
{
    /// <summary>
    /// .
    /// </summary>
    public GameObject gpuSkinGo1;

    /// <summary>
    /// .
    /// </summary>
    public GameObject gpuSkinGo2;

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        InitGpuSkinList(gpuSkinGo1);
        InitGpuSkinList(gpuSkinGo2);
    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        GPUSkinMgr.S.OnUpdate();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="go"></param>
    void InitGpuSkinList(GameObject go)
    {
        if (go == null)
            return;

        for (int i = 0; i < 100; i++)
        {
            var instGo = GameObject.Instantiate(go);
            if (instGo == null)
                continue;

            instGo.name = go.name + i;
            instGo.transform.SetParent(transform);

            float eulerY = Random.Range(0, 360f);
            instGo.transform.localEulerAngles = new Vector3(0f, eulerY, 0f);

            float scale = Random.Range(0.2f, 0.5f);
            instGo.transform.localScale = new Vector3(scale, scale, scale);

            float x = Random.Range(-5f, 5f);
            float z = Random.Range(-5f, 5f);
            instGo.transform.localPosition = new Vector3(x, 0f, z);

            //var script = instGo.GetComponentInChildren<GPUSkinPlayerBase>();
            //if (script != null)
            //{
            //    script.RandomPlay();
            //}
        }
    }
}
