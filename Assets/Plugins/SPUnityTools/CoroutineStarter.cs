using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SP.Tools.Unity
{
    public class CoroutineStarter : SingletonClass<CoroutineStarter>
    {
        public static void Do(IEnumerator enumerator) => instance.StartCoroutine(enumerator);
    }
}
