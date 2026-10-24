# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `LightToggleInput`: one key (default F) switches the carried flashlight/lantern on and off from a LightSource-only inventory, without using a hotbar slot.
- `InventoryHUD._extraInventories`: show notifications (e.g. "+ Flashlight") from the player's other inventories.

### Fixed
- `LockableTarget` looks for the key in every inventory on the player, not just the first one, so a separate key inventory works.

## [1.1.0] - 2026-10-16

### Added
- `HeldMeleeSwing`: placeholder slash for a held 3D melee weapon (left-click), with an optional swing sound. Visual only, no hit detection yet.

## [1.0.1] - 2026-09-21

### Fixed
- Decoupled `InventoryHUD` from `PlayerInteraction` to fix CS0246 compilation error when compiling core assembly.
- Introduced `InventoryHUD.PromptProvider` delegate so interaction prompts can be dynamically fed from any external player controller system without a direct assembly reference.
- Updated `FpcPickupBridge` in the FirstPersonController adapter assembly to automatically bind `PlayerInteraction.CurrentPrompt` into `InventoryHUD.PromptProvider`.

## [1.0.0] - 2026-09-20

### Added
- Slot-based inventory with configurable capacity and per-item stack limits.
- Data-driven items via `ItemDefinition` ScriptableObjects (id, icon, category,
  description, world prefab, held prefab, sounds).
- `ItemDatabase` for id-to-definition resolution, with Rebuild From Project.
- Pluggable `ItemUseAction` ScriptableObjects; ships with `ToggleLightAction`
  (flashlight/lantern) and `ReadDocumentAction` (fullscreen readable notes).
- `InventoryHUD` hotbar with number-key selection, scroll-wheel cycling, use, drop
  and a toggleable full inventory panel.
- `HeldItemManager` for rendering the equipped item on the player camera.
- `WorldPickup` and `LockableTarget`, both implementing the First Person Controller's
  `IInteractable` contract.
- Events for inventory changes, slot selection, item use and notifications.
- Standalone UPM package layout with assembly definition.
