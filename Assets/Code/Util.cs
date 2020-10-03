using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Code {
    public class Util {
        public static float EaseCurvedTrack(float one, float two, float t) {
            if (one == two) {
                return one;
            }
            float linear = Mathf.Lerp(one, two, t);
            bool oneIsInt = Mathf.Floor(one) == one;
            bool twoIsInt = Mathf.Floor(two) == two;
            if (oneIsInt && twoIsInt) {
                return linear;
            }
            float eased;
            if (Mathf.Abs(one - two) < .5f) {
                eased = oneIsInt ? EasingFunction.EaseInQuad(one, two, t) : EasingFunction.EaseOutQuad(one, two, t);
            } else {
                eased = oneIsInt ? EasingFunction.EaseOutQuad(one, two, t) : EasingFunction.EaseInQuad(one, two, t);
            }
            return Mathf.Lerp(linear, eased, .4f);
        }
    }
}
