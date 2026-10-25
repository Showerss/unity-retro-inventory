# Retro Inventory & Pickups

A modular, slot-based 90s-styled inventory system for Unity: stackable items, a hotbar, world pickups,
held-item rendering, readable documents and key-locked targets.

The core package has **zero external project dependencies** (only requires Unity New Input System and UGUI/TMP). It works standalone with any player controller, and automatically bridges into the First Person Controller raycast interaction system when installed.

## Architecture Overview

```
ItemDefinition (ScriptableObject - what an item IS)
├── ItemUseAction (ScriptableObject - what happens when you USE it)
└── worldPrefab / heldPrefab (what it looks like on the floor / in your hand)

InventorySystem (MonoBehaviour on the Player - owns the slots)
├── ItemStack[]            slots, each an ItemDefinition + quantity
├── InventoryHUD           draws the hotbar, reads keyboard/mouse input
├── HeldItemManager        spawns heldPrefab on the camera, drives flashlight/lantern
└── ItemDatabase           id -> ItemDefinition lookup, used to rebuild saves

IInventoryInteractable     contract for pickups and locks
├── WorldPickup            an item lying in the world
└── LockableTarget         a door/gate/chest that demands an item id

Adapters/ (compiled only when the matching package is present)
└── FirstPersonController/ bridges IInventoryInteractable to FPC IInteractable
```

### Key Features

1. **100% Standalone & Decoupled.** Core assembly only requires Input System and UGUI.
2. **Slot-based with stacking.** Configurable capacity (1-24). Items stack up to their own `maxStack`; `AddItem` fills partial stacks before taking a new slot.
3. **Data-driven items.** An item is an `ItemDefinition` asset — id, display name, category, icon, description, stack size, world prefab, held prefab, sounds.
4. **Pluggable use behaviour.** Polymorphic `ItemUseAction` ScriptableObject pipeline (`ToggleLightAction`, `ReadDocumentAction`, etc.).
5. **Held items.** `HeldItemManager` parents the selected item's `heldPrefab` to the camera/socket.
6. **Locked targets.** `LockableTarget` validates required keys/keycards before unlocking.
7. **Complete Documentation Included.** See `Documentation~/Architecture.md` for the full technical and AI guide.

---

## Requirements

| Package | Version |
|---|---|
| Unity | 6000.0 or newer |
| Input System | 1.19.0+ (`com.unity.inputsystem`) |
| UGUI / TextMeshPro | 2.0.0+ (`com.unity.ugui`) |

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

## Quick Start

1. Add **`InventorySystem`** to your Player GameObject.
2. Add **`HeldItemManager`** to the Player and assign its hold socket.
3. Create items via **Assets > Create > Retro > Inventory > Item Definition**.
4. Create an **`ItemDatabase`** via **Assets > Create > Retro > Item Database** and click **Rebuild From Project**.
5. Drop `InventoryHUD_Canvas.prefab` (included in package) into the scene.
6. For pickups: place **`WorldPickup`** on any 3D prop.
7. For locked doors: place **`LockableTarget`** on the door with your required key ID.

## Scripting

```csharp
var inv = player.GetComponent<InventorySystem>();

inv.AddItem(keycardDefinition, 1);
inv.HasItem("keycard_red");
inv.UseSelectedItem();
inv.DropSlot(0);

inv.OnInventoryChanged += RefreshMyUI;
inv.OnNotification += msg => Debug.Log(msg);
```

## Shops & Money

1. Add `Wallet` to the player (next to the `InventorySystem`s). Set the currency name.
2. Create a **Shop Catalog** (`Create > Retro > Inventory > Shop Catalog`): listings + buy/sell multipliers. Give items a `Value`; tick `IsQuestItem` on items shops must refuse.
3. NPC: `Shopkeeper` (catalog assigned) + `FpcPickupBridge` + a collider.
4. One `ShopUI` object anywhere in the scene (needs an EventSystem). It opens for any shopkeeper.
5. Optional: `CurrencyPickup` + `FpcPickupBridge` for money in the world, `WalletLabel` for a HUD counter.

Shop stock is per session; the wallet is saved by the pause menu's `WalletSaveParticipant`.

## License

[MIT License](LICENSE.md) © 2026 Phillip Banky
