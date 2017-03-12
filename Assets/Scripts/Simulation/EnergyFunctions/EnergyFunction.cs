﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Simulation.EnergyFunctions
{
    public interface EnergyFunction
    {
        void solve(ref Vector3[] positions);
    }
}
