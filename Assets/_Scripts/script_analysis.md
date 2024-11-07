# Script Dependency Analysis Report

## Project Overview
- Total Scripts: 146
- MonoBehaviours: 120
- ScriptableObjects: 5

## Circular Dependencies
⚠️ Warning: Circular dependencies detected!
- Cycle: EnemyBasicSetup → EnemyBasicDamagablePart → EnemyBasicSetup
- Cycle: EnemyShootingManager → EnemyBasicAI → EnemyBasicSetup → EnemyShootingManager
- Cycle: EnemyBasicSetup → GameManager → EnemyBasicSetup
- Cycle: EnemyShootingManager → EnemyBasicAI → EnemyBasicSetup → GameManager → EnemyShootingManager
- Cycle: GameManager → SceneManagerBTR → GameManager
- Cycle: GameManager → SceneManagerBTR → ScoreManager → PlayerUI → GameManager
- Cycle: ScoreManager → PlayerUI → ScoreManager
- Cycle: GameManager → SceneManagerBTR → ScoreManager → PlayerUI → PlayerHealth → GameManager
- Cycle: ScoreManager → PlayerUI → PlayerHealth → ScoreManager
- Cycle: EnemyBasicSetup → GameManager → SceneManagerBTR → ScoreManager → PlayerUI → PlayerLocking → EnemyBasicSetup
- Cycle: AimAssistController → ShooterMovement → AimAssistController
- Cycle: PlayerLocking → CrosshairCore → PlayerLocking
- Cycle: CrosshairCore → PlayerShooting → CrosshairCore
- Cycle: PlayerLocking → CrosshairCore → PlayerShooting → PlayerLocking
- Cycle: EnemyBasicSetup → GameManager → SceneManagerBTR → ScoreManager → PlayerUI → PlayerLocking → CrosshairCore → PlayerShooting → ProjectileManager → EnemyBasicSetup
- Cycle: AudioManager → ProjectileAudioManager → AudioManager
- Cycle: GameManager → SceneManagerBTR → ScoreManager → PlayerUI → PlayerLocking → CrosshairCore → PlayerShooting → ProjectileManager → GameManager
- Cycle: SceneManagerBTR → ScoreManager → PlayerUI → PlayerLocking → CrosshairCore → PlayerShooting → ProjectileManager → SceneManagerBTR
- Cycle: PlayerLocking → CrosshairCore → PlayerShooting → ProjectileManager → PlayerLocking
- Cycle: ProjectileManager → EnemyShotState → ProjectileManager
- Cycle: EnemyShotState → ProjectileMovement → EnemyShotState
- Cycle: EnemyShotState → ProjectileMovement → ProjectileStateBased → EnemyShotState
- Cycle: ProjectileManager → EnemyShotState → ProjectileMovement → ProjectileStateBased → PlayerLockedState → ProjectileManager
- Cycle: ProjectileStateBased → PlayerLockedState → ProjectilePool → ProjectileStateBased
- Cycle: ProjectileStateBased → PlayerLockedState → ProjectileState → ProjectileStateBased
- Cycle: ProjectileStateBased → PlayerLockedState → ProjectileStateBased
- Cycle: GameManager → SceneManagerBTR → ScoreManager → PlayerUI → PlayerLocking → CrosshairCore → PlayerShooting → ProjectileManager → EnemyShotState → ProjectileMovement → ProjectileStateBased → PlayerShotState → GameManager
- Cycle: ProjectileManager → EnemyShotState → ProjectileMovement → ProjectileStateBased → PlayerShotState → ProjectileManager
- Cycle: ProjectileStateBased → PlayerShotState → ProjectileStateBased
- Cycle: ProjectileStateBased → ProjectileCombat → ProjectileStateBased
- Cycle: ProjectileManager → EnemyShotState → ProjectileMovement → ProjectileStateBased → ProjectileManager
- Cycle: ProjectileMovement → ProjectileStateBased → ProjectileMovement
- Cycle: ProjectileStateBased → ProjectileVisualEffects → ProjectileStateBased
- Cycle: CrosshairCore → PlayerShooting → ProjectileManager → ProjectileSpawner → CrosshairCore
- Cycle: ProjectileManager → ProjectileSpawner → ProjectileManager
- Cycle: CrosshairCore → PlayerTimeControl → CrosshairCore
- Cycle: PlayerLocking → CrosshairCore → PlayerTimeControl → PlayerLocking
- Cycle: GameManager → SceneManagerBTR → ScoreManager → PlayerUI → PlayerLocking → CrosshairCore → PlayerTimeControl → PlayerMovement → StaminaController → GameManager
- Cycle: PlayerMovement → StaminaController → PlayerMovement
- Cycle: EnemyShootingManager → EnemyBasicAI → EnemyShootingManager
- Cycle: StaticEnemyShooting → EnemyShootingManager → StaticEnemyShooting

