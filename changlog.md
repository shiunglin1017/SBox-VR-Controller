# Changelog

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

