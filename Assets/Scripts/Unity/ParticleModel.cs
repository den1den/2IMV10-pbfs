using System;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// This class only binds to a Unity GameObject.
/// </summary>
public class ParticleModel : MonoBehaviour
{
    ParticleModelCalculator pmc;

    // Use this for initialization
    void Start()
    {
        pmc = new ParticleModelCalculator(this);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
