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
                //Debug.Log("Picking up " + item.def.id + " of kind " + item.def.kind);
                if (item.def.kind == ItemDef.Kind.Weapon)
                {
                    //Debug.Log("Hey we are picking up a weapon! and we also should have: " + LazySingletonBehavior<DataManager>.Instance.Items.Get(item.def.id + "_Comp").id);
                    Debug.Log("Hey we are picking up a weapon!");

                    if (LazySingletonBehavior<DataManager>.Instance.Items.TryGet(item.def.id + "_Comp", out ItemDef componentDef))
                    {
                        Item componentItem = new Item(componentDef);
                        instance.AddItem(componentItem, quiet, network);
                        Debug.Log("You got a single " + componentItem.def.id + " which means it exists.");
                        
                        /**********************************************/
                        /*  This is the business of adding a recipe   */
                        /**********************************************/
                        // Get actor...
                        //Actor characterActor = ThirdPersonCameraControl.Instance.CharacterActor;

                        // Make general use recipeDef...
                        ItemDef recipeDef = null;

                        // Loop through all IDs in Items.Keys... 
                        Debug.Log("Fetching Recipes...");
                        foreach (string id in LazySingletonBehavior<DataManager>.Instance.Items.Keys)
                        {
                            // Get ItemDef from string id...
                            recipeDef = LazySingletonBehavior<DataManager>.Instance.Items.Get(id);
                            // if there are crafting components listed and the number is greater than 0...
                            if (recipeDef.craftingComponents != null && recipeDef.craftingComponents.Length > 0)
                            {
                                // if recipe contains component version of picked up item...
                                foreach (ItemDef.CraftingComponent component in recipeDef.craftingComponents)
                                {
                                    if (component.itemDefId.Contains(item.def.id))
                                    {
                                        // Add recipe and boast.
                                        Debug.Log("Relevant recipe found, adding: " + recipeDef.id);
                                        //Inventory.Get(characterActor.gameObject).AddRecipe(recipeDef, false);
                                        instance.AddRecipe(recipeDef, false);
                                        break;
                                    }
                                }
                            }
                            recipeDef = null;
                        }
                    }
                    else
                    {
                        Debug.Log("TryGet Didn't work, returned id: " + componentDef + ".");
                    }
                }
                return orig(instance, item, quiet, network);
            };

            //We need to modify drop so that when you drop an equipment you also drop the ingredient/component
            On.Necro.Inventory.Drop += (orig, instance, def) =>
            {
                Debug.Log("Dropping " + def.id + " of kind " + def.kind);
                if (def.kind == ItemDef.Kind.Weapon)
                {
                    Debug.Log("Hey we are dropping a weapon, lets see if it'll be removed");
                    //Removes ingredient/component with same name if it exists
                    if (instance.HaveIngredient(def.id + "_Comp", 1))
                    {
                        instance.RemoveIngredient(def.id + "_Comp", 1);

                        /***********************************************/
                        /*  This is the business of dropping a recipe  */
                        /***********************************************/

                        Debug.Log(instance.KnownRecipes.ToString());

                        // Loop through all IDs in instance.KnownRecipes... 
                        Debug.Log("(Drop)Checking Recipes...");
                        foreach (ItemDef recipeDef in instance.KnownRecipes)
                        {
                            // Get ItemDef from string id...
                            // if there are crafting components listed and the number is greater than 0...
                            if (recipeDef.craftingComponents != null && recipeDef.craftingComponents.Length > 0)
                            {
                                // if recipe contains component version of picked up item...
                                foreach (ItemDef.CraftingComponent component in recipeDef.craftingComponents)
                                {
                                    if (component.itemDefId.Contains(def.id + "_Comp"))
                                    {
                                        // Add recipe and boast.
                                        Debug.Log("(Drop)Relevant recipe found, dropping: " + recipeDef.id);
                                        Debug.Log("(Drop)Relevant recipe found, dropping: " + instance.gameObject);
                                        //instance.KnownRecipes.Remove(recipeDef);
                                        if (instance.KnownRecipes.Remove(recipeDef))
                                        {
                                            Debug.Log("KnownRecipes.Remove() worked.");
                                            break;
                                        }
                                        else
                                        {
                                            Debug.Log("Failed to remove recipe. The problem is KnownRecipes.");
                                            //Make entirely new dictionary containing the relevant recipes, 
                                            // Then remove from knownrecipes where it intersects the new dictionary.
                                            //instance.KnownRecipes.IntersectWith();
                                        }
                                    }
                                }
                            }
                        }
                        foreach (ItemDef recipe in instance.KnownRecipes)
                        {
                            Debug.Log("Recipe: " + recipe.id);
                            Debug.Log("Recipe: " + recipe.id);
                        }
                        foreach (ItemDef recipe in instance.KnownRecipes)
                        {
                            Debug.Log("Recipe: " + recipe.id);
                            Debug.Log("Recipe: " + recipe.id);
                        }
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

                            /*  
                             *  If crafted item is a weapon, remove the recipe.
                            */

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
