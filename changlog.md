# Changelog

## 2026-05-08 - VR Official API Integration（A / C / D / G）

### 1) 輸入抽象擴充（A）

- `Code/Player/Abstractions/IControllerInput.cs` 追加：
  - `GripPose` / `AimPose`（對映 `VRController.Transform` / `AimTransform`）
  - `IsHandTracking`（對映 `VRController.IsHandTracked`）
  - `Trigger` / `Grip` / `Joystick` 的 `Delta` / `Active` 結構化欄位
  - `ButtonAActive` / `ButtonBActive`
  - `GetFingerSplay( int )`、`GetFingerValue( VRFingerKind )`
  - 新版 `TriggerHaptic( HapticEffect, lengthScale, frequencyScale, amplitudeScale )` 與 `StopAllHaptics()`
  - 新增 `VRFingerKind` enum（值與 `Sandbox.VR.FingerValue` 一致：curl 0..4 / splay 10..13）
- `Code/Player/Services/VRControllerAdapter.cs` 重寫：
  - 透過 `Controller.Trigger.Value/.Delta/.Active` 等結構化欄位取代每幀的「上一幀 vs 此幀」差分
  - 新版 haptic 走 `TriggerHaptics(HapticEffect, ...)`，舊版三參數簽章保留為相容路徑
- `Code/Player/Services/NullController.cs` 補上所有新成員的安全預設（已開放為 public 以便單元測試）
- 武器層改用 `Delta` / `WasPressed`：
  - `Code/Weapons/PistolTrigger.cs`：刪除 `lastPullBack`，rising-edge 用 `(Trigger - TriggerDelta) < 0.9`，開火時 `HapticEffect.HardImpact`
  - `Code/Weapons/PistolSlide.cs`：滑套釋放改 `JoystickPressed`，`PulledBack`/`Load` 觸發震動
  - `Code/Weapons/MagazineLoader.cs`：`ButtonBPressed` 取代每幀 `ButtonB`，插/退彈匣有 `HapticEffect.SoftImpact`
  - `Code/RecoilTest.cs`：刪除 `lastTrigger`，後座力時 `HapticEffect.HardImpact`
- `Code/Player/VrhandInteraction.cs::Search()`：射線改用 `controller.AimPose.Position/Forward`（拿不到時退回 hand-root forward）

### 2) 骨骼級手部追蹤（C）

- 新增 `Code/Player/Abstractions/IHandSkeletonProvider.cs`（直接回傳 `Sandbox.VR.VRHandJointData` value type）
- 新增 `Code/Player/Services/SandboxVRHandSkeletonProvider.cs`（`OnUpdate` 一次性快取 `MotionRange.Controller` / `MotionRange.Hand` 兩組 joints，避免每幀新分配）
- `Code/Player/VRAnimationHelper.cs`：
  - `VRHand` 新增 `[Property] UseSkeletalJoints` 與 `[Property] Dictionary<Sandbox.VR.VRHandJoint, GameObject> JointBones`
  - `Fingers()` 在 `UseSkeletalJoints && HasSkeleton` 時走骨骼路徑（套 `VRHandJointData.Transform` 到綁定 bone），否則維持既有 `BendFingers()` lerp
  - 為了避免與 `Sandbox.VR.VRHand` 撞名，本檔不開全域 `using Sandbox.VR;`，改用完整名稱

### 3) 官方手 / 控制器模型切換（D）

- 新增 `Code/Player/Services/OfficialHandToggle.cs`：依 `VrhandInteraction.IsHolding` 與 `IsHandTracking` 切換 Citizen 手與官方 `Sandbox.VR.VRHand`/`VRModelRenderer` 子物件可見性
- `VrhandInteraction` 為支援 toggle 公開兩個 read-only 屬性：`IsHolding`、`State`

### 4) ModelDoc Attachment 掛載 + 物理甩動（G）

- 新增 `Code/Player/Services/VRHolsterSlot.cs`：
  - `OnStart` 強制把 `SourceRenderer.CreateAttachments = true`，並用 `GetAttachmentObject(AttachmentName)` 取得插槽 GameObject
  - `TryHolster` 兩種模式：
    - `UseSpringPhysics = false`：物品 SetParent + `MotionEnabled = false`（剛性鎖死）
    - `UseSpringPhysics = true`：保持 dynamic，建立 `Sandbox.FixedJoint` 元件，`LinearFrequency` / `AngularFrequency` 控制甩動軟硬度，重力仍生效
  - `TryUnholster` 拆 joint + reparent null
