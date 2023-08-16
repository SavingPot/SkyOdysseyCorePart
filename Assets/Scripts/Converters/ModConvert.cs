using GameCore;
using SP.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public static class ModConvert
    {
        public static Item DatumItemBaseToDatumItem(ItemData basic)
        {
            if (basic == null)
            {
                Debug.LogWarning($"{MethodGetter.GetLastAndCurrentMethodPath()}: {nameof(basic)} 为 null");
                return null;
            }

            Item datumItem = new()
            {
                //datumTexture = ModFactory.CompareTexture(datumItemBase.textureId),
                count = 1,

                data = basic
            };

            return datumItem;
        }
    }
}
