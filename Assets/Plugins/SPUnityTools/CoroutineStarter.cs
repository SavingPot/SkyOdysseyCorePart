using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SP.Tools.Unity
{
    public class CoroutineStarter : SingletonClass<CoroutineStarter>
    {
        public static Coroutine Do(IEnumerator enumerator) => instance.StartCoroutine(enumerator);
    }
}
