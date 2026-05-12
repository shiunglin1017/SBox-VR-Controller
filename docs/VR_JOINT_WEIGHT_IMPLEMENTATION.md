# VR 抓取系統：Joint / Weight 實作說明（SBox-VR-Controller）

## 目的
本文件說明目前專案中 `joint/weight` 的實作方式，讓團隊理解抓取手感是如何形成，並方便後續調參與擴充。

---

## 1. 架構總覽

目前有兩層關鍵 Joint：

1. **`ItemJoint`（手 ↔ 物品）**
   - 建立於抓取成功時。
   - 用來讓物品跟著手移動，並保留彈性與重量感。

2. **`_handAnchorJoint`（手 ↔ 控制器追蹤點）**
   - 僅在 `UsePhysicalHand = true` 時啟用。
   - 讓手本體是動態剛體，可被重物反作用力「拖慢/拉扯」。

Weight（重量設定）會同時影響：
- 物品跟手的 spring 強度
- 手追蹤控制器的剛性
- IK 姿勢追隨速度
- 丟擲速度上限

---

## 2. `ItemJoint`：手與物品的連接

抓取時在 `VrhandInteraction.Grab()` 內建立 joint：

- Joint 型別：`PhysicsJoint.CreateFixed(p1, p2)`
- `p1`：物品剛體上的抓點（由 `GrabPoint` 轉成 body local）
- `p2`：手的錨點  
  - 一般模式：`JointPoint`（kinematic）
  - Physical Hand 模式：手本體 `Body`（dynamic）

建立後設定：
- `SpringLinear`
- `SpringAngular`

雖然是 fixed joint，但透過 spring 參數形成「有彈性的跟手」而非完全硬鎖。

---

## 3. `_handAnchorJoint`：手本體追蹤控制器

在 `UsePhysicalHand = true` 時建立 `Sandbox.FixedJoint`：

- `Body = 手物件`
- `AnchorBody = Reference（控制器追蹤參考）`
- 依重量等級（Light/Medium/Heavy）動態調整：
  - `LinearFrequency`
  - `AngularFrequency`
  - `Damping`

效果：
- 輕物：手回到控制器更快、更穩
- 重物：手更容易被慣性或反作用力拉開，重量感更明顯

---

## 4. Weight 來源：`MassBasedWeightProfileProvider`

`ResolveProfile(heldPoint, twoHandedHolding)` 決定重量 profile：

### 4.1 判定重量等級（`GrabWeightClass`）
優先順序：
1. 物件 Tag：`vr_weight_light` / `vr_weight_medium` / `vr_weight_heavy`
2. 若無 Tag，改用 `Rigidbody.Mass` 與門檻判斷

### 4.2 對應到 `GrabWeightProfile`
每個等級包含參數：
- `FollowPositionLerp`
- `FollowRotationLerp`
- `MaxDegreesPerSecond`
- `ReleaseLinearClamp`
- `ReleaseAngularClamp`

### 4.3 雙手握持修正
若 `twoHandedHolding = true`，套用 `TwoHandedMultiplier`，提升追隨穩定度。

---

## 5. Weight 實際影響的行為

### (A) 物品跟手彈性（`ItemJoint`）
以 `FollowPositionLerp` 推導 `springScale`，再影響 joint spring 強度。  
=> 輕物通常更貼手，重物更有拖曳感。

### (B) 手本體追蹤剛性（`_handAnchorJoint`）
`ApplyPhysicalHandFrequencies()` 依重量等級切換頻率/阻尼。  
=> 重物時手不會瞬間回正，體感更自然。

### (C) IK 姿勢穩定器
`RotationLimitedHandPoseStabilizer` 使用 profile 的：
- `FollowPositionLerp`
- `FollowRotationLerp`
- `MaxDegreesPerSecond`  
控制追隨插值與旋轉速度上限。  
=> 重物轉向較慢且穩，降低抖動與穿幫。

### (D) 丟擲速度限制
`Drop()` 呼叫 throw estimator 時，使用：
- `ReleaseLinearClamp`
- `ReleaseAngularClamp`  
=> 輕物可丟更快，重物速度被更早限制。

---

## 6. Runtime 流程（抓取到放手）

1. **開始抓取**
   - 解析重量 profile
   - 建立 `ItemJoint`
   - 套用 spring 參數

2. **持有中（每幀）**
   - 重新解析重量（含雙手狀態）
   - 更新 joint 點位（`UpdateItemJoint`）
   - 套用 hand anchor 頻率（Physical Hand 模式）
   - 用 stabilizer 平滑 IK

3. **放手**
   - 用 throw estimator + weight clamp 計算出手速度
   - 移除 `ItemJoint`
   - 手的 profile 回到 Light（加速回復追蹤）

---

## 7. 設計優點（為何彈性高）

此架構將重量影響集中在 profile/provider，且同時作用於物理、IK、丟擲三層。  
因此新增物品類型時，通常只需調整 profile 或 provider 規則，不必到處加特例邏輯。

---

## 8. 主要參考檔案

- `Code/Player/VrhandInteraction.cs`
- `Code/Player/Services/MassBasedWeightProfileProvider.cs`
- `Code/VRLogic/GrabWeightProfile.cs`
- `Code/Player/Services/RotationLimitedHandPoseStabilizer.cs`
- `Code/Player/Services/PeakThrowVelocityEstimator.cs`
