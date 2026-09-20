# Retro Inventory & Pickup System — Architecture & AI Developer Guide

> **Project:** 90s Retro Shader / Industrial FPS  
> **Unity Version:** 6000.4.8f1  
> **Render Pipeline:** Universal Render Pipeline (URP)  
> **Input Backend:** Unity New Input System (`UnityEngine.InputSystem`)  
> **Target Audience:** Future AI Agents (Claude, GPT, Gemini, Cursor) & Human Developers  

---

## 1. Architecture Overview & Design Philosophy

This inventory system was engineered specifically for retro 90s-styled first-person games (inspired by *Half-Life*, *Deus Ex*, *Silent Hill*, and *System Shock*). It enforces strict **decoupling**, **data-driven design via ScriptableObjects**, and **polymorphic action execution**.

### Architectural Decision: Code vs. Documentation for Future AIs
> **Question:** *"Is the code itself enough for future AIs to pick up and understand fully, or do you need this document?"*  
>
> **Answer:** While the codebase is strictly typed and clean, **having this dedicated architecture document is vastly superior**. Here is why:
> 1. **Token Economy & Context Window:** When asking an AI to add a feature (e.g. *"add item throwing"* or *"add a crowbar with hands"*), feeding it this 5-minute architectural brief prevents the AI from reading dozens of files, guessing relationships, or rewriting working components.
> 2. **Explicit Extension Points:** Future models excel when given defined extension points (e.g. `ItemUseAction`, `HeldItemManager`, `LockableTarget`). Without an architecture guide, an AI might place item logic inside a monolithic switch statement instead of using the existing ScriptableObject action pipeline.
> 3. **Scene-Component Relationships:** Scene hierarchies, camera sockets, UI anchors, and raycast collider rules are difficult for code-only AI scans to discern. This document explicitly maps how scene objects must be configured.

---

## 2. Core Architecture Diagram

```
+-------------------------------------------------------------+
|                      DATA LAYER (Assets)                    |
|  ItemDefinition (ScriptableObject)                          |
|    - id, displayName, category, description, maxStack       |
|    - isUsable, isConsumable                                 |
|    - worldPrefab, heldPrefab                                |
|    - useAction (ItemUseAction ScriptableObject)             |
+------------------------------+------------------------------+
                               | referenced by
        +----------------------+----------------------+
        |                                             |
+-------v-------------------------+     +-------------v---------------------+
|      WORLD PICKUPS              |     |          PLAYER CORE              |
|  WorldPickup : IInteractable    |     |  InventorySystem                  |
|    - ItemDefinition, quantity   |     |    - Slots (ItemStack[])          |
|    - Non-trigger raycast box    |     |    - AddItem(), RemoveItem()      |
|    - Bobbing & rotating anim    |     |    - UseSelectedItem(), DropSlot()|
+---------------+-----------------+     |  HeldItemManager                  |
                |                       |    - HeldItemSocket (under Cam)   |
                | detected by           |    - Flashlight / Lantern Lights  |
                v                       +--------------+--------------------+
+---------------------------------+                    |
|   PlayerInteraction (Raycast)   |                    | updates / events
|     - Hits IInteractable        |                    v
|     - Updates CurrentPrompt     |     +-----------------------------------+
|     - Triggers on [E] key       |     |          UI / HUD LAYER           |
+---------------------------------+     |  InventoryHUD                     |
                                        |    - [E] Interaction Prompt       |
                                        |    - Toast Notification banner    |
                                        |    - Hotbar [1]..[5] Dock         |
                                        |    - [TAB] Retro Bag Satchel      |
                                        |    - Document / Lore Reader Modal |
                                        +-----------------------------------+
```

---

## 3. Directory Structure

