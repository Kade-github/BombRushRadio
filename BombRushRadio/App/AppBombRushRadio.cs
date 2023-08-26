using Reptile.Phone;
using UnityEngine;

namespace BombRushRadio;

public class AppBombRushRadio : App
{
    public BombRushRadio_ButtonList list;
    public override void OnAppInit()
    {
        list =  gameObject.AddComponent<BombRushRadio_ButtonList>();
        list.SCROLL_RANGE = 1;
        list.m_AppButtonPrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
        PhoneScrollButton button = list.gameObject.AddComponent<PhoneScrollButton>();
        //list.InitalizeScrollView();
        base.OnAppInit();
    }
    
}