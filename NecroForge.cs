using Necro;
using Partiality.Modloader;
using HBS;
using System.Collections.Generic;
using HBS.Text;
using HBS.Collections;
using HBS.Logging;
using HBS.DebugConsole;
using HBS.Pooling;
using MonoMod.ModInterop;
using UnityEngine;
using Necro.UI;

namespace NecroForge
{
    class NecroForge : PartialityMod
    {
        public override void Init()
        {
            typeof(Utils).ModInterop();


            //We need to modify AddItem so that when you pick up equipment, you also get an ingredient/component with it
            On.Necro.Inventory.AddItem += (orig, instance, item, quiet, network) =>
            {
                Debug.Log("Picking up " + item.def.id + " of kind " + item.def.kind);
                if (item.def.kind == ItemDef.Kind.Weapon)
                {
                    Debug.Log("Hey we are picking up a weapon!");

                    //Now add that ingredient to your inventory!
                    //instance.AddIngredient(newIng.def.id, 1);
                    //Utils.NetworkAddIngredient(newIng.def.id, 1);

                    //We make a new item definition for our equipment ingredient/component
                    ItemDef blankItem = new ItemDef();

                    //We give it the same name as the equipment
                    blankItem.id = item.def.id + "_Comp";

                    blankItem.group = "Component";
                    blankItem.tier = 0;
                    blankItem.obscurity = 0;
                    blankItem.prefabName = "pf-crafting_component-octahedron";
                    blankItem.kind = ItemDef.Kind.Ingredient;
                    blankItem.boneRef = "SourceRoot";
                    blankItem.maxCount = 100;

                    Item newIng = new Item(blankItem);
                    newIng.intValue = 1;
                    //newIng.def.id = blankItem.id + "_Comp";
                    newIng.def.id = blankItem.id;


                    instance.AddItem(newIng);

                    Debug.Log("Do we now have that ingredient? Lets find out- " + instance.HaveIngredient(newIng.def.id, 1));


                }
                return orig(instance, item, quiet, network);
            };

            //We need to modify drop so that when you drop an equipment you also drop the ingredient/component
            On.Necro.Inventory.Drop += (orig, instance, def) =>
            {
                Debug.Log("Dropping " + def.id + " of kind " + def.kind);
                Debug.Log("Item Kind - " + def.kind);
                if (def.kind == ItemDef.Kind.Weapon)
                {
                    Debug.Log("Hey we are dropping a weapon, lets see if it'll be removed");
                    //Removes ingredient/component with same name if it exists
                    if (instance.HaveIngredient(def.id + "_Comp", 1))
                    {
                        instance.RemoveIngredient(def.id + "_Comp", 1);
                    }
                }
                return orig(instance, def);
            };

            //We need to modify craft so that when you craft something new with your ingredient/component, the associated equipment will also vanish
            On.Necro.Inventory.Craft += (orig, instance, itemDef) =>
            {
                bool returnVal;

                //Need to make a list of the ids of the components that make up the craftable item
                ItemDef[] compIds = new ItemDef[itemDef.craftingComponents.Length];
                for (int i = 0; i < itemDef.craftingComponents.Length; i++)
                {
                    ItemDef temp = new ItemDef();
                    temp.id = itemDef.craftingComponents[i].itemDefId;
                    compIds[i] = temp;
                }
                //Now we run the original function, and if its successful, we know now to check for equipment
                returnVal = orig(instance, itemDef);

                if (returnVal == true)
                {
                    ItemDef temp = new ItemDef();
                    for (int x = 0; x < compIds.Length; x++)
                    {
                        int index = 0;
                        //Give this temp ItemDef the name of the ingredient/component
                        for (int i = 0; i < compIds[x].id.Length; i++)
                        {
                            if (compIds[x].id[i] == '_')
                                break;
                            index++;
                        }

                        temp.id = compIds[x].id.Substring(0, index);


                        if (!string.IsNullOrEmpty(temp.id) && compIds[x].id.Length != index)
                        {
                            //Now, search the Item CSV for an item with the same name as the ingredient
                            temp = LazySingletonBehavior<DataManager>.Instance.Items.Get(temp.id);



                            Debug.Log("Checking if " + temp.id + " is equipped");
                            //This creates an equippable that will then be used as a reference to delete the equipment from the player's inventory
                            Equippable equipBoi = instance.GetEquippedFromDef(temp);
                            ThirdPersonCameraControl._instance.Inventory.removeEquipped(equipBoi);


                            Debug.Log("Yay we removed it!");
                        }
                    }
                }

                return returnVal;
            };
            base.Init();
        }

    }
}