```
Assets/
├── Documentation/
│   └── INVENTORY_SYSTEM_ARCHITECTURE.md    <-- You are here
├── ItemData/
│   ├── Actions/
│   │   ├── Action_Flashlight.asset         <-- ToggleLightAction (Spotlight)
│   │   ├── Action_Lantern.asset            <-- ToggleLightAction (Pointlight)
│   │   └── Action_Logbook.asset            <-- ReadDocumentAction (Facility notes)
│   ├── item_flashlight.asset               <-- ItemDefinition
│   ├── item_lantern.asset                  <-- ItemDefinition
│   ├── item_brass_key.asset                <-- ItemDefinition
│   ├── item_security_keycard.asset         <-- ItemDefinition
│   ├── item_dusty_journal.asset            <-- ItemDefinition
│   └── item_battery.asset                  <-- ItemDefinition
├── Prefabs/
│   ├── InventoryHUD_Canvas.prefab          <-- Complete HUD & UI modals
│   └── Pickups/
│       ├── Pickup_Flashlight.prefab        <-- World pickup prefab
│       ├── Pickup_Lantern.prefab           <-- World pickup prefab
│       ├── Pickup_BrassKey.prefab          <-- World pickup prefab
│       ├── Pickup_Keycard.prefab           <-- World pickup prefab
│       ├── Pickup_Book.prefab              <-- World pickup prefab
│       └── Pickup_Battery.prefab           <-- World pickup prefab
├── Scripts/
│   └── Inventory/
│       ├── ItemCategory.cs                 <-- Enum (Key, LightSource, Document, etc.)
│       ├── ItemDefinition.cs               <-- Core ScriptableObject definition
│       ├── ItemStack.cs                    <-- Slot container (Item + Quantity)
│       ├── ItemUseAction.cs                <-- Abstract base for item behaviors
│       ├── InventorySystem.cs              <-- Player inventory controller
│       ├── WorldPickup.cs                  <-- Pickup component (IInteractable)
│       ├── HeldItemManager.cs              <-- Hands socket & dynamic light sources
│       ├── LockableTarget.cs               <-- Key/Keycard locked door target
│       ├── HotbarSlotUI.cs                 <-- Individual slot UI element
│       ├── InventoryHUD.cs                 <-- HUD, Hotbar, Modal UI controller
│       └── Actions/
│           ├── ToggleLightAction.cs        <-- Light toggling behavior
│           └── ReadDocumentAction.cs       <-- Lore/logbook reader behavior
```

---

## 4. Key Classes & Systems

### 4.1 `ItemDefinition` (ScriptableObject)
Defines an item template. All instances live in `Assets/ItemData/`.
- `Id`: Programmatic string identifier (e.g. `"key_brass"`, `"flashlight"`).
- `DisplayName`: In-game name (e.g. `"Brass Key"`).
- `Category`: `Key`, `LightSource`, `Document`, `Tool`, `Consumable`, `Misc`.
- `IsUsable` & `IsConsumable`: Controls whether it triggers an action and whether the item is destroyed on use.
- `UseAction`: Reference to an `ItemUseAction` ScriptableObject.
- `WorldPrefab`: The prefab instantiated when dropping the item.
- `HeldPrefab`: 3D visual attached to the camera socket when equipped.

### 4.2 `ItemUseAction` (Polymorphic Strategy Pattern)
Abstract base class:
```csharp
public abstract class ItemUseAction : ScriptableObject
{
    public abstract bool Execute(GameObject user, ItemDefinition item);
    public virtual string GetFailureReason() => "...";
}
```
- **Why this matters for future AIs:** Never write a switch-case statement in `InventorySystem` for new item actions. Always derive a new class from `ItemUseAction`, create an `.asset` instance, and attach it to the `ItemDefinition`.

### 4.3 `InventorySystem` (MonoBehaviour on `Player`)
- Stores list of `ItemStack` (default 8 slots).
- `AddItem(ItemDefinition item, int count)`: Stacks up to `MaxStack`, or finds next empty slot. Emits `OnInventoryChanged` and `OnNotification`.
- `UseSelectedItem()` / `UseSlot(index)`: Invokes `item.TryUse(player, out message)`.
- `DropSlot(index)`: Instantiates `WorldPrefab` in front of player with forward velocity.
- `SelectSlot(index)`: Equips slot and invokes `OnSlotSelected`.

### 4.4 `WorldPickup` (MonoBehaviour on Pickups)
Implements `IInteractable` to interface directly with `PlayerInteraction`.
- **CRITICAL RULE:** `PlayerInteraction` executes `Physics.Raycast` using `QueryTriggerInteraction.Ignore`. Therefore, any `WorldPickup` GameObject MUST have a collider with `isTrigger = false` (or a solid child collider), otherwise raycasts will pass straight through it.
- Automatically executes smooth retro sine-wave bobbing and rotating.
- When interacted with via `[E]`, adds itself to the player's inventory and destroys the world object.

### 4.5 `HeldItemManager` (MonoBehaviour on `Player`)
- Locates/creates `HeldItemSocket` as a child of the `Camera`.
- Handles instantiation of `item.HeldPrefab` when slots are equipped.
- Contains references to:
  - `_flashlightLight`: Spotlight on camera (narrow, long-range).
  - `_lanternLight`: Pointlight on player (wide, warm 360-degree ambient).

### 4.6 `LockableTarget` (MonoBehaviour)
Implements `IInteractable`. Attach to any door, gate, locker, or chest.
- Checks if player inventory contains `RequiredKeyId`.
- If missing: Displays `[Locked] Requires [Key Name]` and plays rattle audio.
- If present: Displays `[E] Unlock with [Key Name]`, plays unlock chime, fires `OnUnlocked`, and triggers connected `Door` or `LevelTransitionBox`.