- `VrhandInteraction` 新增 holster 互動：
  - `Searching()` 階段：手在某個非空 slot 範圍內按 Grip → 取出物品並走現有 `Grab` 流程
  - `Holding()` 階段：放開 Grip 時若手在某個空且接受該物品的 slot 範圍內 → `Drop()` 後立即 `TryHolster`
- `VrhandInteraction` 新增 `[Property] UsePhysicalHand`：
  - `false`（預設）：手部 `Body.MotionEnabled = false`，每幀 kinematic snap 到 tracker（行為與既有版本一致）
  - `true`：手部 dynamic + 一條官方 `Sandbox.FixedJoint` 連結 hand body → Reference (tracker)，並依 `_currentWeightProfile.WeightClass` 動態設定 `LinearFrequency` / `AngularFrequency`（Light/Medium/Heavy 三檔）
  - 實體手模式下 `ItemJoint` 第二端錨點改用 `Body.PhysicsBody`（即手本體），武器重量會透過 spring 反作用力把手往下/向後拖

### 5) 單元測試

- 新增 `Code/unittest/VRLogic/NullControllerTests.cs`：覆蓋所有新成員的安全預設，含新版 `HapticEffect` 簽章。
- 新增 `Code/unittest/VRLogic/VRFingerKindTests.cs`：以 `[InlineData]` 對應 9 個 `Sandbox.VR.FingerValue` 值，確認 `(int)` cast 一致（含 splay 從 10 起的 gap）。
- `dotnet test Code/unittest/tftvrfullbody.unittest.csproj` 結果：通過 26 / 失敗 0。

### 6) 文件

- 新增 `docs/VR_OFFICIAL_API_INTEGRATION.md`：列出新介面、新元件、編輯器手動接線步驟（Player.prefab、ModelDoc attachment、VRHolsterSlot 配置、UsePhysicalHand 開關）；檔頭加 zh-TW 速查表。
- 更新 `docs/VR_DI_ARCHITECTURE.md`：補入 `IHandSkeletonProvider` / `VRHolsterSlot` / `OfficialHandToggle` 的角色。

### 7) 實際異動檔案清單

修改：

- `Code/Player/Abstractions/IControllerInput.cs`
- `Code/Player/Services/VRControllerAdapter.cs`
- `Code/Player/Services/NullController.cs`（同時改為 `public`）
- `Code/Player/VrhandInteraction.cs`
- `Code/Player/VRAnimationHelper.cs`
- `Code/Weapons/PistolTrigger.cs`
- `Code/Weapons/PistolSlide.cs`
- `Code/Weapons/MagazineLoader.cs`
- `Code/RecoilTest.cs`
- `docs/VR_DI_ARCHITECTURE.md`
- `changlog.md`

新增：

- `Code/Player/Abstractions/IHandSkeletonProvider.cs`
- `Code/Player/Services/SandboxVRHandSkeletonProvider.cs`
- `Code/Player/Services/OfficialHandToggle.cs`
- `Code/Player/Services/VRHolsterSlot.cs`
- `Code/unittest/VRLogic/NullControllerTests.cs`
- `Code/unittest/VRLogic/VRFingerKindTests.cs`
- `docs/VR_OFFICIAL_API_INTEGRATION.md`

未動 prefab：`Assets/prefabs/Player.prefab`（2638 行序列化檔含 GUID 鏈，必須在 s&box 編輯器內手連接，步驟見 `docs/VR_OFFICIAL_API_INTEGRATION.md` D / G2 / G4 節）。

### 8) 編譯與測試驗證

- `dotnet build Code/tftvrfullbody.csproj`：**0 錯誤 / 0 警告**（incremental rebuild 後）。
- `dotnet test Code/unittest/tftvrfullbody.unittest.csproj`：**通過 26 / 失敗 0**（既有 11 + 新增 15）。
  - `NullControllerTests`：6 個（`Side` / `IsTracked` 與 `IsHandTracking` / 類比輸入歸零 / 數位旗標歸零 / 手指讀值 / 兩種 haptic 多載皆 no-op）。
  - `VRFingerKindTests`：9 個 `[InlineData]` 對映 `Sandbox.VR.FingerValue` 全部成員（含 splay 從 10 起的 gap，避免直接 `(int) cast` 跑掉）。

## 2026-05-06 - VR Full Body / Interface-based DI 重構與追蹤修復

### 1) 架構重構（Interface-based DI）

- 新增抽象層 `Code/Player/Abstractions/`：
  - `IVRInputProvider`
  - `IControllerInput`
  - `IHandTracker`
  - `IMovementInputSource`
  - `HandSide`
