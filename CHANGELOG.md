# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
