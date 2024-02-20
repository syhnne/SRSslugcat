using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;



namespace SRSslugcat;

internal class Options : OptionInterface
{
    public Configurable<KeyCode> GravityControlKey;
    public Options() 
    {

        GravityControlKey = config.Bind<KeyCode>("GravityControlKey", KeyCode.G);
    }

    public override void Initialize()
    {
        base.Initialize();
        InGameTranslator inGameTranslator = Custom.rainWorld.inGameTranslator;
        float yspacing = 50f;
        float xposLabel = 20f;
        float xposOpt = 200f;
        float xmax = 600f;
        float ymax = 600f;

        Tabs = new OpTab[]
        {
            new OpTab(this, "Options")
        };

        string desc = "The key to be pressed when controlling gravity";
        Tabs[0].AddItems(
            new OpLabel(xposLabel, ymax - yspacing - 100f, inGameTranslator.Translate("Gravity control key"), false)
            { description = desc },
            new OpKeyBinder(GravityControlKey, new Vector2(xposOpt, ymax - yspacing - 100f), new Vector2(50f, 10f), true, OpKeyBinder.BindController.AnyController)
            { description = desc }
        );


    }

}