- 新增服務層 `Code/Player/Services/`：
  - `SandboxVRInputProvider`
  - `VRControllerAdapter`
  - `NullController`
  - `SandboxVRHandTracker`
  - `VRMovementInputSource`
  - `KeyboardMovementInputSource`
  - `CompositeMovementInputSource`

### 2) 手部追蹤核心修復

- 重寫 `Code/Player/VrhandInteraction.cs`：
  - 手部追蹤由 `OnUpdate()` 改為 `OnPreRender()`，避免讀到上一幀追蹤姿態。
  - `Searching` 狀態下直接使用 tracker pose：
    - `WorldPosition = _tracker.Pose.Position`
    - `WorldRotation = _tracker.Pose.Rotation`
  - 手部剛體改為 kinematic（`Body.MotionEnabled = false`），移除手本體彈簧拉動延遲。
  - 保留物件持握時的 `FixedJoint` 流程（只用於抓握物件，不用於追手）。
  - 增加 `IVRInputProvider` / `IHandTracker` 解析與守衛。

### 3) 追蹤器解析問題修復（本次除錯重點）

- 問題現象：
  - `VRHandTracker` log 顯示 `tracked=True` 且座標持續更新。
  - `VrhandInteraction` log 顯示 `trackerNull=True`，手不跟隨。
- 根因：
  - `VrhandInteraction` 原本只用 `FindMode.EverythingInSelfAndAncestors` 找 `IHandTracker`，
    但 `HandLRef/HandRRef` 是 sibling，不在同一層級鏈上，導致找不到 tracker。
- 修正：
  - 新增 `ResolveTracker()`：
    - 先走 local hierarchy 快速路徑。
    - 找不到時 fallback：掃 scene 所有 `IHandTracker`，用「同 root + 同 Side」配對。
  - 另外修正 API 相容性：
    - `FindMode.Everything` 在目前版本不存在，改用 `Scene.GetAllComponents<IHandTracker>()`。

### 4) FixedJoint 編譯歧義修復

- 檔案：`Code/Player/VrhandInteraction.cs`
- 問題：`FixedJoint` 在 `Sandbox.Physics.FixedJoint` 與 `Sandbox.FixedJoint` 之間歧義。
- 修正：將欄位型別明確指定為 `Sandbox.Physics.FixedJoint`。

### 5) 其他功能改動

- `Code/Player/VRAnimationHelper.cs`
  - 改用 `IVRInputProvider`。
  - 手指流程改用 `_handsCache`，避免每幀 `new List<VRHand>` 配置。
- `Code/Player/Vrrotate.cs`
  - 改用 `IVRInputProvider`。
  - 補 `IsProxy` / `IsAvailable` 守衛。
- `Code/XMovement/Example/PlayerWalkControllerSimple.cs`
  - 改用 `IMovementInputSource`，移除直接讀 `Input.VR`。
- 武器相關（`Code/Weapons/`）：
  - `PistolTrigger.cs`、`PistolSlide.cs`、`MagazineLoader.cs`
  - 改由 `GrabPoint.GrabbedHand?.Controller`（`IControllerInput`）讀取輸入。
- `Code/RecoilTest.cs`
  - 改用 `IVRInputProvider`，加入 VR 可用性守衛。
- `Code/Localise.cs`
  - 修正 `ShadowBody` 空參考：`ShadowBody?.GameObject?.Destroy();`

### 6) Prefab 綁定更新

- 更新 `Assets/prefabs/Player.prefab`：
  - root 新增：
    - `TFT.VR.Services.SandboxVRInputProvider`
    - `TFT.VR.Services.VRMovementInputSource`
    - `TFT.VR.Services.KeyboardMovementInputSource`
    - `TFT.VR.Services.CompositeMovementInputSource`
  - `HandLRef` / `HandRRef` 新增：
    - `TFT.VR.Services.SandboxVRHandTracker`
  - `SandboxVRInputProvider.ManagedTrackers` 綁定 head + left hand ref + right hand ref 三個 `VRTrackedObject`。

### 7) Owner-only VR 行為

- `SandboxVRInputProvider.ApplyOwnership()` 會在 proxy 或非 VR 時關閉：
  - `VRAnchor`
  - `ManagedTrackers`（3 個 `VRTrackedObject`）
- 目的：避免 proxy 玩家被本地 HMD/控制器姿態覆蓋。

### 8) 文件新增

- `docs/VR_DI_ARCHITECTURE.md`
- `docs/VR_TROUBLESHOOTING.md`

### 9) 診斷日誌（Debug）

- 新增可開關 debug 欄位（預設關閉）：
  - `SandboxVRInputProvider.DebugLogs`
  - `SandboxVRHandTracker.DebugLogs`
  - `VrhandInteraction.DebugLogs`
