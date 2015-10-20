using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Сockroach
{
    class RaceTrack
    {
        public int TopCoord;
        public int BottomCoord;
        public int LeftCoord;
        public int RightCoord;

        public RaceTrack(int t, int b, int l, int r)
        {
            this.TopCoord = t;
            this.BottomCoord = b;
            this.LeftCoord = l;
            this.RightCoord = r;
        }

        public bool Contains(Control c)
        {
            return TopCoord < c.Top && c.Bottom < BottomCoord;
        }
    }
}
