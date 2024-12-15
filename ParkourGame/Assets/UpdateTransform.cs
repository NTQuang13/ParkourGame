using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateTransform : MonoBehaviour
{
    [SerializeField] GameObject network;
    // Start is called before the first frame update
    void Start()
    {
        //network.GetComponent<NetworkCommunicator>().OnUpdateTransform += UpdateTransform_OnUpdateTransform;
    }

    private void UpdateTransform_OnUpdateTransform(object sender, System.EventArgs e)
    {
        throw new System.NotImplementedException();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