- 日誌會顯示：
  - provider 可用性與 ownership 狀態
  - tracker 是否 tracked 與 pose
  - hand interaction 是否成功 snap、或未 snap 原因（proxy / provider unavailable / tracker null）

### 10) 驗證結果（目前）

- `VrhandInteraction`、`SandboxVRInputProvider`、`SandboxVRHandTracker` 相關修改後 lint 正常。
- `FixedJoint` 歧義編譯錯已排除。
- 已定位並修正「tracker 正常但手拿不到 tracker」的主要根因（sibling tracker 解析）。

## 2026-05-07 - VR 抓取 DI 擴充 / Alyx 手感模組導入 / 測試落地

### 1) 新增 VRLogic 模組（規則與估算）

- 新增 `Code/VRLogic/`：
  - `VrInteractionConstants.cs`（`weapon_hold` 等常數）
  - `GrabInteractionRules.cs`（抓取/放手/距離規則）
  - `RotationClampRules.cs`（角速度限幅插值）
  - `ThrowSignalBuffer.cs`（放手速度樣本緩衝）
  - `ThrowEstimator.cs`（峰值鄰域平均 + 速度 clamp）
  - `GrabWeightProfile.cs`（Light/Medium/Heavy 初始參數）
  - `SkeletonMappingProfile.cs`（跨骨架映射 profile 資源骨架）

### 2) DI 介面擴充（抓取、穩定、投擲、重量、重綁）

- 新增 `Code/Player/Abstractions/`：
  - `IGrabPoseResolver`
  - `IHandPoseStabilizer`
  - `IThrowVelocityEstimator`
  - `IWeightProfileProvider`
  - `IRigRebinder`

### 3) DI 服務實作新增

- 新增 `Code/Player/Services/`：
  - `AttachmentFirstGrabPoseResolver`（attachment first + grabpoint fallback）
  - `RotationLimitedHandPoseStabilizer`
  - `PeakThrowVelocityEstimator`
  - `MassBasedWeightProfileProvider`
  - `DefaultRigRebinder`
  - `PlayerControllerRigBridge`

### 4) `VrhandInteraction` 主要改動

- 接入 DI 服務解析：
  - `IGrabPoseResolver`
  - `IHandPoseStabilizer`
  - `IThrowVelocityEstimator`
  - `IWeightProfileProvider`
- 抓取規則改為 `GrabInteractionRules`（取代硬編閾值判斷）。
- Holding 姿態流程：
  - 先由 resolver 決定目標姿態（attachment / fallback）
  - 再由 stabilizer 做平滑與旋轉限速
- 新增投擲估算流程：
  - 取樣速度推入 `ThrowSignalBuffer`
  - 放手時使用 `ThrowEstimator` 套用 clamp
- 新增重量 profile 套用：
  - 依質量/標籤取得 Light/Medium/Heavy
  - 動態影響 joint spring 強度

### 5) `VRAnimationHelper` 與 rebind 接口

- 新增 `RebindRig(mappingProfileId)`：
  - 呼叫 `IRigRebinder.TryRebindRig(...)`
  - 以短時 blend 方式恢復手部 IK 目標，降低切換跳動

### 6) 文件與驗證

- 新增 `docs/vr-migration-validation.md`：
  - attachment/fallback 驗證
  - throw/stabilizer 驗證
  - weight profile 驗證
  - rig rebind 驗證

### 7) 單元測試

- 新增測試專案：`Code/unittest/tftvrfullbody.unittest.csproj`
- 新增測試檔：
  - `VRLogic/GrabInteractionRulesTests.cs`
  - `VRLogic/RotationClampRulesTests.cs`
  - `VRLogic/ThrowEstimatorTests.cs`
  - `VRLogic/GrabWeightProfileTests.cs`
- 測試執行結果（.NET 10）：
  - `dotnet test "Code/unittest/tftvrfullbody.unittest.csproj"`
  - 通過 `11`、失敗 `0`

### 8) 當日修正紀錄

- 修正 `The name 'Math' does not exist in current context`：
  - 補齊 `using System;`（`ThrowSignalBuffer`、`ThrowEstimator`、`RotationClampRules`、`VrhandInteraction`）
- 修正 `ModelRenderer` 無 `GetAttachment`：
  - `AttachmentFirstGrabPoseResolver` 改為使用 `SkinnedModelRenderer` 查 attachment
- 修正單元測試組件參考路徑：
  - `tftvrfullbody.unittest.csproj` 改為正確指向 `../../../../SteamLibrary/...`