## Highly Connected Scripts
- DebugProjectileShooter
  - Dependencies: 7
  - Referenced by: 0
- ColliderHitCallback
  - Dependencies: 5
  - Referenced by: 2
- EnemyBasicSetup
  - Dependencies: 13
  - Referenced by: 9
- EnemyExplodeSetup
  - Dependencies: 9
  - Referenced by: 1
- EnemyEyeballSetup
  - Dependencies: 4
  - Referenced by: 0
- EnemyKiller
  - Dependencies: 3
  - Referenced by: 0
- EnemyShootingManager
  - Dependencies: 4
  - Referenced by: 4
- EnemySnakeMidBoss
  - Dependencies: 10
  - Referenced by: 0
- EnemySpeedController
  - Dependencies: 4
  - Referenced by: 0
- EnemyTwinSnakeBoss
  - Dependencies: 7
  - Referenced by: 1
- DebugControls
  - Dependencies: 8
  - Referenced by: 0
- GameManager
  - Dependencies: 16
  - Referenced by: 13
- SceneManagerBTR
  - Dependencies: 11
  - Referenced by: 8
- SceneStarterForDev
  - Dependencies: 4
  - Referenced by: 0
- WaveEventSubscriptions
  - Dependencies: 10
  - Referenced by: 0
- CrosshairCore
  - Dependencies: 4
  - Referenced by: 5
- PlayerHealth
  - Dependencies: 3
  - Referenced by: 4
- PlayerLocking
  - Dependencies: 14
  - Referenced by: 9
- PlayerMovement
  - Dependencies: 5
  - Referenced by: 4
- PlayerShooting
  - Dependencies: 6
  - Referenced by: 2
- PlayerTimeControl
  - Dependencies: 8
  - Referenced by: 1
- PlayerUI
  - Dependencies: 5
  - Referenced by: 2
- EnemyShotState
  - Dependencies: 6
  - Referenced by: 4
- PlayerLockedState
  - Dependencies: 5
  - Referenced by: 2
- PlayerShotState
  - Dependencies: 7
  - Referenced by: 5
- ProjectileManager
  - Dependencies: 14
  - Referenced by: 16
- ProjectileSpawner
  - Dependencies: 7
  - Referenced by: 8
- ProjectileStateBased
  - Dependencies: 14
  - Referenced by: 23
- StaticEnemyShooting
  - Dependencies: 5
  - Referenced by: 2

## System Analysis
### Audio
Scripts: 2
Scripts:
- AudioManager
  Dependencies:
  - ProjectileAudioManager (refs: 3)
- ProjectileAudioManager
  Dependencies:
  - AudioManager (refs: 2)

### Core Systems
Scripts: 1
Scripts:
- ParticleSystemPooler

### Data
Scripts: 5
Scripts:
- DebugSettings
- SceneGroup
- SceneListData
  Dependencies:
  - SceneGroup (refs: 1)
- SongArrangement
- SplineDataSO

### Gameplay
Scripts: 115
Dependencies:
- Audio: 4 connections
- Data: 1 connections
- Management: 56 connections
- UI: 1 connections
Scripts:
- AsyncOperationExtensions
- BackgroundFX
  Dependencies:
  - ConditionalDebug (refs: 18)
- BaseBehaviour
- ButoParent
- ChildActivator
  Dependencies:
  - ConditionalDebug (refs: 3)
  - StaticEnemyShooting (refs: 3)
- ChildToSpline
- ChronosKoreographyHandler
  Dependencies:
  - ConditionalDebug (refs: 9)
- CinemachineCameraSwitching
  Dependencies:
  - EventManager (refs: 16)
  - ConditionalDebug (refs: 3)
