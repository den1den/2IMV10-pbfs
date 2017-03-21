using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public interface PMCalculator {
    int Iterations { get; set; }

    void Update( float dt );
    void Release( );
}
