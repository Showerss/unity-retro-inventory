# Retro Inventory & Pickups

A slot-based 90s-styled inventory for Unity: stackable items, a hotbar, world pickups,
held-item rendering, readable documents and key-locked targets.

## Architecture Overview

```
ItemDefinition (ScriptableObject - what an item IS)
├── ItemUseAction (ScriptableObject - what happens when you USE it)
└── worldPrefab / heldPrefab (what it looks like on the floor / in your hand)

InventorySystem (MonoBehaviour on the Player - owns the slots)
├── ItemStack[]        slots, each an ItemDefinition + quantity
├── InventoryHUD       draws the hotbar, reads keyboard/mouse input
├── HeldItemManager    spawns heldPrefab on the camera, drives flashlight/lantern
└── ItemDatabase       id -> ItemDefinition lookup, used to rebuild saves

WorldPickup     (IInteractable) - an item lying in the world
LockableTarget  (IInteractable) - a door/gate that demands an item id
```

### Key Features

1. **Slot-based with stacking.** Capacity is configurable (1-24). Items stack up to
   their own `maxStack`; `AddItem` fills partial stacks before taking a new slot.
2. **Data-driven items.** An item is an `ItemDefinition` asset — id, display name,
   category, icon, description, stack size, world prefab, held prefab, sounds. No code
   per item.
3. **Pluggable use behaviour.** `ItemUseAction` is a ScriptableObject base class.
   Ships with `ToggleLightAction` (flashlight/lantern) and `ReadDocumentAction`
   (fullscreen readable note). Subclass it for anything else.
4. **Held items.** `HeldItemManager` parents the selected item's `heldPrefab` to the
   camera, so the player visibly carries what they've equipped.
5. **Locked targets.** `LockableTarget` sits in front of a Door or any other
   `IInteractable` and consumes/checks a required item id before delegating through.
6. **Events for everything.** `OnInventoryChanged`, `OnSlotChanged`, `OnSlotSelected`,
   `OnItemUsed`, `OnNotification` — wire your own UI without touching this package.

---

## Requirements

This package depends on **[First Person Controller](https://github.com/Showerss/unity-first-person-controller)**
for its `IInteractable` / `PlayerInteraction` raycast interaction system. `WorldPickup`
and `LockableTarget` implement `IInteractable`, and `InventoryHUD` reads the current
interaction prompt from `PlayerInteraction`.

It is declared in `package.json`, so Unity's Package Manager will pull it in
automatically when you install via git URL.

---

## Installation

### Option 1: Via Git URL (Package Manager)
1. In Unity, open **Window > Package Manager**.
2. Click the **`+`** icon in the upper-left corner and choose **Add package from git URL...**
3. Enter:
   ```text
   https://github.com/Showerss/unity-retro-inventory.git
   ```

### Option 2: Via `manifest.json`
Add this line to your project's `Packages/manifest.json` under `"dependencies"`:
```json
"com.retro.inventory": "https://github.com/Showerss/unity-retro-inventory.git"
```
Or for local development:
```json
"com.retro.inventory": "file:../path/to/Packages/com.retro.inventory"
```

## Quick Start

1. Add **`InventorySystem`** to your Player GameObject and set the slot capacity.
2. Add **`HeldItemManager`** to the Player and point its hold anchor at your camera.
3. Create items via **Assets > Create > Retro > Inventory > Item Definition**. Give each
   a unique `Id` — that string is what saves, pickups and locks all key off.
4. Create an **`ItemDatabase`** via **Assets > Create > Retro > Item Database** and press
   **Rebuild From Project** so ids resolve on load.
5. Drop a Canvas with **`InventoryHUD`** into the scene and assign the `InventorySystem`
   (it will `FindAnyObjectByType` as a fallback).
6. For pickups: put **`WorldPickup`** on a prop and assign its `ItemDefinition`. A
   non-trigger collider is added automatically if one is missing.
7. For locked doors: put **`LockableTarget`** between the player and the real Door, and
   set the required item id.

### Default controls

`InventoryHUD` reads the keyboard directly (New Input System), so there is no action
asset to wire up:

| Input | Action |
|---|---|
| `1`–`8` | Select hotbar slot |
| Mouse wheel | Cycle hotbar |
| `F` / `Q` | Use selected item |
| `G` | Drop selected item |
| `I` / `Tab` | Toggle full inventory panel |
| `E` / `Space` | Advance / close a document |
| `Esc` | Close the open panel |

## Scripting

```csharp
var inv = player.GetComponent<InventorySystem>();

inv.AddItem(keycardDefinition, 1);     // returns false if full
inv.HasItem("keycard_red");            // by id
inv.GetItemCount("battery");
inv.RemoveItemById("battery", 2);
inv.UseSelectedItem();
inv.DropSlot(0);                       // returns the spawned GameObject

inv.OnInventoryChanged += RefreshMyUI;
inv.OnNotification += msg => Debug.Log(msg);
```

## Saving

This package does not own a save format. If you also use
[Retro Pause Menu](https://github.com/Showerss/unity-retro-pause-menu), add its
`InventorySaveParticipant` component next to your `InventorySystem` and inventory
contents persist into that package's save slots automatically. Otherwise, serialize
`Slots` yourself — each `ItemStack` is just an item id plus a quantity.

## License

[MIT License](LICENSE.md) © 2026 Phillip Banky