- CinemachineColliderExtension
- CinemachineWorldUpYExtension
- ColliderHitCallback
  Dependencies:
  - ConditionalDebug (refs: 9)
  - ILimbDamageReceiver (refs: 3)
  - NonLockableEnemy (refs: 2)
  - BaseBehaviour (refs: 1)
  - IDamageable (refs: 1)
- ConditionalDebug
  Dependencies:
  - DebugSettings (refs: 19)
- CrosshairCore
  Dependencies:
  - PlayerLocking (refs: 13)
  - ConditionalDebug (refs: 12)
  - PlayerShooting (refs: 5)
  - PlayerTimeControl (refs: 5)
- CSEllipse
- CurvySplineControlPointAdjuster
- CustomAIPathAlignedToSurface
  Dependencies:
  - ConditionalDebug (refs: 33)
- CustomParentConstraint
- DebugControls
  Dependencies:
  - ConditionalDebug (refs: 45)
  - ScoreManager (refs: 8)
  - SceneManagerBTR (refs: 7)
  - PlayerHealth (refs: 6)
  - ProjectileManager (refs: 6)
  - IDamageable (refs: 4)
  - GameManager (refs: 3)
  - ProjectileStateBased (refs: 3)
- DebugProjectileShooter
  Dependencies:
  - ProjectileStateBased (refs: 6)
  - CrosshairCore (refs: 3)
  - PlayerLocking (refs: 3)
  - PlayerShooting (refs: 3)
  - ProjectileManager (refs: 2)
  - ProjectilePool (refs: 2)
  - PlayerShotState (refs: 1)
- DestroyEffect
- DigitalLayerEffect
- DisableAstarOnPlayer
- DisablePlayerFeatures
  Dependencies:
  - PlayerMovement (refs: 5)
- DottedLine
- DOTweenInitializer
- EnemyBase
  Dependencies:
  - ConditionalDebug (refs: 9)
  - IDamageable (refs: 1)
- EnemyBasicAI
  Dependencies:
  - EnemyBasicSetup (refs: 4)
  - EnemyShootingManager (refs: 4)
- EnemyBasicDamagablePart
  Dependencies:
  - EnemyBasicSetup (refs: 1)
  - IDamageable (refs: 1)
- EnemyBasicSetup
  Dependencies:
  - ConditionalDebug (refs: 48)
  - ProjectileEffectManager (refs: 12)
  - GameManager (refs: 11)
  - EnemyShootingManager (refs: 6)
  - ProjectileSpawner (refs: 6)
  - ProjectileManager (refs: 4)
  - EnemyBasicDamagablePart (refs: 3)
  - ScoreManager (refs: 2)
  - TimeManager (refs: 2)
  - ProjectileStateBased (refs: 2)
  - BaseBehaviour (refs: 1)
  - IAttackAgent (refs: 1)
  - IDamageable (refs: 1)
- EnemyChargeAndExplodeAI
  Dependencies:
  - EnemyExplodeSetup (refs: 3)
- EnemyExplodeSetup
  Dependencies:
  - ConditionalDebug (refs: 57)
  - ParticleSystemManager (refs: 24)
  - GameManager (refs: 7)
  - IDamageable (refs: 4)
  - AudioManager (refs: 4)
  - ScoreManager (refs: 2)
  - TimeManager (refs: 2)
  - BaseBehaviour (refs: 1)
  - IAttackAgent (refs: 1)
- EnemyEyeballSetup
  Dependencies:
  - ConditionalDebug (refs: 24)
  - ProjectileManager (refs: 4)
  - ProjectileSpawner (refs: 4)
  - IDamageable (refs: 2)
- EnemyInstantKill
  Dependencies:
  - EnemyBasicSetup (refs: 2)
- EnemyKiller
  Dependencies:
  - IDamageable (refs: 2)
  - EnemyBasicSetup (refs: 1)
  - ProjectileStateBased (refs: 1)
- EnemyShotState
  Dependencies:
  - ProjectileManager (refs: 4)
  - ConditionalDebug (refs: 3)
  - IDamageable (refs: 2)
  - ProjectileMovement (refs: 2)
  - ProjectileState (refs: 1)
  - ProjectileStateBased (refs: 1)