---

## 5. Controls & Keybindings

| Key / Input | Action | Behavior |
|-------------|--------|----------|
| **[E]** | Interact / Pick Up | Picks up looked-at item, or opens doors/locks. |
| **[1] - [5]** | Hotbar Select | Directly equips hotbar slot 1 through 5. |
| **Mouse Scroll** | Cycle Hotbar | Cycles equipped slot forwards/backwards. |
| **[F]** or **[Q]** | Use Item | Toggles flashlight/lantern, reads book, or uses key. |
| **[G]** | Drop Item | Drops selected item into the 3D world in front of player. |
| **[TAB]** or **[I]** | Toggle Satchel | Opens detailed bag inspection modal (frees cursor). |
| **[ESC]** | Close Modals | Closes document reader or satchel bag modal. |

---

## 6. How-To Extension Recipes for Future AIs

### Recipe 1: How to Create a New Item & Pickup
1. **Create Item Asset:**  
   In Unity Editor or via script:  
   `Create -> Retro -> Inventory -> Item Definition`  
   Fill in `Id` (e.g. `"crowbar"`), `DisplayName`, `Category`, `Description`.
2. **Create 3D Pickup Prefab:**  
   - Create GameObject with 3D model/mesh.
   - Add a `BoxCollider` (ensure `isTrigger = false`).
   - Add `WorldPickup` component, assign the `ItemDefinition` reference.
   - Save into `Assets/Prefabs/Pickups/Pickup_Crowbar.prefab`.
3. **Assign Prefab Reference:**  
   Drag the prefab into the `_worldPrefab` field of your `ItemDefinition`.

### Recipe 2: How to Add Custom Item Logic (e.g. Health Pack, Food, Weapon)
1. Create a new C# script inheriting from `ItemUseAction`:
   ```csharp
   using UnityEngine;
   using Retro.Inventory;

   [CreateAssetMenu(menuName = "Retro/Inventory/Actions/Heal Action")]
   public class HealAction : ItemUseAction
   {
       [SerializeField] private float _healAmount = 25f;

       public override bool Execute(GameObject user, ItemDefinition item)
       {
           // Find health component on user
           var health = user.GetComponent<PlayerHealth>();
           if (health != null && health.CurrentHealth < health.MaxHealth)
           {
               health.RestoreHealth(_healAmount);
               return true;
           }
           return false;
       }

       public override string GetFailureReason() => "Health is already full!";
   }
   ```
2. Create an instance of this asset in `Assets/ItemData/Actions/`.
3. Assign it to `ItemDefinition._useAction`. Check `IsConsumable = true`.

### Recipe 3: How to Display Items in Player Hands
1. Create a 3D model/viewmodel prefab for the item (e.g. `Held_Flashlight.prefab` or `Held_Handgun.prefab`).
2. Remove any physics colliders on the viewmodel prefab so it doesn't collide with the player.
3. Assign the prefab to `ItemDefinition._heldPrefab`.
4. When the slot is selected, `HeldItemManager` automatically instantiates it under `Player/Camera/HeldItemSocket`.
5. Adjust position/rotation offsets on `HeldItemSocket` as needed for your viewmodel hands.

### Recipe 4: How to Add Throwing Physics to Dropped Items
In `InventorySystem.DropSlot(int index)`:
- The method already instantiates `item.WorldPrefab` and looks for a `Rigidbody`.
- To enable throwing with variable force, increase the velocity vector:
  ```csharp
  rb.linearVelocity = (Camera.main.transform.forward + Vector3.up * 0.2f).normalized * throwForce;
  ```

### Recipe 5: How to Connect Items with Quests (`Retro.QuestSystem`)
When an objective requires picking up an item (e.g. *"Find the Brass Key"*):
1. In `QuestTrigger` or an event listener, subscribe to `InventorySystem.OnInventoryChanged`.
2. Check `inventory.HasItem("key_brass")`.
3. Call `QuestManager.Instance.CompleteObjective(...)`.

---

## 7. Troubleshooting & Verification Rules
1. **Pickup not responding to [E]?**  
   Check the pickup's collider. If `isTrigger` is `true`, `PlayerInteraction`'s raycast will ignore it. Set `isTrigger = false`.
2. **Interaction Prompt missing?**  
   Ensure `InventoryHUD_Canvas` is active in the scene and `_playerInteraction` reference is wired.
3. **Audio silent?**  
   Confirm `AudioSource` on `Player` and `InventorySystem` are present. Volume is controlled via Unity mixer or clip settings.
4. **Mouse cursor stuck during bag menu?**  
   `InventoryHUD` manages `Cursor.lockState = CursorLockMode.None` during modals and locks it back on close.
