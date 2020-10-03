using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Model {
    public class Train {
        public Tuple<int, int> lastCoor, coor, nextCoor;
        public bool isPlayer;

        public Train(Tuple<int, int> lastCoor, Tuple<int, int> coor, Tuple<int, int> nextCoor, bool isPlayer = false) {
            this.lastCoor = lastCoor;
            this.coor = coor;
            this.nextCoor = nextCoor;
            this.isPlayer = isPlayer;
        }

        public void Update(Tuple<int, int> nextNextCoor) {
            lastCoor = coor;
            coor = nextCoor;
            nextCoor = nextNextCoor;
        }
    }
}