- EnemySnakeMidBoss
  Dependencies:
  - ColliderHitCallback (refs: 6)
  - DestroyEffect (refs: 5)
  - ProjectileManager (refs: 5)
  - SceneManagerBTR (refs: 4)
  - IDamageable (refs: 2)
  - ProjectileSpawner (refs: 2)
  - BaseBehaviour (refs: 1)
  - ILimbDamageReceiver (refs: 1)
  - NonLockableEnemy (refs: 1)
  - ProjectileStateBased (refs: 1)
- EnemyTeleporter
  Dependencies:
  - ConditionalDebug (refs: 9)
- EnemyTwinSnakeBoss
  Dependencies:
  - ConditionalDebug (refs: 39)
  - ColliderHitCallback (refs: 7)
  - ProjectileManager (refs: 5)
  - PlayerLocking (refs: 2)
  - ProjectileSpawner (refs: 2)
  - ILimbDamageReceiver (refs: 1)
  - GameManager (refs: 1)
- FMODCustomLogger
- FMODFilterParameterControl
- FMODLogger
- FmodOneshots
  Dependencies:
  - AudioManager (refs: 4)
- follow3DReticle
- FollowExactPosition
- GenerateCurvySplineFromGameobjects
- IAttackAgent
- IDamageable
- ILimbDamageReceiver
- KoreographerSpinEvent
- KoreoVFXTrigger
  Dependencies:
  - ConditionalDebug (refs: 24)
- LevelLoader
- LookAtCamera
- LookAtGameObejct
- LookAtReversed
- MainMenuSwitchScene
- MeshPrefabPlacer
- MotionExtractionBaseEffect
- NonLockableEnemy
- ObjectAvoidance
- ObjectTeleporter
- OnSwitchSceneEvent
  Dependencies:
  - SceneManagerBTR (refs: 4)
  - GameManager (refs: 2)
- Orbital
- OuroborosInfiniteTrack
  Dependencies:
  - ConditionalDebug (refs: 15)
  - CSEllipse (refs: 3)
- ParallelPosition
- ParentToPlayerPlane
- PersistentObject
- PlacePrefabsOnMesh
- PlanetScript
- PlayerGroundAdjuster
- PlayerHealth
  Dependencies:
  - ScoreManager (refs: 14)
  - GameManager (refs: 2)
  - IDamageable (refs: 1)
- PlayerLockedState
  Dependencies:
  - ConditionalDebug (refs: 12)
  - ProjectileManager (refs: 3)
  - ProjectilePool (refs: 2)
  - ProjectileState (refs: 1)
  - ProjectileStateBased (refs: 1)
- PlayerLocking
  Dependencies:
  - ProjectileStateBased (refs: 11)
  - EnemyBasicSetup (refs: 6)
  - ProjectileManager (refs: 6)
  - PlayerLockedState (refs: 5)
  - NonLockableEnemy (refs: 3)
  - AimAssistController (refs: 3)
  - CrosshairCore (refs: 3)
  - ShooterMovement (refs: 3)
  - StaminaController (refs: 3)
  - UILockOnEffect (refs: 3)
  - EnemyBasicDamagablePart (refs: 2)
  - ProjectilePool (refs: 2)
  - EnemyShotState (refs: 1)
  - PlayerShotState (refs: 1)
- PlayerMovement
  Dependencies:
  - ShooterMovement (refs: 6)
  - LookAtReversed (refs: 4)
  - StaminaController (refs: 4)
  - PlayerHealth (refs: 2)
  - ProjectileStateBased (refs: 1)
- PlayerShooting
  Dependencies:
  - ProjectileSpawner (refs: 6)
  - ConditionalDebug (refs: 3)
  - CrosshairCore (refs: 2)
  - PlayerLocking (refs: 2)
  - ProjectileManager (refs: 2)
  - ProjectileStateBased (refs: 1)
- PlayerShotState
  Dependencies:
  - ProjectileManager (refs: 4)
  - ConditionalDebug (refs: 3)
  - IDamageable (refs: 3)
  - GameManager (refs: 2)
  - ProjectilePool (refs: 2)
  - ProjectileState (refs: 1)
  - ProjectileStateBased (refs: 1)
