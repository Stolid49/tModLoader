using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;

namespace Terraria.ModLoader {
public abstract class Mod
{
    internal string file;
    internal Assembly code;
    private string name;
    public string Name
    {
        get
        {
            return name;
        }
    }
    private ModProperties properties;
    public ModProperties Properties
    {
        get
        {
            return properties;
        }
    }
    internal readonly List<ModRecipe> recipes = new List<ModRecipe>();
    internal readonly IDictionary<string, ModItem> items = new Dictionary<string, ModItem>();
    internal GlobalItem globalItem;

    /*
     * Initializes the mod's information, such as its name.
     */
    internal void Init()
    {
        ModProperties properties = new ModProperties();
        properties.Autoload = false;
        SetModInfo(out name, ref properties);
        this.properties = properties;
    }

    public abstract void SetModInfo(out string name, ref ModProperties properties);

    public abstract void Load();

    public virtual void AddRecipes() {}

    internal void Autoload()
    {
        Type[] classes = code.GetTypes();
        foreach(Type type in classes)
        {
            if(type.IsSubclassOf(typeof(ModItem)))
            {
                ModItem item = (ModItem)Activator.CreateInstance(type);
                item.mod = this;
                string name = type.Name;
                string texture = (type.Namespace + "." + type.Name).Replace('.', '/');
                EquipType? equip = null;
                if(item.Autoload(ref name, ref texture, ref equip))
                {
                    ErrorLogger.Log(texture);
                    AddItem(name, item, texture);
                    if(equip.HasValue)
                    {
                        string equipTexture = texture + "_" + equip.Value;
                        string armTexture = texture + "_Arms";
                        string femaleTexture = texture + "_FemaleBody";
                        item.AutoloadEquip(ref equipTexture, ref armTexture, ref femaleTexture);
                        int slot = AddEquipTexture(equip.Value, equipTexture, armTexture, femaleTexture);
                        EquipLoader.idToType[item.item.type] = equip.Value;
                        EquipLoader.idToSlot[item.item.type] = slot;
                    }
                }
            }
        }
    }

    public void AddItem(string name, ModItem item, string texture)
    {
        int id = ItemLoader.ReserveItemID();
        item.item.name = name;
        item.item.ResetStats(id);
        items[name] = item;
        ItemLoader.items[id] = item;
        item.texture = texture;
        item.mod = this;
    }

    public ModItem GetItem(string name)
    {
        if (items.ContainsKey(name))
        {
            return items[name];
        }
        else
        {
            return null;
        }
    }

    public int ItemType(string name)
    {
        ModItem item = GetItem(name);
        if(item == null)
        {
            return 0;
        }
        return item.item.type;
    }

    public void SetGlobalItem(GlobalItem globalItem)
    {
        globalItem.mod = this;
        this.globalItem = globalItem;
    }

    public GlobalItem GetGlobalItem()
    {
        return this.globalItem;
    }

    public int AddEquipTexture(EquipType type, string texture, string armTexture = "", string femaleTexture = "")
    {
        int slot = EquipLoader.ReserveEquipID(type);
        EquipLoader.equips[type][texture] = slot;
        if(type == EquipType.Body)
        {
            EquipLoader.armTextures[slot] = armTexture;
            EquipLoader.femaleTextures[slot] = femaleTexture.Length > 0 ? femaleTexture : texture;
        }
        return slot;
    }

    internal void SetupContent()
    {
        foreach(ModItem item in items.Values)
        {
            Main.itemTexture[item.item.type] = ModLoader.GetTexture(item.texture);
            Main.itemName[item.item.type] = item.item.name;
            EquipLoader.SetSlot(item.item);
            item.SetDefaults();
            DrawAnimation animation = item.GetAnimation();
            if(animation != null)
            {
                Main.RegisterItemAnimation(item.item.type, animation);
                ItemLoader.animations.Add(item.item.type);
            }
        }
    }

    internal void Unload()
    {
        recipes.Clear();
        items.Clear();
        globalItem = null;
    }
}}