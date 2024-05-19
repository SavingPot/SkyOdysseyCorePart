# Details

Date : 2024-05-19 11:15:48

Directory d:\\MakeGames\\GameProject\\SandboxWorld\\Assets\\Scripts\\Gameplay

Total : 51 files,  9956 codes, 1064 comments, 3138 blanks, all 14158 lines

[Summary](results.md) / Details / [Diff Summary](diff.md) / [Diff Details](diff-details.md)

## Files
| filename | language | code | comment | blank | total |
| :--- | :--- | ---: | ---: | ---: | ---: |
| [Assets/Scripts/Gameplay/BiomeData.cs](/Assets/Scripts/Gameplay/BiomeData.cs) | C# | 119 | 0 | 16 | 135 |
| [Assets/Scripts/Gameplay/Controls/CameraController.cs](/Assets/Scripts/Gameplay/Controls/CameraController.cs) | C# | 41 | 0 | 10 | 51 |
| [Assets/Scripts/Gameplay/Controls/GControls.cs](/Assets/Scripts/Gameplay/Controls/GControls.cs) | C# | 261 | 26 | 68 | 355 |
| [Assets/Scripts/Gameplay/Entities/Behaviours/CreatureHumanBodyPartsBehaviour.cs](/Assets/Scripts/Gameplay/Entities/Behaviours/CreatureHumanBodyPartsBehaviour.cs) | C# | 30 | 4 | 5 | 39 |
| [Assets/Scripts/Gameplay/Entities/Behaviours/EntityInventoryOwnerBehaviour.cs](/Assets/Scripts/Gameplay/Entities/Behaviours/EntityInventoryOwnerBehaviour.cs) | C# | 84 | 48 | 26 | 158 |
| [Assets/Scripts/Gameplay/Entities/Behaviours/EntityIsOnGroundBehaviour.cs](/Assets/Scripts/Gameplay/Entities/Behaviours/EntityIsOnGroundBehaviour.cs) | C# | 43 | 5 | 9 | 57 |
| [Assets/Scripts/Gameplay/Entities/Behaviours/EntityItemContainerBehaviour.cs](/Assets/Scripts/Gameplay/Entities/Behaviours/EntityItemContainerBehaviour.cs) | C# | 53 | 11 | 12 | 76 |
| [Assets/Scripts/Gameplay/Entities/BloodParticlePool.cs](/Assets/Scripts/Gameplay/Entities/BloodParticlePool.cs) | C# | 26 | 1 | 6 | 33 |
| [Assets/Scripts/Gameplay/Entities/Bullet.cs](/Assets/Scripts/Gameplay/Entities/Bullet.cs) | C# | 67 | 3 | 14 | 84 |
| [Assets/Scripts/Gameplay/Entities/CoinEntity.cs](/Assets/Scripts/Gameplay/Entities/CoinEntity.cs) | C# | 46 | 1 | 20 | 67 |
| [Assets/Scripts/Gameplay/Entities/Creature.cs](/Assets/Scripts/Gameplay/Entities/Creature.cs) | C# | 296 | 32 | 119 | 447 |
| [Assets/Scripts/Gameplay/Entities/CreatureBodyPart.cs](/Assets/Scripts/Gameplay/Entities/CreatureBodyPart.cs) | C# | 90 | 5 | 13 | 108 |
| [Assets/Scripts/Gameplay/Entities/DamageTextPool.cs](/Assets/Scripts/Gameplay/Entities/DamageTextPool.cs) | C# | 40 | 1 | 6 | 47 |
| [Assets/Scripts/Gameplay/Entities/Drop.cs](/Assets/Scripts/Gameplay/Entities/Drop.cs) | C# | 96 | 5 | 30 | 131 |
| [Assets/Scripts/Gameplay/Entities/Enemy.cs](/Assets/Scripts/Gameplay/Entities/Enemy.cs) | C# | 104 | 5 | 31 | 140 |
| [Assets/Scripts/Gameplay/Entities/Entity.cs](/Assets/Scripts/Gameplay/Entities/Entity.cs) | C# | 560 | 90 | 240 | 890 |
| [Assets/Scripts/Gameplay/Entities/EntityCenter.cs](/Assets/Scripts/Gameplay/Entities/EntityCenter.cs) | C# | 111 | 3 | 32 | 146 |
| [Assets/Scripts/Gameplay/Entities/EntityData.cs](/Assets/Scripts/Gameplay/Entities/EntityData.cs) | C# | 37 | 0 | 2 | 39 |
| [Assets/Scripts/Gameplay/Entities/EntityIdAttribute.cs](/Assets/Scripts/Gameplay/Entities/EntityIdAttribute.cs) | C# | 13 | 0 | 3 | 16 |
| [Assets/Scripts/Gameplay/Entities/EntityInit.cs](/Assets/Scripts/Gameplay/Entities/EntityInit.cs) | C# | 382 | 32 | 102 | 516 |
| [Assets/Scripts/Gameplay/Entities/Inventory.cs](/Assets/Scripts/Gameplay/Entities/Inventory.cs) | C# | 722 | 42 | 237 | 1,001 |
| [Assets/Scripts/Gameplay/Entities/ItemBehaviour.cs](/Assets/Scripts/Gameplay/Entities/ItemBehaviour.cs) | C# | 101 | 12 | 35 | 148 |
| [Assets/Scripts/Gameplay/Entities/NPC.cs](/Assets/Scripts/Gameplay/Entities/NPC.cs) | C# | 18 | 4 | 10 | 32 |
| [Assets/Scripts/Gameplay/Entities/NotSummonableAttribute.cs](/Assets/Scripts/Gameplay/Entities/NotSummonableAttribute.cs) | C# | 5 | 0 | 1 | 6 |
| [Assets/Scripts/Gameplay/Entities/Player/Contollers/GamepadController.cs](/Assets/Scripts/Gameplay/Entities/Player/Contollers/GamepadController.cs) | C# | 42 | 1 | 6 | 49 |
| [Assets/Scripts/Gameplay/Entities/Player/Contollers/KeyboardAndMouseController.cs](/Assets/Scripts/Gameplay/Entities/Player/Contollers/KeyboardAndMouseController.cs) | C# | 42 | 1 | 6 | 49 |
| [Assets/Scripts/Gameplay/Entities/Player/Contollers/PlayerController.cs](/Assets/Scripts/Gameplay/Entities/Player/Contollers/PlayerController.cs) | C# | 42 | 0 | 10 | 52 |
| [Assets/Scripts/Gameplay/Entities/Player/Contollers/TouchscreenController.cs](/Assets/Scripts/Gameplay/Entities/Player/Contollers/TouchscreenController.cs) | C# | 47 | 1 | 14 | 62 |
| [Assets/Scripts/Gameplay/Entities/Player/InfoShowers.cs](/Assets/Scripts/Gameplay/Entities/Player/InfoShowers.cs) | C# | 190 | 8 | 137 | 335 |
| [Assets/Scripts/Gameplay/Entities/Player/Player.cs](/Assets/Scripts/Gameplay/Entities/Player/Player.cs) | C# | 1,224 | 185 | 502 | 1,911 |
| [Assets/Scripts/Gameplay/Entities/Player/PlayerCenter.cs](/Assets/Scripts/Gameplay/Entities/Player/PlayerCenter.cs) | C# | 61 | 3 | 11 | 75 |
| [Assets/Scripts/Gameplay/Entities/Player/PlayerSkin.cs](/Assets/Scripts/Gameplay/Entities/Player/PlayerSkin.cs) | C# | 89 | 0 | 10 | 99 |
| [Assets/Scripts/Gameplay/Entities/Player/PlayerUI.cs](/Assets/Scripts/Gameplay/Entities/Player/PlayerUI.cs) | C# | 1,478 | 196 | 486 | 2,160 |
| [Assets/Scripts/Gameplay/Entities/Player/SlotUIs.cs](/Assets/Scripts/Gameplay/Entities/Player/SlotUIs.cs) | C# | 169 | 13 | 50 | 232 |
| [Assets/Scripts/Gameplay/Entities/StateMachine.cs](/Assets/Scripts/Gameplay/Entities/StateMachine.cs) | C# | 28 | 0 | 6 | 34 |
| [Assets/Scripts/Gameplay/GAudio.cs](/Assets/Scripts/Gameplay/GAudio.cs) | C# | 136 | 10 | 37 | 183 |
| [Assets/Scripts/Gameplay/GFiles.cs](/Assets/Scripts/Gameplay/GFiles.cs) | C# | 291 | 13 | 64 | 368 |
| [Assets/Scripts/Gameplay/GM.cs](/Assets/Scripts/Gameplay/GM.cs) | C# | 1,182 | 213 | 336 | 1,731 |
| [Assets/Scripts/Gameplay/GTime.cs](/Assets/Scripts/Gameplay/GTime.cs) | C# | 179 | 9 | 49 | 237 |
| [Assets/Scripts/Gameplay/GameCallbacks.cs](/Assets/Scripts/Gameplay/GameCallbacks.cs) | C# | 37 | 0 | 11 | 48 |
| [Assets/Scripts/Gameplay/Magic/IManaContainer.cs](/Assets/Scripts/Gameplay/Magic/IManaContainer.cs) | C# | 21 | 0 | 6 | 27 |
| [Assets/Scripts/Gameplay/Magic/ISpellContainer.cs](/Assets/Scripts/Gameplay/Magic/ISpellContainer.cs) | C# | 23 | 0 | 3 | 26 |
| [Assets/Scripts/Gameplay/Magic/SpellBehaviour.cs](/Assets/Scripts/Gameplay/Magic/SpellBehaviour.cs) | C# | 25 | 1 | 9 | 35 |
| [Assets/Scripts/Gameplay/Map/Block.cs](/Assets/Scripts/Gameplay/Map/Block.cs) | C# | 147 | 15 | 50 | 212 |
| [Assets/Scripts/Gameplay/Map/BlockData.cs](/Assets/Scripts/Gameplay/Map/BlockData.cs) | C# | 89 | 1 | 27 | 117 |
| [Assets/Scripts/Gameplay/Map/Chunk.cs](/Assets/Scripts/Gameplay/Map/Chunk.cs) | C# | 140 | 14 | 35 | 189 |
| [Assets/Scripts/Gameplay/Map/Map.cs](/Assets/Scripts/Gameplay/Map/Map.cs) | C# | 376 | 21 | 96 | 493 |
| [Assets/Scripts/Gameplay/Map/MapUtils.cs](/Assets/Scripts/Gameplay/Map/MapUtils.cs) | C# | 9 | 1 | 2 | 12 |
| [Assets/Scripts/Gameplay/Map/PosConvert.cs](/Assets/Scripts/Gameplay/Map/PosConvert.cs) | C# | 89 | 5 | 24 | 118 |
| [Assets/Scripts/Gameplay/RandomUpdater.cs](/Assets/Scripts/Gameplay/RandomUpdater.cs) | C# | 137 | 18 | 30 | 185 |
| [Assets/Scripts/Gameplay/Region.cs](/Assets/Scripts/Gameplay/Region.cs) | C# | 318 | 5 | 74 | 397 |

[Summary](results.md) / Details / [Diff Summary](diff.md) / [Diff Details](diff-details.md)