- PlayerTimeControl
  Dependencies:
  - CrosshairCore (refs: 14)
  - JPGEffectController (refs: 12)
  - MusicManager (refs: 12)
  - QuickTimeEventManager (refs: 11)
  - TimeManager (refs: 10)
  - PlayerLocking (refs: 2)
  - PlayerMovement (refs: 2)
  - ProjectileStateBased (refs: 2)
- PlayVFXOnEvent
  Dependencies:
  - EventManager (refs: 4)
- PoolProjectiles
  Dependencies:
  - ProjectileStateBased (refs: 1)
- PrefabGenerator
- ProjectileCombat
  Dependencies:
  - ProjectileStateBased (refs: 2)
  - IDamageable (refs: 1)
- ProjectileDetection
  Dependencies:
  - ProjectileStateBased (refs: 3)
- ProjectileInitializer
  Dependencies:
  - ProjectileStateBased (refs: 1)
- ProjectileMovement
  Dependencies:
  - ProjectileStateBased (refs: 2)
  - EnemyShotState (refs: 1)
- ProjectilePool
  Dependencies:
  - ProjectileStateBased (refs: 12)
  - ConditionalDebug (refs: 9)
- ProjectileSpawner
  Dependencies:
  - ConditionalDebug (refs: 45)
  - ProjectilePool (refs: 10)
  - ProjectileStateBased (refs: 10)
  - ProjectileManager (refs: 9)
  - CrosshairCore (refs: 8)
  - ProjectileEffectManager (refs: 4)
  - PlayerShotState (refs: 2)
- ProjectileState
  Dependencies:
  - ProjectileStateBased (refs: 3)
- ProjectileStateBased
  Dependencies:
  - ConditionalDebug (refs: 39)
  - ProjectileEffectManager (refs: 14)
  - AudioManager (refs: 12)
  - EnemyShotState (refs: 6)
  - ProjectilePool (refs: 6)
  - ProjectileManager (refs: 3)
  - ProjectileState (refs: 3)
  - ProjectileAudioManager (refs: 2)
  - PlayerShotState (refs: 2)
  - ProjectileCombat (refs: 2)
  - ProjectileMovement (refs: 2)
  - ProjectileVisualEffects (refs: 2)
  - IDamageable (refs: 1)
  - PlayerLockedState (refs: 1)
- ProjectileVisualEffects
  Dependencies:
  - ProjectileStateBased (refs: 2)
- RadarAdjuster
- ReloadScene
- RotateRing
- SceneConfiguration
  Dependencies:
  - ProjectileSpawner (refs: 4)
- SceneStarterForDev
  Dependencies:
  - CinemachineCameraSwitching (refs: 3)
  - ConditionalDebug (refs: 3)
  - SceneManagerBTR (refs: 2)
  - GameManager (refs: 1)
- SceneSwitchCleanup
  Dependencies:
  - CinemachineCameraSwitching (refs: 3)
  - ConditionalDebug (refs: 3)
- SetInitialWave
- SetMainCameraProperties
- ShaderPrewarmer
- ShooterMovement
  Dependencies:
  - AimAssistController (refs: 4)
- SkyboxFX
- SnakeTween
- SpawnFromPool
- StartingVFX
- StaticEnemyShooting
  Dependencies:
  - ConditionalDebug (refs: 30)
  - EnemyShootingManager (refs: 10)
  - ProjectileSpawner (refs: 6)
  - ProjectileManager (refs: 4)
  - ProjectileStateBased (refs: 1)
- TailOrientation
  Dependencies:
  - CustomAIPathAlignedToSurface (refs: 4)
- TailSegment
- TempoSpin
- TimeTesting
- TransparentOptimizationFeature
- UnityMainThreadDispatcher
- UpdateSplineWithBones
- WarpSpeed
- WaveCustomEventSorter
  Dependencies:
  - SceneManagerBTR (refs: 2)
- WaveEventSubscriptions
  Dependencies:
  - SceneManagerBTR (refs: 12)
  - ProjectileManager (refs: 9)
  - GameManager (refs: 6)
  - EventManager (refs: 4)
  - SplineManager (refs: 3)
  - CinemachineCameraSwitching (refs: 2)
  - FmodOneshots (refs: 2)
  - ScoreManager (refs: 2)
  - PlayerLocking (refs: 2)
  - ShooterMovement (refs: 2)
