using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Model {
    public class Train {
        public Tuple<int, int> coor, nextCoor;

        public Train(Tuple<int, int> coor, Tuple<int, int> nextCoor) {
            this.coor = coor;
            this.nextCoor = nextCoor;
        }
    }
}