- WaveHUDTimed
- YAxisRotationLimiter

### Management
Scripts: 21
Dependencies:
- Audio: 1 connections
- Data: 4 connections
- Gameplay: 31 connections
- UI: 2 connections
Scripts:
- AimAssistController
  Dependencies:
  - ShooterMovement (refs: 3)
- EnemyShootingManager
  Dependencies:
  - ConditionalDebug (refs: 84)
  - EnemyBasicAI (refs: 6)
  - StaticEnemyShooting (refs: 6)
  - EnemyBasicSetup (refs: 4)
- EnemySpeedController
  Dependencies:
  - ConditionalDebug (refs: 6)
  - EnemyBasicDamagablePart (refs: 3)
  - EnemyBasicSetup (refs: 2)
  - CustomAIPathAlignedToSurface (refs: 2)
- EnemyTwinSnakeController
  Dependencies:
  - EnemyTwinSnakeBoss (refs: 6)
- EventManager
- FMODManager
- GameManager
  Dependencies:
  - EventManager (refs: 32)
  - SceneManagerBTR (refs: 17)
  - TimeManager (refs: 11)
  - MusicManager (refs: 11)
  - PlayerLocking (refs: 9)
  - ScoreManager (refs: 8)
  - DebugSettings (refs: 4)
  - PlayerHealth (refs: 4)
  - ProjectileManager (refs: 4)
  - CinemachineCameraSwitching (refs: 3)
  - EnemyBasicSetup (refs: 3)
  - ShooterMovement (refs: 3)
  - PlayerUI (refs: 3)
  - EnemyShootingManager (refs: 1)
  - PlayerMovement (refs: 1)
  - UnityMainThreadDispatcher (refs: 1)
- GlobalVolumeManager
- JPGEffectController
- MusicManager
  Dependencies:
  - SceneGroup (refs: 3)
- ParticleSystemManager
- ProjectileEffectManager
- ProjectileManager
  Dependencies:
  - ConditionalDebug (refs: 72)
  - ProjectileStateBased (refs: 16)
  - EventManager (refs: 10)
  - GameManager (refs: 10)
  - ProjectilePool (refs: 7)
  - TimeManager (refs: 6)
  - AudioManager (refs: 4)
  - ProjectileEffectManager (refs: 3)
  - ProjectileSpawner (refs: 3)
  - EnemyBasicSetup (refs: 2)
  - SceneManagerBTR (refs: 2)
  - PlayerLocking (refs: 2)
  - PlayerShotState (refs: 2)
  - EnemyShotState (refs: 1)
- QuickTimeEventManager
- SceneLoadActivationController
- SceneManagerBTR
  Dependencies:
  - ConditionalDebug (refs: 126)
  - MusicManager (refs: 12)
  - EventManager (refs: 6)
  - GameManager (refs: 6)
  - ScoreManager (refs: 6)
  - GlobalVolumeManager (refs: 4)
  - SplineManager (refs: 2)
  - EnemyBase (refs: 1)
  - SceneGroup (refs: 1)
  - AsyncOperationExtensions (refs: 1)
  - UnityMainThreadDispatcher (refs: 1)
- ScoreManager
  Dependencies:
  - ConditionalDebug (refs: 9)
  - PlayerUI (refs: 2)
- SparkManager
- SplineManager
- StaminaController
  Dependencies:
  - GameManager (refs: 3)
  - PlayerMovement (refs: 1)
- TimeManager
  Dependencies:
  - QuickTimeEventManager (refs: 4)
  - DebugSettings (refs: 2)

### UI
Scripts: 2
Dependencies:
- Gameplay: 2 connections
- Management: 3 connections
Scripts:
- PlayerUI
  Dependencies:
  - ScoreManager (refs: 14)
  - StaminaController (refs: 6)
  - PlayerLocking (refs: 4)
  - PlayerHealth (refs: 2)
  - GameManager (refs: 1)
- UILockOnEffect

## Architectural Analysis
### Domain (Utility)
Scripts: 118
Dependencies:
- Controllers
- Data

### Controllers (Utility)
Scripts: 22
Dependencies:
- Domain
- Data
- UI

### Data (DataAccess)
Scripts: 5

### UI (UI)
Scripts: 1
Dependencies:
- Controllers
- Domain

